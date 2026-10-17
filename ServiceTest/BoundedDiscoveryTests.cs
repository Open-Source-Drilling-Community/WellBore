using System;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OSDC.Drilling.WellBore.Service.Controllers;
using OSDC.Drilling.WellBore.Service.Mcp;
using OSDC.Drilling.WellBore.Service.Mcp.Tools;

namespace DiscoveryContractTests;

[TestFixture]
public sealed class BoundedDiscoveryTests
{
    [Test]
    public void Heavy_collection_routes_are_absent_and_discovery_items_omit_engineering_collections()
    {
        var controllers = typeof(WellBoreController).Assembly.GetTypes().Where(type => typeof(ControllerBase).IsAssignableFrom(type));
        var actions = controllers.SelectMany(type => type.GetMethods()).Where(method => method.GetCustomAttribute<HttpGetAttribute>() != null).ToArray();
        Assert.That(actions.Any(method => method.GetCustomAttribute<HttpGetAttribute>()!.Template == "HeavyData"), Is.False);
        var discovery = actions.Where(method => method.Name.StartsWith("Discover", StringComparison.Ordinal)).ToArray();
        Assert.That(discovery, Has.Length.EqualTo(3));
        string[] forbidden = ["Slots", "SurveyStationList", "ErrorSourceList", "CasingSections", "Options", "UnitChoices", "RigJobs", "QuantityUnitConversionList"];
        foreach (var action in discovery)
        {
            Type page = action.ReturnType.GenericTypeArguments.Single();
            Type item = page.GetProperty("Items")!.PropertyType.GenericTypeArguments.Single();
            Assert.That(item.GetProperties().Select(property => property.Name).Intersect(forbidden), Is.Empty, action.Name);
            Assert.That(page.GetProperty("NextOffset"), Is.Not.Null, action.Name);
        }
    }

    [Test]
    public async Task Mcp_discovery_rejects_invalid_pages_before_resolving_storage_and_never_registers_heavy_lists()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddWellBoreRestMcpTools();
        using var provider = services.BuildServiceProvider();
        var tools = provider.GetServices<IMcpTool>().ToArray();
        Assert.That(tools.Any(tool => tool.Name.EndsWith("_get_all", StringComparison.Ordinal) || tool.Name.EndsWith("_get_all_light", StringComparison.Ordinal) || tool.Name.Contains("_get_all_by_", StringComparison.Ordinal)), Is.False);
        var discovery = tools.Where(tool => tool.Name.EndsWith("_discover", StringComparison.Ordinal)).ToArray();
        Assert.That(discovery, Has.Length.EqualTo(3));
        foreach (var tool in discovery)
        {
            Assert.That(tool.InputSchema!["properties"]!["limit"]!["maximum"]!.GetValue<int>(), Is.EqualTo(200), tool.Name);
            Assert.That(tool.InputSchema!["additionalProperties"]!.GetValue<bool>(), Is.False, tool.Name);
            foreach (JsonObject input in new[] { new JsonObject { ["limit"] = 201 }, new JsonObject { ["offset"] = -1 }, new JsonObject { ["limit"] = "all" }, new JsonObject { ["limit"] = null }, new JsonObject { ["unexpected"] = true } })
            {
                JsonNode? result = await tool.InvokeAsync(input, CancellationToken.None);
                Assert.That(result?["status"]?.GetValue<int>(), Is.EqualTo(400), tool.Name + ": " + input);
            }
        }
    }
}
