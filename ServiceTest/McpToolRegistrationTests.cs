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
        ["SearchWellBores"] = "well_bore_search",
        ["ValidateWellBoreExternalReferences"] = "well_bore_validate_external_references",
        ["AuditWellBoreExternalReferences"] = "well_bore_audit_external_references",
        ["BatchExportWellBores"] = "well_bore_batch_export",
        ["BatchRestoreWellBores"] = "well_bore_batch_restore",
        ["GetAllWellBoreByWellID"] = "well_bore_get_all_by_well_id",
        ["GetAllWellBoreByRigId"] = "well_bore_get_all_by_rig_id",
        ["GetAllWellBoreByParentWellBoreId"] = "well_bore_get_all_by_parent_id",
        ["GetAllSidetrackedWellBore"] = "well_bore_get_all_sidetracked",
        ["PostWellBore"] = "well_bore_create",
        ["PutWellBoreById"] = "well_bore_update_by_id",
        ["PutWellBoreDetails"] = "well_bore_details_update",
        ["PutWellBoreTopology"] = "well_bore_topology_update",
        ["PostWellBoreIdentityAssignment"] = "well_bore_identity_assignment_add",
        ["PutWellBoreIdentityAssignment"] = "well_bore_identity_assignment_update_by_id",
        ["DeleteWellBoreIdentityAssignment"] = "well_bore_identity_assignment_delete_by_id",
        ["PostWellBoreFeatureAssignment"] = "well_bore_feature_assignment_add",
        ["PutWellBoreFeatureAssignment"] = "well_bore_feature_assignment_update_by_id",
        ["DeleteWellBoreFeatureAssignment"] = "well_bore_feature_assignment_delete_by_id",
        ["DeleteWellBoreById"] = "well_bore_delete_by_id",
        ["GetAllWellBoreIdentityId"] = "well_bore_identity_get_all_ids",
        ["GetAllWellBoreIdentityMetaInfo"] = "well_bore_identity_get_all_meta_info",
        ["GetWellBoreIdentityById"] = "well_bore_identity_get_by_id",
        ["GetAllWellBoreIdentity"] = "well_bore_identity_get_all",
        ["PostWellBoreIdentity"] = "well_bore_identity_create",
        ["PutWellBoreIdentityById"] = "well_bore_identity_update_by_id",
        ["DeleteWellBoreIdentityById"] = "well_bore_identity_delete_by_id",
        ["GetAllWellBoreFeatureCategoryId"] = "well_bore_feature_category_get_all_ids",
        ["GetAllWellBoreFeatureCategoryMetaInfo"] = "well_bore_feature_category_get_all_meta_info",
        ["GetWellBoreFeatureCategoryById"] = "well_bore_feature_category_get_by_id",
        ["GetAllWellBoreFeatureCategory"] = "well_bore_feature_category_get_all",
        ["PostWellBoreFeatureCategory"] = "well_bore_feature_category_create",
        ["PutWellBoreFeatureCategoryById"] = "well_bore_feature_category_update_by_id",
        ["DeleteWellBoreFeatureCategoryById"] = "well_bore_feature_category_delete_by_id"
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
        var endpoints = new[] { typeof(WellBoreController), typeof(WellBoreIdentityController), typeof(WellBoreFeatureCategoryController) }
            .SelectMany(type => type.GetMethods())
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
            "TieInPointAlongHoleDepth", "SidetrackType",
            "WellBoreIdentityAssignments", "WellBoreFeatureAssignments"
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
        Assert.That(Property(wellBore, "SidetrackType")["deprecated"]?.GetValue<bool>(), Is.True);
    }

    [Test]
    public void Update_tool_schema_requires_matching_id_and_wellbore_arguments()
    {
        JsonObject root = RequireObject(_tools["well_bore_update_by_id"].InputSchema);
        Assert.That(RequiredNames(root), Is.EquivalentTo(new[] { "wellBore", "id", "expectedModifiedUtc" }));
        Assert.That(Property(root, "id")["format"]?.GetValue<string>(), Is.EqualTo("uuid"));
        Assert.That(Property(root, "expectedModifiedUtc")["format"]?.GetValue<string>(), Is.EqualTo("date-time"));
        Assert.That(Property(root, "id")["description"]?.GetValue<string>(), Does.Contain("wellBore.MetaInfo.ID"));
    }

    [Test]
    public void Every_tool_publishes_strict_input_output_and_behavior_metadata()
    {
        foreach (IMcpTool tool in _tools.Values)
        {
            JsonObject input = RequireObject(tool.InputSchema);
            JsonObject output = RequireObject(tool.OutputSchema);
            Assert.Multiple(() =>
            {
                Assert.That(input["type"]?.GetValue<string>(), Is.EqualTo("object"), tool.Name);
                Assert.That(input["additionalProperties"]?.GetValue<bool>(), Is.False, tool.Name);
                Assert.That(output["type"]?.GetValue<string>(), Is.EqualTo("object"), tool.Name);
                Assert.That(output["additionalProperties"]?.GetValue<bool>(), Is.False, tool.Name);
                Assert.That(tool.Behavior.Title, Is.Not.Empty, tool.Name);
            });
        }
    }

    [Test]
    public void Destructive_and_read_only_hints_match_tool_semantics()
    {
        Assert.That(_tools["well_bore_get_by_id"].Behavior.ReadOnlyHint, Is.True);
        Assert.That(_tools["well_bore_search"].Behavior.ReadOnlyHint, Is.True);
        Assert.That(_tools["well_bore_validate_external_references"].Behavior.ReadOnlyHint, Is.True);
        Assert.That(_tools["well_bore_audit_external_references"].Behavior.ReadOnlyHint, Is.True);
        Assert.That(_tools["well_bore_delete_by_id"].Behavior.DestructiveHint, Is.True);
        Assert.That(_tools["well_bore_batch_restore"].Behavior.DestructiveHint, Is.True);
        Assert.That(_tools["well_bore_details_update"].Behavior.ReadOnlyHint, Is.False);
    }

    [TestCase("well_bore_delete_by_id")]
    [TestCase("well_bore_identity_delete_by_id")]
    [TestCase("well_bore_feature_category_delete_by_id")]
    public void Delete_tools_require_a_concurrency_revision(string toolName)
    {
        JsonObject root = RequireObject(_tools[toolName].InputSchema);
        Assert.That(RequiredNames(root), Is.EquivalentTo(new[] { "id", "expectedModifiedUtc" }));
        Assert.That(Property(root, "expectedModifiedUtc")["format"]?.GetValue<string>(), Is.EqualTo("date-time"));
    }

    [TestCase("wellId", "not-a-uuid")]
    [TestCase("modifiedFromUtc", "not-a-timestamp")]
    public async Task Search_rejects_malformed_optional_filters(string key, string value)
    {
        JsonObject? response = await _tools["well_bore_search"]
            .InvokeAsync(new JsonObject { [key] = value }, CancellationToken.None) as JsonObject;
        Assert.That(response?["status"]?.GetValue<int>(), Is.EqualTo(400));
    }

    [Test]
    public void External_reference_tools_publish_bounded_strict_contracts()
    {
        JsonObject validateInput = RequireObject(_tools["well_bore_validate_external_references"].InputSchema);
        Assert.That(RequiredNames(validateInput), Is.EquivalentTo(new[] { "wellBoreId" }));

        JsonObject auditInput = RequireObject(_tools["well_bore_audit_external_references"].InputSchema);
        JsonObject request = Property(auditInput, "request");
        Assert.That(RequiredNames(request), Is.EquivalentTo(new[] { "Scope" }));
        Assert.That(Property(request, "WellBoreIDs")["uniqueItems"]?.GetValue<bool>(), Is.True);
        Assert.That(Property(request, "Limit")["maximum"]?.GetValue<int>(), Is.EqualTo(100));

        JsonObject validateOutput = RequireObject(_tools["well_bore_validate_external_references"].OutputSchema);
        JsonObject validation = Property(validateOutput, "data");
        Assert.That(PropertyNames(validation), Does.Contain("WellExists"));
        Assert.That(PropertyNames(validation), Does.Contain("RigExists"));
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

    [Test]
    public void Batch_tools_publish_strict_versioned_schemas()
    {
        JsonObject exportRoot = RequireObject(_tools["well_bore_batch_export"].InputSchema);
        JsonObject exportRequest = Property(exportRoot, "request");
        Assert.That(RequiredNames(exportRequest), Is.EquivalentTo(new[] { "Scope" }));
        Assert.That(Property(exportRequest, "WellBoreIDs")["uniqueItems"]?.GetValue<bool>(), Is.True);

        JsonObject restoreRoot = RequireObject(_tools["well_bore_batch_restore"].InputSchema);
        JsonObject restoreRequest = Property(restoreRoot, "request");
        Assert.That(RequiredNames(restoreRequest),
            Is.EquivalentTo(new[] { "ConflictPolicy", "CatalogPolicy", "Document" }));
        JsonObject document = Property(restoreRequest, "Document");
        Assert.That(Property(document, "FormatIdentifier")["const"]?.GetValue<string>(),
            Is.EqualTo("OSDC.Drilling.WellBore.BatchExport"));
        Assert.That(Property(document, "SchemaVersion")["const"]?.GetValue<int>(), Is.EqualTo(1));
        Assert.That(Property(document, "WellBores")["minItems"]?.GetValue<int>(), Is.EqualTo(1));
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
