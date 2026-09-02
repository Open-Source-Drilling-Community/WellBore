using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.Drilling.WellBore.Model;
using OSDC.Drilling.WellBore.Service;
using OSDC.Drilling.WellBore.Service.Controllers;
using OSDC.Drilling.WellBore.Service.Managers;
using WellBoreModel = OSDC.Drilling.WellBore.Model.WellBore;

namespace OSDC.Drilling.WellBore.ServiceTest;

[TestFixture]
public sealed class WellBoreContractStrengtheningTests
{
    private SqlConnectionManager _connections = null!;
    private WellBoreController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        foreach (Type type in new[] { typeof(WellBoreManager), typeof(WellBoreIdentityManager), typeof(WellBoreFeatureCategoryManager) })
            type.GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, null);
        ILoggerFactory factory = LoggerFactory.Create(builder => builder.ClearProviders());
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"WellBoreContract_{Guid.NewGuid():N}.db");
        _connections = new SqlConnectionManager($"Data Source={path}", factory.CreateLogger<SqlConnectionManager>());
        _controller = new WellBoreController(factory.CreateLogger<WellBoreManager>(), _connections);
    }

    [Test]
    public void Full_update_rejects_stale_revision_without_overwriting_first_writer()
    {
        var value = NewWellBore();
        Assert.That(_controller.PostWellBore(value), Is.TypeOf<OkResult>());
        DateTimeOffset original = value.LastModificationDate!.Value;
        value.Name = "First writer";
        Assert.That(_controller.PutWellBoreById(value.MetaInfo!.ID, original, value), Is.TypeOf<OkResult>());

        var stale = NewWellBore(value.MetaInfo.ID);
        stale.Name = "Stale writer";
        Assert.That(_controller.PutWellBoreById(stale.MetaInfo!.ID, original, stale), Is.TypeOf<ConflictObjectResult>());
        WellBoreModel stored = Read(value.MetaInfo.ID);
        Assert.That(stored.Name, Is.EqualTo("First writer"));
        Assert.That(stored.LastModificationDate, Is.GreaterThan(original));
    }

    [Test]
    public void Details_update_and_search_are_surgical_and_deterministic()
    {
        WellBoreModel first = NewWellBore(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        WellBoreModel second = NewWellBore(Guid.Parse("00000000-0000-0000-0000-000000000002"));
        first.Name = "Alpha Main";
        second.Name = "Alpha Lateral";
        Assert.That(_controller.PostWellBore(first), Is.TypeOf<OkResult>());
        Assert.That(_controller.PostWellBore(second), Is.TypeOf<OkResult>());
        Guid? originalRig = first.RigID;

        ActionResult details = _controller.PutWellBoreDetails(first.MetaInfo!.ID, first.LastModificationDate!.Value,
            new WellBoreDetailsUpdate { Name = "Renamed Alpha", Description = "Only details changed" });
        WellBoreModel updated = (WellBoreModel)((OkObjectResult)details).Value!;
        Assert.That(updated.RigID, Is.EqualTo(originalRig));

        ActionResult<WellBoreSearchResult> result = _controller.SearchWellBores(offset: 1, limit: 1, name: "alpha");
        var page = (WellBoreSearchResult)((OkObjectResult)result.Result!).Value!;
        Assert.Multiple(() =>
        {
            Assert.That(page.Total, Is.EqualTo(2));
            Assert.That(page.Items, Has.Count.EqualTo(1));
            Assert.That(page.Items[0].MetaInfo!.ID, Is.EqualTo(second.MetaInfo!.ID));
        });
    }

    [Test]
    public void Identity_assignment_mutation_returns_new_revision_and_rejects_stale_follow_up()
    {
        WellBoreIdentity identity = WellBoreIdentityManager.GetInstance(
            LoggerFactory.Create(builder => builder.ClearProviders()).CreateLogger<WellBoreIdentityManager>(), _connections)
            .GetAllWellBoreIdentity()!.Cast<WellBoreIdentity>().First();
        WellBoreModel value = NewWellBore();
        Assert.That(_controller.PostWellBore(value), Is.TypeOf<OkResult>());
        DateTimeOffset original = value.LastModificationDate!.Value;
        var assignment = new WellBoreIdentityAssignment { ID = Guid.NewGuid(), IdentityID = identity.MetaInfo!.ID, Value = "External-1" };

        ActionResult addedResult = _controller.PostWellBoreIdentityAssignment(value.MetaInfo!.ID, original, assignment);
        WellBoreModel added = (WellBoreModel)((OkObjectResult)addedResult).Value!;
        Assert.That(added.WellBoreIdentityAssignments, Has.Count.EqualTo(1));
        Assert.That(added.LastModificationDate, Is.GreaterThan(original));
        Assert.That(_controller.DeleteWellBoreIdentityAssignment(value.MetaInfo.ID, assignment.ID, original),
            Is.TypeOf<ConflictObjectResult>());
    }

    [Test]
    public void Parent_relationship_is_validated_and_parent_delete_is_reference_safe()
    {
        WellBoreModel parent = NewWellBore();
        Assert.That(_controller.PostWellBore(parent), Is.TypeOf<OkResult>());
        WellBoreModel child = NewWellBore();
        child.WellID = parent.WellID;
        child.IsSidetrack = true;
        child.ParentWellBoreID = parent.MetaInfo!.ID;
        child.SidetrackType = SidetrackType.Technical;
        Assert.That(_controller.PostWellBore(child), Is.TypeOf<OkResult>());
        Assert.That(_controller.DeleteWellBoreById(parent.MetaInfo.ID, parent.LastModificationDate!.Value),
            Is.TypeOf<ConflictObjectResult>());

        WellBoreModel orphan = NewWellBore();
        orphan.IsSidetrack = true;
        orphan.ParentWellBoreID = Guid.NewGuid();
        orphan.SidetrackType = SidetrackType.Production;
        Assert.That(_controller.PostWellBore(orphan), Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public void Legacy_document_without_timestamps_gets_stable_non_destructive_revision()
    {
        WellBoreModel legacy = NewWellBore();
        legacy.CreationDate = null;
        legacy.LastModificationDate = null;
        using (SqliteConnection? connection = _connections.GetConnection())
        using (SqliteCommand command = connection!.CreateCommand())
        {
            command.CommandText = "INSERT INTO WellBoreTable (ID,MetaInfo,WellID,RigID,IsSidetrack,ParentWellBoreID,WellBore) VALUES ($id,$meta,$well,$rig,0,NULL,$document)";
            command.Parameters.AddWithValue("$id", legacy.MetaInfo!.ID.ToString());
            command.Parameters.AddWithValue("$meta", JsonSerializer.Serialize(legacy.MetaInfo, JsonSettings.Options));
            command.Parameters.AddWithValue("$well", legacy.WellID!.Value.ToString());
            command.Parameters.AddWithValue("$rig", legacy.RigID!.Value.ToString());
            command.Parameters.AddWithValue("$document", JsonSerializer.Serialize(legacy, JsonSettings.Options));
            Assert.That(command.ExecuteNonQuery(), Is.EqualTo(1));
        }
        WellBoreModel normalized = Read(legacy.MetaInfo.ID);
        Assert.That(normalized.LastModificationDate, Is.EqualTo(DateTimeOffset.UnixEpoch));
        Assert.That(_controller.PutWellBoreDetails(legacy.MetaInfo.ID, DateTimeOffset.UnixEpoch,
            new WellBoreDetailsUpdate { Name = "Legacy updated", Description = normalized.Description }), Is.TypeOf<OkObjectResult>());
        Assert.That(Read(legacy.MetaInfo.ID).Name, Is.EqualTo("Legacy updated"));
    }

    private WellBoreModel Read(Guid id) => (WellBoreModel)((OkObjectResult)_controller.GetWellBoreById(id).Result!).Value!;

    private static WellBoreModel NewWellBore(Guid? id = null) => new()
    {
        MetaInfo = new MetaInfo { ID = id ?? Guid.NewGuid() },
        Name = "Test WellBore",
        Description = "Contract test",
        WellID = Guid.NewGuid(),
        RigID = Guid.NewGuid(),
        IsSidetrack = false,
        SidetrackType = SidetrackType.Undefined
    };
}
