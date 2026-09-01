using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OSDC.Drilling.WellBore.Service.Managers;

namespace OSDC.Drilling.WellBore.Service.Mcp;

public sealed class McpHubRegistrationService : BackgroundService
{
    public static readonly Guid ServiceTypeId = Guid.Parse("78cd6dcd-1548-47bd-922a-e4d63d691241");
    private const string InstanceIdFileName = "wellbore-mcp-hub-instance-id.txt";
    private readonly IHttpClientFactory _clients;
    private readonly ILogger<McpHubRegistrationService> _logger;
    private readonly IOptionsMonitor<McpHubOptions> _options;
    private Guid? _registeredInstanceId;

    public McpHubRegistrationService(IHttpClientFactory clients, ILogger<McpHubRegistrationService> logger, IOptionsMonitor<McpHubOptions> options)
    {
        _clients = clients;
        _logger = logger;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        McpHubOptions options = _options.CurrentValue;
        if (!options.Enabled) { _logger.LogInformation("MCP hub registration is disabled."); return; }
        if (!IsComplete(options)) { _logger.LogWarning("MCP hub registration skipped because its URLs are not configured."); return; }
        while (!stoppingToken.IsCancellationRequested)
        {
            options = _options.CurrentValue;
            if (!options.Enabled || !IsComplete(options)) return;
            try
            {
                Guid instanceId = ResolveInstanceId(options);
                Uri collectionUri = CreateCollectionUri(options);
                Registration registration = CreateRegistration(options, instanceId);
                using HttpClient client = _clients.CreateClient(nameof(McpHubRegistrationService));
                using HttpResponseMessage response = await PutOrPostAsync(client, collectionUri, instanceId, registration, stoppingToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    _registeredInstanceId = instanceId;
                    _logger.LogInformation("Registered WellBore MCP endpoint on MCP hub at {HubUri}", collectionUri);
                }
                else _logger.LogWarning("MCP hub registration failed with status {StatusCode}.", response.StatusCode);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex) { _logger.LogWarning(ex, "MCP hub registration attempt failed."); }
            try { await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, options.RetryIntervalSeconds)), stoppingToken).ConfigureAwait(false); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        McpHubOptions options = _options.CurrentValue;
        if (options.UnregisterOnShutdown && _registeredInstanceId.HasValue && !string.IsNullOrWhiteSpace(options.HubBaseUrl))
        {
            try
            {
                using HttpClient client = _clients.CreateClient(nameof(McpHubRegistrationService));
                using HttpResponseMessage response = await client.DeleteAsync(new Uri(CreateCollectionUri(options), _registeredInstanceId.Value.ToString()), cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
                    _logger.LogWarning("MCP hub unregister failed with status {StatusCode}.", response.StatusCode);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
            catch (Exception ex) { _logger.LogWarning(ex, "MCP hub unregister failed during shutdown."); }
        }
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<HttpResponseMessage> PutOrPostAsync(HttpClient client, Uri collectionUri, Guid instanceId, Registration registration, CancellationToken ct)
    {
        HttpResponseMessage response = await client.PutAsJsonAsync(new Uri(collectionUri, instanceId.ToString()), registration, ct).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.NotFound) return response;
        response.Dispose();
        return await client.PostAsJsonAsync(collectionUri, registration, ct).ConfigureAwait(false);
    }
    private static bool IsComplete(McpHubOptions options) => !string.IsNullOrWhiteSpace(options.HubBaseUrl) && !string.IsNullOrWhiteSpace(options.PublicBaseUrl);
    private static Registration CreateRegistration(McpHubOptions options, Guid instanceId)
    {
        string publicUrl = options.PublicBaseUrl!.TrimEnd('/');
        return new Registration(ServiceTypeId, instanceId, string.IsNullOrWhiteSpace(options.ServiceName) ? "WellBore" : options.ServiceName,
            $"{publicUrl}/WellBore/api/mcp", ToWebSocketUrl($"{publicUrl}/WellBore/api/mcp/ws"), DateTimeOffset.UtcNow);
    }
    private static Uri CreateCollectionUri(McpHubOptions options) => new(new Uri(options.HubBaseUrl!.TrimEnd('/') + "/"),
        (string.IsNullOrWhiteSpace(options.RegistrationEndpoint) ? "McpMicroservice" : options.RegistrationEndpoint.Trim('/')) + "/");
    private static Guid ResolveInstanceId(McpHubOptions options)
    {
        if (Guid.TryParse(options.InstanceId, out Guid configured) && configured != Guid.Empty) return configured;
        Directory.CreateDirectory(SqlConnectionManager.HOME_DIRECTORY);
        string file = Path.Combine(SqlConnectionManager.HOME_DIRECTORY, InstanceIdFileName);
        if (File.Exists(file) && Guid.TryParse(File.ReadAllText(file), out Guid persisted) && persisted != Guid.Empty) return persisted;
        Guid generated = Guid.NewGuid();
        File.WriteAllText(file, generated.ToString());
        return generated;
    }
    private static string ToWebSocketUrl(string url) => url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        ? "wss://" + url[8..] : url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ? "ws://" + url[7..] : url;

    private sealed record Registration(Guid ServiceTypeId, Guid InstanceId, string Name, string McpHttpUrl, string McpWebSocketUrl, DateTimeOffset LastSeenUtc);
}
