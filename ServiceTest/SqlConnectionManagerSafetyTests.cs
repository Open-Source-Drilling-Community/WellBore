using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.Drilling.WellBore.Model;
using OSDC.Drilling.WellBore.Service;
using OSDC.Drilling.WellBore.Service.Managers;
using System.Text.Json;

namespace OSDC.Drilling.WellBore.ServiceTest;

[TestFixture]
public sealed class SqlConnectionManagerSafetyTests
{
    private ILogger<SqlConnectionManager> _logger = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        ILoggerFactory factory = LoggerFactory.Create(builder => builder.ClearProviders());
        _logger = factory.CreateLogger<SqlConnectionManager>();
    }

    [Test]
    public void Unexpected_schema_aborts_startup_without_dropping_data()
    {
        string path = TemporaryDatabasePath();
        string connectionString = $"Data Source={path}";
        try
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = "CREATE TABLE Unexpected (ID TEXT PRIMARY KEY, Payload TEXT); " +
                                      "INSERT INTO Unexpected (ID, Payload) VALUES ('marker', 'preserve-me');";
                command.ExecuteNonQuery();
            }

            Assert.Throws<InvalidOperationException>(() => new SqlConnectionManager(connectionString, _logger));

            using var verification = new SqliteConnection(connectionString);
            verification.Open();
            using SqliteCommand verify = verification.CreateCommand();
            verify.CommandText = "SELECT Payload FROM Unexpected WHERE ID='marker'";
            Assert.That(verify.ExecuteScalar(), Is.EqualTo("preserve-me"));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Test]
    public void Legacy_schema_is_migrated_additively_and_preserves_existing_rows()
    {
        string path = TemporaryDatabasePath();
        string connectionString = $"Data Source={path}";
        try
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = "CREATE TABLE WellBoreTable (ID text primary key, MetaInfo text, WellID text, RigID text, IsSidetrack bool, ParentWellBoreID text, WellBore text); " +
                                      "CREATE UNIQUE INDEX WellBoreTableIndex ON WellBoreTable (ID); " +
                                      "INSERT INTO WellBoreTable (ID, MetaInfo, WellBore) VALUES ('marker', '{\"ID\":\"marker\"}', '{\"Name\":\"preserve-me\"}');";
                command.ExecuteNonQuery();
            }

            _ = new SqlConnectionManager(connectionString, _logger);

            using var verification = new SqliteConnection(connectionString);
            verification.Open();
            using SqliteCommand verify = verification.CreateCommand();
            verify.CommandText = "SELECT WellBore FROM WellBoreTable WHERE ID='marker'; " +
                                 "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name IN ('WellBoreIdentityTable','WellBoreFeatureCategoryTable');";
            using SqliteDataReader reader = verify.ExecuteReader();
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.GetString(0), Does.Contain("preserve-me"));
            Assert.That(reader.NextResult(), Is.True);
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.GetInt64(0), Is.EqualTo(2));
            using SqliteCommand version = verification.CreateCommand();
            version.CommandText = "PRAGMA user_version";
            Assert.That(Convert.ToInt32(version.ExecuteScalar()), Is.EqualTo(SqlConnectionManager.CURRENT_SCHEMA_VERSION));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Test]
    public void Reopening_expected_schema_preserves_existing_rows()
    {
        string path = TemporaryDatabasePath();
        string connectionString = $"Data Source={path}";
        try
        {
            _ = new SqlConnectionManager(connectionString, _logger);
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = "INSERT INTO WellBoreTable (ID, MetaInfo, WellBore) VALUES ('marker', '{}', '{}')";
                command.ExecuteNonQuery();
            }

            _ = new SqlConnectionManager(connectionString, _logger);

            using var verification = new SqliteConnection(connectionString);
            verification.Open();
            using SqliteCommand verify = verification.CreateCommand();
            verify.CommandText = "SELECT COUNT(*) FROM WellBoreTable WHERE ID='marker'";
            Assert.That(Convert.ToInt64(verify.ExecuteScalar()), Is.EqualTo(1));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Test]
    public void Version1_sidetrack_values_are_backfilled_transactionally_without_losing_document_data()
    {
        string path = TemporaryDatabasePath();
        string connectionString = $"Data Source={path}";
        Guid id = Guid.NewGuid();
        DateTimeOffset originalRevision = DateTimeOffset.UtcNow.AddDays(-1);
        try
        {
            _ = new SqlConnectionManager(connectionString, _logger);
            var legacy = new Model.WellBore
            {
                MetaInfo = new MetaInfo { ID = id }, Name = "preserve-name", Description = "preserve-description",
                IsSidetrack = true, ParentWellBoreID = Guid.NewGuid(), SidetrackType = SidetrackType.Production,
                CreationDate = originalRevision.AddDays(-1), LastModificationDate = originalRevision
            };
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = "DELETE FROM WellBoreFeatureCategoryTable WHERE Name='SidetrackClassification'; " +
                                      "INSERT INTO WellBoreTable (ID,MetaInfo,IsSidetrack,ParentWellBoreID,WellBore) VALUES ($id,$meta,1,$parent,$document); " +
                                      "PRAGMA user_version = 1;";
                command.Parameters.AddWithValue("$id", id.ToString());
                command.Parameters.AddWithValue("$meta", JsonSerializer.Serialize(legacy.MetaInfo, JsonSettings.Options));
                command.Parameters.AddWithValue("$parent", legacy.ParentWellBoreID.Value.ToString());
                command.Parameters.AddWithValue("$document", JsonSerializer.Serialize(legacy, JsonSettings.Options));
                command.ExecuteNonQuery();
            }

            _ = new SqlConnectionManager(connectionString, _logger);

            using var verification = new SqliteConnection(connectionString);
            verification.Open();
            WellBoreFeatureCategory category = ReadCategory(verification);
            WellBoreFeatureOption production = category.Options!.Single(value => value.Name == "Production");
            using SqliteCommand read = verification.CreateCommand();
            read.CommandText = "SELECT WellBore FROM WellBoreTable WHERE ID=$id";
            read.Parameters.AddWithValue("$id", id.ToString());
            Model.WellBore migrated = JsonSerializer.Deserialize<Model.WellBore>((string)read.ExecuteScalar()!, JsonSettings.Options)!;
            Assert.Multiple(() =>
            {
                Assert.That(migrated.Name, Is.EqualTo("preserve-name"));
                Assert.That(migrated.Description, Is.EqualTo("preserve-description"));
                Assert.That(migrated.SidetrackType, Is.EqualTo(SidetrackType.Production));
                Assert.That(migrated.LastModificationDate, Is.GreaterThan(originalRevision));
                Assert.That(migrated.WellBoreFeatureAssignments, Has.Count.EqualTo(1));
                Assert.That(migrated.WellBoreFeatureAssignments![0].FeatureCategoryID, Is.EqualTo(category.MetaInfo!.ID));
                Assert.That(migrated.WellBoreFeatureAssignments[0].FeatureOptionID, Is.EqualTo(production.ID));
            });
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Test]
    public void Version1_sidetrack_migration_rolls_back_catalog_and_documents_on_invalid_data()
    {
        string path = TemporaryDatabasePath();
        string connectionString = $"Data Source={path}";
        Guid validId = Guid.NewGuid();
        const string invalidId = "invalid-document";
        try
        {
            _ = new SqlConnectionManager(connectionString, _logger);
            var legacy = new Model.WellBore
            {
                MetaInfo = new MetaInfo { ID = validId }, Name = "must-remain-unchanged",
                IsSidetrack = true, SidetrackType = SidetrackType.Technical
            };
            string originalDocument = JsonSerializer.Serialize(legacy, JsonSettings.Options);
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = "DELETE FROM WellBoreFeatureCategoryTable WHERE Name='SidetrackClassification'; " +
                                      "INSERT INTO WellBoreTable (ID,MetaInfo,IsSidetrack,WellBore) VALUES ($validId,$meta,1,$validDocument); " +
                                      "INSERT INTO WellBoreTable (ID,MetaInfo,WellBore) VALUES ($invalidId,'{}','not-json'); " +
                                      "PRAGMA user_version = 1;";
                command.Parameters.AddWithValue("$validId", validId.ToString());
                command.Parameters.AddWithValue("$meta", JsonSerializer.Serialize(legacy.MetaInfo, JsonSettings.Options));
                command.Parameters.AddWithValue("$validDocument", originalDocument);
                command.Parameters.AddWithValue("$invalidId", invalidId);
                command.ExecuteNonQuery();
            }

            Assert.Throws<JsonException>(() => new SqlConnectionManager(connectionString, _logger));

            using var verification = new SqliteConnection(connectionString);
            verification.Open();
            using SqliteCommand verify = verification.CreateCommand();
            verify.CommandText = "SELECT WellBore FROM WellBoreTable WHERE ID=$id";
            verify.Parameters.AddWithValue("$id", validId.ToString());
            Assert.That(verify.ExecuteScalar(), Is.EqualTo(originalDocument));
            using SqliteCommand category = verification.CreateCommand();
            category.CommandText = "SELECT COUNT(*) FROM WellBoreFeatureCategoryTable WHERE Name='SidetrackClassification'";
            Assert.That(Convert.ToInt64(category.ExecuteScalar()), Is.Zero);
            using SqliteCommand version = verification.CreateCommand();
            version.CommandText = "PRAGMA user_version";
            Assert.That(Convert.ToInt32(version.ExecuteScalar()), Is.EqualTo(1));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private static WellBoreFeatureCategory ReadCategory(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT WellBoreFeatureCategory FROM WellBoreFeatureCategoryTable WHERE Name='SidetrackClassification'";
        return JsonSerializer.Deserialize<WellBoreFeatureCategory>((string)command.ExecuteScalar()!, JsonSettings.Options)!;
    }

    private static string TemporaryDatabasePath() => Path.Combine(
        TestContext.CurrentContext.WorkDirectory, $"WellBoreSafety_{Guid.NewGuid():N}.db");
}
