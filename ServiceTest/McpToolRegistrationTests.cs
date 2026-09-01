using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using OSDC.Drilling.WellBore.Service.Controllers;
using OSDC.Drilling.WellBore.Service.Mcp;
using OSDC.Drilling.WellBore.Service.Mcp.Tools;

namespace OSDC.Drilling.WellBore.ServiceTest;

[TestFixture]
public sealed class McpToolRegistrationTests
{
    private static readonly IReadOnlyDictionary<string, string> EndpointToolMap = new Dictionary<string, string>
    {
        ["GetAllWellBoreId"] = "well_bore_get_all_ids",
        ["GetAllWellBoreMetaInfo"] = "well_bore_get_all_meta_info",
        ["GetWellBoreById"] = "well_bore_get_by_id",
        ["GetAllWellBore"] = "well_bore_get_all",
        ["GetAllWellBoreByWellID"] = "well_bore_get_all_by_well_id",
        ["GetAllWellBoreByRigId"] = "well_bore_get_all_by_rig_id",
        ["GetAllWellBoreByParentWellBoreId"] = "well_bore_get_all_by_parent_id",
        ["GetAllSidetrackedWellBore"] = "well_bore_get_all_sidetracked",
        ["PostWellBore"] = "well_bore_create",
        ["PutWellBoreById"] = "well_bore_update_by_id",
        ["DeleteWellBoreById"] = "well_bore_delete_by_id"
    };

