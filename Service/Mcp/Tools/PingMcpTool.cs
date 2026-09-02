using System;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using OSDC.Drilling.WellBore.Service.Mcp;

namespace OSDC.Drilling.WellBore.Service.Mcp.Tools;

public sealed class PingMcpTool : IMcpTool
{
    public string Name => "ping";

    public string Description => "Returns a pong response so clients can verify MCP connectivity.";

    public McpToolBehavior Behavior => new("Ping WellBore MCP", true, false, true, false);

    public JsonNode InputSchema => JsonNode.Parse("""{"type":"object","additionalProperties":false}""")!;

    public JsonNode OutputSchema => JsonNode.Parse("""{"type":"object","properties":{"message":{"type":"string"},"timestamp":{"type":"string","format":"date-time"}},"required":["message","timestamp"],"additionalProperties":false}""")!;

    public Task<JsonNode?> InvokeAsync(JsonObject? arguments, CancellationToken cancellationToken)
    {
        var payload = new JsonObject
        {
            ["message"] = "pong",
            ["timestamp"] = DateTimeOffset.UtcNow.ToString("O")
        };

        return Task.FromResult<JsonNode?>(payload);
    }
}
