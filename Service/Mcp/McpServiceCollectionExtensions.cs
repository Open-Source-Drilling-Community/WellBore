using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace NORCE.Drilling.WellBore.Service.Mcp;

public static class McpServiceCollectionExtensions
{
    public static IServiceCollection AddLegacyMcpTool<TTool>(this IServiceCollection services)
        where TTool : class, IMcpTool
    {
        services.AddSingleton<TTool>();
        services.AddSingleton<IMcpTool>(sp => sp.GetRequiredService<TTool>());
        services.AddSingleton<McpServerTool>(sp =>
        {
            var tool = sp.GetRequiredService<TTool>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            return new LegacyMcpServerToolAdapter(tool, loggerFactory);
        });

        return services;
    }

    public static IServiceCollection AddLegacyMcpTool(this IServiceCollection services, string name, string description,
        JsonNode? inputSchema, Func<IServiceProvider, JsonObject?, CancellationToken, Task<JsonNode?>> invokeAsync)
    {
        services.AddSingleton<IMcpTool>(sp => new DelegateMcpTool(
            name, description, inputSchema, (arguments, cancellationToken) => invokeAsync(sp, arguments, cancellationToken)));
        services.AddSingleton<McpServerTool>(sp => new LegacyMcpServerToolAdapter(
            sp.GetServices<IMcpTool>().Last(tool => tool.Name == name), sp.GetRequiredService<ILoggerFactory>()));
        return services;
    }

    private sealed class DelegateMcpTool : IMcpTool
    {
        private readonly Func<JsonObject?, CancellationToken, Task<JsonNode?>> _invokeAsync;
        public DelegateMcpTool(string name, string description, JsonNode? inputSchema,
            Func<JsonObject?, CancellationToken, Task<JsonNode?>> invokeAsync)
        {
            Name = name;
            Description = description;
            InputSchema = inputSchema;
            _invokeAsync = invokeAsync;
        }
        public string Name { get; }
        public string Description { get; }
        public JsonNode? InputSchema { get; }
        public Task<JsonNode?> InvokeAsync(JsonObject? arguments, CancellationToken cancellationToken) => _invokeAsync(arguments, cancellationToken);
    }
}
