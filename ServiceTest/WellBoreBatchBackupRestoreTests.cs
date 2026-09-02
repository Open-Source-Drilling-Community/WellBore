using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.Drilling.WellBore.Model;
using OSDC.Drilling.WellBore.Service;
using OSDC.Drilling.WellBore.Service.Managers;
using System.Reflection;
using System.Text.Json;
using WellBoreModel = OSDC.Drilling.WellBore.Model.WellBore;

namespace OSDC.Drilling.WellBore.ServiceTest;

[TestFixture]
public class WellBoreBatchBackupRestoreTests
{
    [SetUp]
    public void ResetManagers()
    {
        Reset(typeof(WellBoreManager));
        Reset(typeof(WellBoreIdentityManager));
        Reset(typeof(WellBoreFeatureCategoryManager));
    }

    [Test]
    public void Export_SelectedWellBores_PreservesOrderAndIncludesOnlyReferencedCatalogs()
    {
        string path = TempDatabase();
        SqlConnectionManager connections = Manager(path);
        WellBoreIdentityManager identities = WellBoreIdentityManager.GetInstance(NullLogger<WellBoreIdentityManager>.Instance, connections);
        WellBoreFeatureCategoryManager categories = WellBoreFeatureCategoryManager.GetInstance(NullLogger<WellBoreFeatureCategoryManager>.Instance, connections);
        WellBoreIdentity usedIdentity = Identity("UsedIdentity");
        WellBoreIdentity unusedIdentity = Identity("UnusedIdentity");
        WellBoreFeatureCategory category = Category("Purpose", "Producer");
        Assert.That(identities.AddWellBoreIdentity(usedIdentity), Is.True);
        Assert.That(identities.AddWellBoreIdentity(unusedIdentity), Is.True);
        Assert.That(categories.AddWellBoreFeatureCategory(category), Is.True);
        WellBoreManager wellBores = WellBoreManager.GetInstance(NullLogger<WellBoreManager>.Instance, connections);
        WellBoreModel first = WellBore("First", usedIdentity, category);
        WellBoreModel second = WellBore("Second", usedIdentity, category);
        Assert.That(wellBores.AddWellBore(first), Is.True);
        Assert.That(wellBores.AddWellBore(second), Is.True);

        WellBoreBatchExportOutcome outcome = wellBores.ExportBatch(new WellBoreBatchExportRequest
        {
            Scope = WellBoreBatchExportScope.Selected,
            WellBoreIDs = [second.MetaInfo!.ID, first.MetaInfo!.ID]
        });

        Assert.That(outcome.IsSuccess, Is.True);
        Assert.That(outcome.Document!.WellBores.Select(value => value.MetaInfo!.ID),
            Is.EqualTo(new[] { second.MetaInfo!.ID, first.MetaInfo!.ID }));
        Assert.That(outcome.Document.CatalogDependencies.Identities.Select(value => value.Name), Is.EqualTo(new[] { "UsedIdentity" }));
        Assert.That(outcome.Document.CatalogDependencies.FeatureCategories, Has.Count.EqualTo(1));
        Assert.That(outcome.Document.CatalogDependencies.FeatureCategories[0].Options, Has.Count.EqualTo(1));
    }

