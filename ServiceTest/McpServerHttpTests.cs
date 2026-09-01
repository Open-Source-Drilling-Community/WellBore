using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using OSDC.Drilling.WellBore.Service.Mcp;
using OSDC.Drilling.WellBore.Service.Mcp.Tools;

namespace OSDC.Drilling.WellBore.ServiceTest;

[TestFixture]
public sealed class McpServerHttpTests
{
    private HttpClientTransport _transport = null!;
    private McpClient _client = null!;

    [OneTimeSetUp]
    public async Task SetUp()
    {
        _transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri("http://localhost:8080/wellbore/api/mcp"),
            TransportMode = HttpTransportMode.AutoDetect
        }, NullLoggerFactory.Instance);
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
