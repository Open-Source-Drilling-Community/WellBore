using System.Text.Json;
using System.Text.Json.Nodes;
using NUnit.Framework;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.DotnetLibraries.General.ResourceClassification;
using OSDC.Drilling.WellBore.Model;

namespace OSDC.Drilling.WellBore.ModelTest;

public class ClassificationContractTests
{
    private const string CategoryJson = """
        {"MetaInfo":{"ID":"080d1c26-876d-446f-8ab3-0e151e42ce36","HttpHostName":null,"HttpHostBasePath":null,"HttpEndPoint":null},
        "Name":"Type","IsExclusive":true,"HasValidityPeriod":true,
        "Options":[{"ID":"e48af852-3b03-4daa-bb52-2baf92d9c521","Name":"Option"}],
        "CreationDate":null,"LastModificationDate":null}
        """;

    [Test]
    public void IdentityKeepsItsWireShapeAndExistingInterface()
    {
        const string json = """
            {"ID":"e48af852-3b03-4daa-bb52-2baf92d9c521","IdentityID":null,"Value":"ABC"}
            """;
        var value = JsonSerializer.Deserialize<WellBoreIdentityAssignment>(json)!;
        Assert.That(value, Is.InstanceOf<IIdentityAssignment>());
        Assert.That(value, Is.InstanceOf<IdentityAssignment>());
        Assert.That(JsonNode.DeepEquals(JsonNode.Parse(json), JsonSerializer.SerializeToNode(value)), Is.True);
        Assert.That(new WellBoreIdentity(), Is.InstanceOf<IdentityDefinition>());
    }

    [Test]
    public void WellBoreFeatureRetainsConcreteOptionsAndRoundTripsStoredJson()
    {
        var category = JsonSerializer.Deserialize<WellBoreFeatureCategory>(CategoryJson)!;
        Assert.That(category, Is.InstanceOf<FeatureCategory<WellBoreFeatureOption>>());
        Assert.That(category.Options!.Single(), Is.TypeOf<WellBoreFeatureOption>());
        Assert.That(JsonNode.DeepEquals(JsonNode.Parse(CategoryJson), JsonSerializer.SerializeToNode(category)), Is.True);
        IFeatureCategory contract = category;
        contract.Options = [new FeatureOption { ID = Guid.NewGuid(), Name = "Other" }];
        Assert.That(category.Options!.Single(), Is.TypeOf<WellBoreFeatureOption>());
        Assert.That(category.Options!.Single().Name, Is.EqualTo("Other"));
        contract.Options = null;
        Assert.That(category.Options, Is.Null);
    }

    [Test]
    public void WellBoreFeatureAssignmentRetainsNullableReferencesAndValidityFields()
    {
        const string json = """
            {"ID":"e48af852-3b03-4daa-bb52-2baf92d9c521","FeatureCategoryID":null,"FeatureOptionID":null,"FromDate":null,"ToDate":null}
            """;
        var value = JsonSerializer.Deserialize<WellBoreFeatureAssignment>(json)!;
        Assert.That(value, Is.InstanceOf<IFeatureAssignment>());
        Assert.That(JsonNode.DeepEquals(JsonNode.Parse(json), JsonSerializer.SerializeToNode(value)), Is.True);
    }

}
