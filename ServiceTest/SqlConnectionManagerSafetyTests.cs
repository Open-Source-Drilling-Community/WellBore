using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using OSDC.Drilling.WellBore.Service.Managers;

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

    private static string TemporaryDatabasePath() => Path.Combine(
        TestContext.CurrentContext.WorkDirectory, $"WellBoreSafety_{Guid.NewGuid():N}.db");
}
