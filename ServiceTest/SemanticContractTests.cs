using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using NUnit.Framework;
using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
using OSDC.DotnetLibraries.General.Math;
using OSDC.Drilling.WellBore.Model;
using OSDC.Drilling.WellBore.Service;
using OSDC.Drilling.WellBore.Service.Mcp;
using OSDC.Drilling.WellBore.Service.Mcp.Tools;
using Swashbuckle.AspNetCore.SwaggerGen;
using Model = OSDC.Drilling.WellBore.Model;

namespace OSDC.Drilling.WellBore.SemanticTests;

public class SemanticContractTests
{
    private const string Extension = SemanticMetadata.ExtensionName;

    private static JsonObject Rest(Type type)
    {
        var options = new SchemaGeneratorOptions { SchemaIdSelector = t => t.FullName! };
        options.SchemaFilters.Add(new SemanticSchemaFilter());
        var generator = new SchemaGenerator(options, new JsonSerializerDataContractResolver(new JsonSerializerOptions()));
        var repository = new SchemaRepository();
        generator.GenerateSchema(type, repository);
        using var text = new StringWriter();
        var writer = new OpenApiJsonWriter(text);
        repository.Schemas[type.FullName!].SerializeAsV3(writer);
        writer.Flush();
        return JsonNode.Parse(text.ToString())!.AsObject();
    }

    private static JsonObject Mcp(string name, bool output = true)
    {
        var services = new ServiceCollection().AddLogging();
        services.AddWellBoreRestMcpTools();
        using var provider = services.BuildServiceProvider();
        var tool = provider.GetServices<IMcpTool>().Single(t => t.Name == name);
        return (output ? tool.OutputSchema : tool.InputSchema).AsObject();
    }

    [Test]
    public void ResourceAndInheritedClassificationsPublishCuratedCatalogue()
    {
        var rest = Rest(typeof(Model.WellBore));
        var mcp = Mcp("well_bore_get_by_id")["properties"]!["data"]!;
        Assert.That(rest[Extension]!["catalogueVersion"]!.GetValue<string>(), Is.EqualTo("0.15.0"));
        Assert.That(JsonNode.DeepEquals(rest[Extension], mcp[Extension]), Is.True);
        var category = Rest(typeof(Model.WellBoreFeatureCategory));
        Assert.That(category[Extension]!["concept"]!.GetValue<string>(), Is.EqualTo(Concepts.FeatureCategory));
        Assert.That(category["properties"]!["IsExclusive"]![Extension]!["concept"]!.GetValue<string>(), Is.EqualTo(Concepts.CategoryExclusivity));
        var assignment = Rest(typeof(Model.WellBoreFeatureAssignment));
        Assert.That(assignment["properties"]!["FromDate"]![Extension]!["role"]!.GetValue<string>(), Is.EqualTo(Concepts.ValidityStart));
        Assert.That(assignment["properties"]!["ToDate"]![Extension]!["role"]!.GetValue<string>(), Is.EqualTo(Concepts.ValidityEnd));
        Assert.That(assignment["properties"]!["FeatureOptionID"]![Extension]!["concept"]!.GetValue<string>(), Is.EqualTo(Concepts.ResourceIdentifier));
    }

    [Test]
    public void EveryPublishedBindingResolvesToReviewedVocabulary()
    {
        var services = new ServiceCollection().AddLogging();
        services.AddWellBoreRestMcpTools();
        using var provider = services.BuildServiceProvider();
        int count = 0;
        void Check(JsonNode? node)
        {
            if (node is JsonObject obj)
            {
                if (obj["catalogue"] != null)
                {
                    if (obj["concept"] != null)
                    {
                        count++;
                        Assert.That(obj["catalogueVersion"]!.GetValue<string>(), Is.EqualTo("0.15.0"));
                        Assert.That(obj["curationStatus"]!.GetValue<string>(), Is.EqualTo("Reviewed"));
                        Assert.That(SemanticCatalogue.Default.Get(obj["concept"]!.GetValue<string>()).Status, Is.EqualTo(CurationStatus.Reviewed));
                    }
                }
                foreach (var child in obj) Check(child.Value);
            }
            else if (node is JsonArray array) foreach (var child in array) Check(child);
        }
        foreach (var tool in provider.GetServices<IMcpTool>()) { Check(tool.InputSchema); Check(tool.OutputSchema); }
        Assert.That(count, Is.GreaterThan(100));
    }