    [Test]
    public void Restore_MapOrCreateMissing_RewritesCatalogReferencesAndCommitsAtomically()
    {
        string path = TempDatabase();
        SqlConnectionManager connections = Manager(path);
        WellBoreIdentity sourceIdentity = Identity("PortableIdentity");
        WellBoreFeatureCategory sourceCategory = Category("PortableCategory", "PortableOption");
        WellBoreModel sourceWell = WellBore("PortableWell", sourceIdentity, sourceCategory);
        WellBoreBatchExportDocument document = Document(sourceWell, sourceIdentity, sourceCategory);

        using SqliteConnection connection = connections.GetConnection()!;
        WellBoreBatchRestoreOutcome outcome = WellBoreBatchRestorer.Restore(connection, new WellBoreBatchRestoreRequest
        {
            ConflictPolicy = WellBoreBatchRestoreConflictPolicy.FailIfExists,
            CatalogPolicy = WellBoreBatchCatalogRestorePolicy.MapOrCreateMissing,
            Document = document
        }, DateTimeOffset.UtcNow);

        Assert.That(outcome.IsSuccess, Is.True);
        Assert.That(outcome.Response!.CreatedCount, Is.EqualTo(1));
        Assert.That(outcome.Response.CreatedCatalogDefinitionCount, Is.EqualTo(2));
        Assert.That(outcome.Response.CreatedCatalogOptionCount, Is.EqualTo(1));
        Guid localIdentity = outcome.Response.CatalogMappings.Single(value => value.Catalog == "Identity").LocalID;
        Guid localCategory = outcome.Response.CatalogMappings.Single(value => value.Catalog == "FeatureCategory").LocalID;
        Guid localOption = outcome.Response.CatalogMappings.Single(value => value.Catalog == "FeatureOption").LocalID;
        Assert.That(localIdentity, Is.Not.EqualTo(sourceIdentity.MetaInfo!.ID));
        WellBoreModel restored = ReadWell(path, sourceWell.MetaInfo!.ID);
        Assert.That(restored.WellBoreIdentityAssignments![0].IdentityID, Is.EqualTo(localIdentity));
        Assert.That(restored.WellBoreFeatureAssignments![0].FeatureCategoryID, Is.EqualTo(localCategory));
        Assert.That(restored.WellBoreFeatureAssignments[0].FeatureOptionID, Is.EqualTo(localOption));
    }

    [Test]
    public void Restore_Collision_RollsBackPendingCatalogCreationAndPreservesExistingWell()
    {
        string path = TempDatabase();
        SqlConnectionManager connections = Manager(path);
        long featureCountBefore = Count(path, "WellBoreFeatureCategoryTable");
        WellBoreManager manager = WellBoreManager.GetInstance(NullLogger<WellBoreManager>.Instance, connections);
        WellBoreModel existing = new() { MetaInfo = new MetaInfo { ID = Guid.NewGuid() }, Name = "Existing" };
        Assert.That(manager.AddWellBore(existing), Is.True);
        string before = ReadWellJson(path, existing.MetaInfo!.ID);
        WellBoreIdentity sourceIdentity = Identity("WouldBeCreated");
        WellBoreFeatureCategory sourceCategory = Category("WouldBeCreatedCategory", "Option");
        WellBoreModel colliding = WellBore("Replacement", sourceIdentity, sourceCategory, existing.MetaInfo.ID);

        using SqliteConnection connection = connections.GetConnection()!;
        WellBoreBatchRestoreOutcome outcome = WellBoreBatchRestorer.Restore(connection, new WellBoreBatchRestoreRequest
        {
            ConflictPolicy = WellBoreBatchRestoreConflictPolicy.FailIfExists,
            CatalogPolicy = WellBoreBatchCatalogRestorePolicy.MapOrCreateMissing,
            Document = Document(colliding, sourceIdentity, sourceCategory)
        }, DateTimeOffset.UtcNow);

        Assert.That(outcome.FailureKind, Is.EqualTo(WellBoreBatchRestoreFailureKind.Conflict));
        Assert.That(ReadWellJson(path, existing.MetaInfo.ID), Is.EqualTo(before));
        Assert.That(Count(path, "WellBoreIdentityTable"), Is.Zero);
        Assert.That(Count(path, "WellBoreFeatureCategoryTable"), Is.EqualTo(featureCountBefore));
    }