    private ServiceProvider _provider = null!;
    private IReadOnlyDictionary<string, IMcpTool> _tools = null!;

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLegacyMcpTool<PingMcpTool>();
        services.AddWellBoreRestMcpTools();
        _provider = services.BuildServiceProvider();
        _tools = _provider.GetServices<IMcpTool>().ToDictionary(tool => tool.Name);
    }

    [TearDown]
    public void TearDown() => _provider.Dispose();

    [Test]
    public void Every_non_statistics_controller_endpoint_has_a_registered_tool()
    {
        var endpoints = typeof(WellBoreController).GetMethods()
            .Where(method => method.GetCustomAttributes(typeof(HttpMethodAttribute), true).Length > 0)
            .Select(method => method.Name);
        Assert.That(endpoints, Is.EquivalentTo(EndpointToolMap.Keys));
        Assert.That(_tools.Keys, Is.EquivalentTo(EndpointToolMap.Values.Append("ping")));
    }

    [Test]
    public void Usage_statistics_are_not_exposed() => Assert.That(_tools.Keys, Has.None.Contains("statistics"));

    [Test]
    public void Protocol_tool_names_are_valid_and_unique()
    {
        string[] names = _provider.GetServices<McpServerTool>().Select(tool => tool.ProtocolTool.Name).ToArray();
        Assert.That(names, Has.Length.EqualTo(_tools.Count));
        Assert.That(names, Is.Unique);
        Assert.That(names.All(name => !name.Contains('.')), Is.True);
    }

    [Test]
    public void Rest_tools_have_detailed_descriptions()
    {
        foreach (string toolName in EndpointToolMap.Values)
        {
            Assert.That(_tools[toolName].Description, Has.Length.GreaterThan(100), toolName);
        }
    }

    [TestCase("well_bore_get_all_ids")]
    [TestCase("well_bore_get_all_meta_info")]
    [TestCase("well_bore_get_all")]
    [TestCase("well_bore_get_all_sidetracked")]
    public void Parameterless_tools_publish_an_explicit_empty_object_schema(string toolName)
    {
        JsonObject schema = RequireObject(_tools[toolName].InputSchema);
        Assert.That(schema["type"]?.GetValue<string>(), Is.EqualTo("object"));
        Assert.That(schema["additionalProperties"]?.GetValue<bool>(), Is.False);
    }

    [Test]
    public void Create_tool_schema_describes_the_complete_wellbore_payload()
    {
        JsonObject root = RequireObject(_tools["well_bore_create"].InputSchema);
        Assert.That(RequiredNames(root), Is.EquivalentTo(new[] { "wellBore" }));

        JsonObject wellBore = Property(root, "wellBore");
        Assert.That(RequiredNames(wellBore), Does.Contain("MetaInfo"));
        Assert.That(PropertyNames(wellBore), Is.EquivalentTo(new[]
        {
            "MetaInfo", "Name", "Description", "CreationDate", "LastModificationDate",
            "WellID", "RigID", "IsSidetrack", "ParentWellBoreID",
            "TieInPointAlongHoleDepth", "SidetrackType"
        }));
        Assert.That(wellBore["additionalProperties"]?.GetValue<bool>(), Is.False);

        JsonObject metaInfo = Property(wellBore, "MetaInfo");
        Assert.That(RequiredNames(metaInfo), Does.Contain("ID"));
        Assert.That(Property(metaInfo, "ID")["format"]?.GetValue<string>(), Is.EqualTo("uuid"));
        Assert.That(Property(wellBore, "CreationDate")["format"]?.GetValue<string>(), Is.EqualTo("date-time"));
        Assert.That(Property(wellBore, "WellID")["format"]?.GetValue<string>(), Is.EqualTo("uuid"));
        Assert.That(Property(wellBore, "ParentWellBoreID")["format"]?.GetValue<string>(), Is.EqualTo("uuid"));

        JsonObject tieIn = Property(wellBore, "TieInPointAlongHoleDepth");
        JsonObject gaussian = Property(tieIn, "GaussianValue");
        Assert.That(Property(gaussian, "Mean")["type"], Is.TypeOf<JsonArray>());
        Assert.Multiple(() =>
        {
            Assert.That(tieIn["description"]?.GetValue<string>(), Does.Contain("meters (SI)"));
            Assert.That(tieIn["description"]?.GetValue<string>(), Does.Contain("WGS84 vertical datum"));
            Assert.That(Property(gaussian, "Mean")["description"]?.GetValue<string>(), Does.Contain("meters (SI)"));
            Assert.That(Property(gaussian, "Mean")["description"]?.GetValue<string>(), Does.Contain("WGS84 vertical datum"));
            Assert.That(Property(gaussian, "StandardDeviation")["description"]?.GetValue<string>(), Does.Contain("meters (SI)"));
        });

        string[] sidetrackTypes = ((JsonArray)Property(wellBore, "SidetrackType")["enum"]!)
            .Select(node => node!.GetValue<string>()).ToArray();
        Assert.That(sidetrackTypes, Is.EquivalentTo(new[] { "Undefined", "Technical", "Production", "Appraisal", "Lateral" }));
    }

    [Test]
    public void Update_tool_schema_requires_matching_id_and_wellbore_arguments()
    {
        JsonObject root = RequireObject(_tools["well_bore_update_by_id"].InputSchema);
        Assert.That(RequiredNames(root), Is.EquivalentTo(new[] { "wellBore", "id" }));
        Assert.That(Property(root, "id")["format"]?.GetValue<string>(), Is.EqualTo("uuid"));
        Assert.That(Property(root, "id")["description"]?.GetValue<string>(), Does.Contain("wellBore.MetaInfo.ID"));
    }

    [TestCase("well_bore_get_by_id")]
    [TestCase("well_bore_get_all_by_well_id")]
    [TestCase("well_bore_get_all_by_rig_id")]
    [TestCase("well_bore_get_all_by_parent_id")]
    public async Task Identifier_tools_require_their_identifier(string toolName)
    {
        JsonObject? response = await _tools[toolName].InvokeAsync(new JsonObject(), CancellationToken.None) as JsonObject;
        Assert.That(response?["status"]?.GetValue<int>(), Is.EqualTo(400));
    }

    [Test]
    public async Task Create_tool_requires_a_request_body()
    {
        JsonObject? response = await _tools["well_bore_create"].InvokeAsync(new JsonObject(), CancellationToken.None) as JsonObject;
        Assert.That(response?["status"]?.GetValue<int>(), Is.EqualTo(400));
    }

    private static JsonObject RequireObject(JsonNode? node)
    {
        Assert.That(node, Is.TypeOf<JsonObject>());
        return (JsonObject)node!;
    }

    private static JsonObject Property(JsonObject schema, string name)
    {
        JsonObject properties = RequireObject(schema["properties"]);
        return RequireObject(properties[name]);
    }

    private static string[] PropertyNames(JsonObject schema)
    {
        return RequireObject(schema["properties"]).Select(property => property.Key).ToArray();
    }

    private static string[] RequiredNames(JsonObject schema)
    {
        Assert.That(schema["required"], Is.TypeOf<JsonArray>());
        return ((JsonArray)schema["required"]!).Select(node => node!.GetValue<string>()).ToArray();
    }
}