    [Test]
    public void TieInIsParentAlongHoleCoordinateAndRigJobsKeepTheirTimeAndOwnershipSemantics()
    {
        var rest = Rest(typeof(Model.WellBore));
        var mcp = Mcp("well_bore_get_by_id")["properties"]!["data"]!["properties"]!;
        var tie = mcp["TieInPointAlongHoleDepth"]!;
        var bindings = tie[ProviderSemantics.NestedBindingsExtension]!;
        Assert.That(JsonNode.DeepEquals(bindings, rest["properties"]!["TieInPointAlongHoleDepth"]![ProviderSemantics.NestedBindingsExtension]), Is.True);
        var mean = bindings["/GaussianValue/Mean"]!;
        Assert.That(mean["concept"]!.GetValue<string>(), Is.EqualTo(Concepts.TieInAlongHoleDepth));
        Assert.That(mean["reference"]!.GetValue<string>(), Is.EqualTo(Concepts.Wgs84AlongHoleOrigin));
        Assert.That(mean["referenceProfile"]!.GetValue<string>(), Is.EqualTo(SemanticCatalogue.OsdcCanonicalDrilling));
        Assert.That(mean["referenceScope"]!.GetValue<string>(), Is.EqualTo("canonical-storage-and-api"));
        Assert.That(mean["presentationReferencesAllowed"]!.GetValue<bool>(), Is.True);
        Assert.That(mean["referenceDefinition"]!.GetValue<string>(), Does.Contain("intersection"));
        Assert.Throws<InvalidDataException>(() => ProviderSemantics.Metadata(Concepts.TieInAlongHoleDepth, reference: Concepts.Wgs84));
        Assert.That(mean["physicalQuantity"]!["name"]!.GetValue<string>(), Is.EqualTo("DepthDrilling"));
        Assert.That(bindings["/GaussianValue/StandardDeviation"]!["physicalQuantity"]!["name"]!.GetValue<string>(), Is.EqualTo("LengthStandard"));
        Assert.That(bindings["/GaussianValue/StandardDeviation"]!["reference"], Is.Null);
        Assert.That(rest["properties"]!["TieInMeasuredDepthReference"], Is.Null);
        Assert.That(mcp["TieInMeasuredDepthReference"], Is.Null);
        var jobRest = Rest(typeof(Model.RigJob));
        var variants = mcp["RigJobs"]!["items"]!["oneOf"]!.AsArray();
        foreach (var variant in variants)
        {
            var properties = variant!["properties"]!;
            Assert.That(properties["StartDate"]![Extension]!["role"]!.GetValue<string>(), Is.EqualTo(Concepts.RigJobStart));
            Assert.That(properties["EndDate"]![Extension]!["role"]!.GetValue<string>(), Is.EqualTo(Concepts.RigJobEnd));
            Assert.That(JsonNode.DeepEquals(properties["EndDate"]![Extension], jobRest["properties"]!["EndDate"]![Extension]), Is.True);
            var depth = properties["DrillFloorDepth"]!;
            if (properties["DrillFloorDepthSource"]!["const"]!.GetValue<string>() == "Rig")
                Assert.That(depth["type"]!.GetValue<string>(), Is.EqualTo("null"));
            else
            {
                Assert.That(JsonNode.DeepEquals(depth[ProviderSemantics.NestedBindingsExtension], jobRest["properties"]!["DrillFloorDepth"]![ProviderSemantics.NestedBindingsExtension]), Is.True);
                Assert.That(depth["properties"]!["GaussianValue"]!["properties"]!["Mean"]![Extension]!["concept"]!.GetValue<string>(), Is.EqualTo(Concepts.DrillFloorDepth));
            }
        }
        foreach (string scalar in new[] { "Mean", "StandardDeviation", "MinValue", "MaxValue" })
            Assert.That(JsonNode.DeepEquals(bindings["/GaussianValue/" + scalar], tie["properties"]!["GaussianValue"]!["properties"]![scalar]![Extension]), Is.True);
    }
}