    [Test]
    public void LegacyUpgradeThenRestore_PreservesUnrelatedRows()
    {
        string path = TempDatabase();
        Guid legacyId = Guid.NewGuid();
        CreateLegacyDatabase(path, legacyId);
        string legacyBefore = ReadWellJson(path, legacyId);
        SqlConnectionManager connections = Manager(path);
        WellBoreModel restored = new() { MetaInfo = new MetaInfo { ID = Guid.NewGuid() }, Name = "Restored" };
        WellBoreBatchExportDocument document = new() { ExportedAtUtc = DateTimeOffset.UtcNow, WellBores = [restored] };

        using SqliteConnection connection = connections.GetConnection()!;
        WellBoreBatchRestoreOutcome outcome = WellBoreBatchRestorer.Restore(connection, new WellBoreBatchRestoreRequest
        {
            ConflictPolicy = WellBoreBatchRestoreConflictPolicy.FailIfExists,
            CatalogPolicy = WellBoreBatchCatalogRestorePolicy.MapExisting,
            Document = document
        }, DateTimeOffset.UtcNow);

        Assert.That(outcome.IsSuccess, Is.True);
        Assert.That(ReadWellJson(path, legacyId), Is.EqualTo(legacyBefore));
        Assert.That(Count(path, "WellBoreTable"), Is.EqualTo(2));
    }

    [Test]
    public void Restore_CorruptCatalogDocument_IsRejectedWithoutChangingData()
    {
        string path = TempDatabase();
        SqlConnectionManager connections = Manager(path);
        WellBoreManager manager = WellBoreManager.GetInstance(NullLogger<WellBoreManager>.Instance, connections);
        WellBoreModel existing = new() { MetaInfo = new MetaInfo { ID = Guid.NewGuid() }, Name = "Unchanged" };
        Assert.That(manager.AddWellBore(existing), Is.True);
        string before = ReadWellJson(path, existing.MetaInfo!.ID);
        WellBoreIdentity first = Identity("First");
        WellBoreIdentity duplicate = Identity("Second");
        duplicate.MetaInfo!.ID = first.MetaInfo!.ID;
        WellBoreModel incoming = new()
        {
            MetaInfo = new MetaInfo { ID = Guid.NewGuid() },
            WellBoreIdentityAssignments = [new WellBoreIdentityAssignment { ID = Guid.NewGuid(), IdentityID = first.MetaInfo.ID }]
        };

        using SqliteConnection connection = connections.GetConnection()!;
        WellBoreBatchRestoreOutcome outcome = WellBoreBatchRestorer.Restore(connection, new WellBoreBatchRestoreRequest
        {
            ConflictPolicy = WellBoreBatchRestoreConflictPolicy.FailIfExists,
            CatalogPolicy = WellBoreBatchCatalogRestorePolicy.MapExisting,
            Document = new WellBoreBatchExportDocument
            {
                ExportedAtUtc = DateTimeOffset.UtcNow,
                CatalogDependencies = new WellBoreBatchCatalogDependencies { Identities = [first, duplicate] },
                WellBores = [incoming]
            }
        }, DateTimeOffset.UtcNow);

        Assert.That(outcome.FailureKind, Is.EqualTo(WellBoreBatchRestoreFailureKind.InvalidRequest));
        Assert.That(ReadWellJson(path, existing.MetaInfo.ID), Is.EqualTo(before));
        Assert.That(Count(path, "WellBoreTable"), Is.EqualTo(1));
    }

    [Test]
    public void Restore_InvalidAssignment_RollsBackCatalogCreationAndWellBoreWrites()
    {
        string path = TempDatabase();
        SqlConnectionManager connections = Manager(path);
        long featureCountBefore = Count(path, "WellBoreFeatureCategoryTable");
        WellBoreIdentity identity = Identity("PortableIdentity");
        WellBoreFeatureCategory category = Category("PortableCategory", "PortableOption");
        WellBoreModel incoming = WellBore("InvalidAssignment", identity, category);
        incoming.WellBoreIdentityAssignments![0].Value = " ";

        using SqliteConnection connection = connections.GetConnection()!;
        WellBoreBatchRestoreOutcome outcome = WellBoreBatchRestorer.Restore(connection, new WellBoreBatchRestoreRequest
        {
            ConflictPolicy = WellBoreBatchRestoreConflictPolicy.FailIfExists,
            CatalogPolicy = WellBoreBatchCatalogRestorePolicy.MapOrCreateMissing,
            Document = Document(incoming, identity, category)
        }, DateTimeOffset.UtcNow);

        Assert.That(outcome.FailureKind, Is.EqualTo(WellBoreBatchRestoreFailureKind.InvalidRequest));
        Assert.That(Count(path, "WellBoreTable"), Is.Zero);
        Assert.That(Count(path, "WellBoreIdentityTable"), Is.Zero);
        Assert.That(Count(path, "WellBoreFeatureCategoryTable"), Is.EqualTo(featureCountBefore));
    }

