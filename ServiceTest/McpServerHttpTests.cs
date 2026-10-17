using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using OSDC.Drilling.WellBore.Service.Mcp;
using OSDC.Drilling.WellBore.Service.Mcp.Tools;

namespace OSDC.Drilling.WellBore.ServiceTest;

[TestFixture]
[NonParallelizable]
public sealed class McpServerHttpTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _httpClient = null!;
    private HttpClientTransport _transport = null!;
    private McpClient _client = null!;

    [OneTimeSetUp]
    public async Task SetUp()
    {
        ServiceTestHost.ResetManagerSingletons();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseContentRoot(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Service")));
            builder.ConfigureLogging(logging => logging.ClearProviders());
        });
        _httpClient = _factory.CreateClient();
        _transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(_httpClient.BaseAddress!, "WellBore/api/mcp"),
            TransportMode = HttpTransportMode.StreamableHttp
        }, _httpClient, NullLoggerFactory.Instance, ownsHttpClient: false);
        _client = await McpClient.CreateAsync(_transport, new McpClientOptions
        {
            ClientInfo = new Implementation { Name = "WellBoreServiceTest", Version = "1.0.0" }
        }, NullLoggerFactory.Instance, CancellationToken.None);
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        if (_client is not null) await _client.DisposeAsync();
        if (_transport is not null) await _transport.DisposeAsync();
        _httpClient?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task Removed_heavy_route_returns_404_and_light_discovery_returns_a_bounded_page()
    {
        using var retired = await _httpClient.GetAsync("WellBore/api/WellBore/HeavyData");
        Assert.That(retired.StatusCode, Is.AnyOf(System.Net.HttpStatusCode.NotFound, System.Net.HttpStatusCode.BadRequest));
        using var discovery = await _httpClient.GetAsync("WellBore/api/WellBore/LightData?offset=0&limit=1");
        Assert.That(discovery.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
        var page = JsonNode.Parse(await discovery.Content.ReadAsStringAsync())!;
        Assert.That(page["Items"]!.AsArray().Count, Is.LessThanOrEqualTo(1));
        Assert.That(page["Limit"]!.GetValue<int>(), Is.EqualTo(1));
        Assert.That(page.AsObject().ContainsKey("NextOffset"), Is.True);
        using var invalid = await _httpClient.GetAsync("WellBore/api/WellBore/LightData?limit=201");
        Assert.That(invalid.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Http_endpoint_publishes_every_registered_non_statistics_tool()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLegacyMcpTool<PingMcpTool>();
        services.AddWellBoreRestMcpTools();
        using ServiceProvider provider = services.BuildServiceProvider();
        var expected = provider.GetServices<McpServerTool>().Select(tool => tool.ProtocolTool.Name);
        string[] remote = (await _client.ListToolsAsync(cancellationToken: CancellationToken.None)).Select(tool => tool.Name).ToArray();
        Assert.That(remote, Is.EquivalentTo(expected));
        Assert.That(remote, Has.None.Contains("statistics"));
    }

    [Test]
    public async Task Ping_can_be_invoked_over_http()
    {
        var result = await _client.CallToolAsync("ping", new Dictionary<string, object?>(), cancellationToken: CancellationToken.None);
        Assert.That(((JsonObject)result.StructuredContent!)["message"]?.GetValue<string>(), Is.EqualTo("pong"));
    }
}