    private static WellBoreIdentity Identity(string name) => new() { MetaInfo = new MetaInfo { ID = Guid.NewGuid() }, Name = name };
    private static WellBoreFeatureCategory Category(string name, string option) => new()
    {
        MetaInfo = new MetaInfo { ID = Guid.NewGuid() }, Name = name, IsExclusive = true,
        HasValidityPeriod = true, Options = [new WellBoreFeatureOption { ID = Guid.NewGuid(), Name = option }]
    };
    private static WellBoreModel WellBore(string name, WellBoreIdentity identity, WellBoreFeatureCategory category, Guid? id = null) => new()
    {
        MetaInfo = new MetaInfo { ID = id ?? Guid.NewGuid() }, Name = name,
        WellBoreIdentityAssignments = [new WellBoreIdentityAssignment { ID = Guid.NewGuid(), IdentityID = identity.MetaInfo!.ID, Value = "value" }],
        WellBoreFeatureAssignments = [new WellBoreFeatureAssignment { ID = Guid.NewGuid(), FeatureCategoryID = category.MetaInfo!.ID, FeatureOptionID = category.Options![0].ID }]
    };
    private static WellBoreBatchExportDocument Document(WellBoreModel wellBore, WellBoreIdentity identity, WellBoreFeatureCategory category) => new()
    {
        ExportedAtUtc = DateTimeOffset.UtcNow,
        CatalogDependencies = new WellBoreBatchCatalogDependencies { Identities = [identity], FeatureCategories = [category] },
        WellBores = [wellBore]
    };
    private static string TempDatabase() => Path.Combine(TestContext.CurrentContext.WorkDirectory, $"WellBoreBatch_{Guid.NewGuid():N}.db");
    private static SqlConnectionManager Manager(string path) => new($"Data Source={path}", NullLogger<SqlConnectionManager>.Instance);

    private static WellBoreModel ReadWell(string path, Guid id) => JsonSerializer.Deserialize<WellBoreModel>(ReadWellJson(path, id), JsonSettings.Options)!;
    private static string ReadWellJson(string path, Guid id)
    {
        using SqliteConnection connection = new($"Data Source={path}"); connection.Open();
        using SqliteCommand command = connection.CreateCommand(); command.CommandText = "SELECT WellBore FROM WellBoreTable WHERE ID=$id";
        command.Parameters.AddWithValue("$id", id.ToString()); return (string)command.ExecuteScalar()!;
    }
    private static long Count(string path, string table)
    {
        using SqliteConnection connection = new($"Data Source={path}"); connection.Open();
        using SqliteCommand command = connection.CreateCommand(); command.CommandText = $"SELECT COUNT(*) FROM {table}";
        return Convert.ToInt64(command.ExecuteScalar());
    }
    private static void CreateLegacyDatabase(string path, Guid id)
    {
        using SqliteConnection connection = new($"Data Source={path}"); connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE WellBoreTable (ID text primary key,MetaInfo text,WellID text,RigID text,IsSidetrack bool,ParentWellBoreID text,WellBore text); CREATE UNIQUE INDEX WellBoreTableIndex ON WellBoreTable(ID); INSERT INTO WellBoreTable VALUES ($id,$meta,NULL,NULL,0,NULL,'{" + "\"MetaInfo\":{\"ID\":\"" + id + "\"},\"Name\":\"Legacy\"}')";
        command.Parameters.AddWithValue("$id", id.ToString());
        command.Parameters.AddWithValue("$meta", JsonSerializer.Serialize(new MetaInfo { ID = id }, JsonSettings.Options));
        command.ExecuteNonQuery();
    }
    private static void Reset(Type type) => type.GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, null);
}
