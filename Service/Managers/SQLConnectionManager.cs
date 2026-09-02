using System;
using System.IO;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace OSDC.Drilling.WellBore.Service.Managers
{
    /// <summary>
    /// A manager for the sql database connection, registered as a singleton through dependency injection (see Program.cs)
    /// Existing databases are upgraded through additive, transactional schema migrations.
    /// Malformed or unknown schemas fail startup and are never repaired by dropping user data.
    /// </summary>
    /// <remarks>
    /// SQLite database connection strategy:
    /// - single connection for every access (chosen strategy in the general case)
    ///     each access to the database is performed through isolated connections stored in a List of connections
    ///     > isolation, reliability, fail-safe, thread-safe, but overhead due to opening connections
    /// - shared connection between access
    ///     one connection is opened for the lifetime of the application and used to access database through various web requests and commands 
    ///     > no overhead, but issues with concurrency, single-point of failure, state management
    /// - scoped connection (registering service with AddScoped rather than AddSingleton)
    ///     one connection is opened per web request
    ///     > same problems as with shared connection, but limited to the scope of one webrequest rather than to the whole lifetime of the application
    /// </remarks>
    public class SqlConnectionManager
    {
        private readonly ILogger<SqlConnectionManager> _logger;
        private readonly string _connectionString;
        public static readonly string HOME_DIRECTORY = ".." + Path.DirectorySeparatorChar + "home" + Path.DirectorySeparatorChar;
        public static readonly string DATABASE_FILENAME = "WellBore.db";
        public static readonly string DATE_TIME_FORMAT = "yyyy-MM-dd HH:mm:ss";
        public const int CURRENT_SCHEMA_VERSION = 2;

        // dictionary describing tables format
        private readonly static Dictionary<string, string[]> _tableStructureDict = new Dictionary<string, string[]>()
            {
                { "WellBoreTable", new string[] {
                    "ID text primary key",
                    "MetaInfo text",
                    "WellID text",
                    "RigID text",
                    "IsSidetrack bool",
                    "ParentWellBoreID text",
                    "WellBore text" }
                },
                { "WellBoreIdentityTable", new string[] {
                    "ID text primary key",
                    "MetaInfo text",
                    "Name text",
                    "CreationDate text",
                    "LastModificationDate text",
                    "WellBoreIdentity text" }
                },
                { "WellBoreFeatureCategoryTable", new string[] {
                    "ID text primary key",
                    "MetaInfo text",
                    "Name text",
                    "IsExclusive integer",
                    "HasValidityPeriod integer",
                    "CreationDate text",
                    "LastModificationDate text",
                    "WellBoreFeatureCategory text" }
                }
            };

        public SqlConnectionManager(string connectionString, ILogger<SqlConnectionManager> logger)
        {
            _connectionString = connectionString;
            _logger = logger;
            _logger.LogInformation("SqliteConnectionManager created");
            if (Initialize())
            {
                ManageDataBase();
            }
            else
            {
                _logger.LogInformation("SqliteConnectionManager created");
            }
        }

        public SqliteConnection? GetConnection()
        {
            // a new SQL connection is opened for every transaction, thus ensuring thread-safety and removing unnecessary locks
            var connection = new SqliteConnection(_connectionString);
            if (connection != null)
            {
                connection.Open();
            }
            else
            {
                _logger.LogError("Problem while opening SQLite connection");
            }
            return connection;
        }

        private bool Initialize()
        {
            if (!Directory.Exists(HOME_DIRECTORY))
            {
                _logger.LogInformation("Creating home directory");
                try
                {
                    Directory.CreateDirectory(HOME_DIRECTORY);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Impossible to create home directory for local storage");
                    return false;
                }
            }
            if (Directory.Exists(HOME_DIRECTORY))
            {
                try
                {
                    string databaseFileName = HOME_DIRECTORY + Path.DirectorySeparatorChar + DATABASE_FILENAME;
                    if (File.Exists(databaseFileName))
                    {
                        _logger.LogInformation("Opening database {_databaseFileName}", DATABASE_FILENAME);
                    }
                    else
                    {
                        _logger.LogInformation("Creating database {_databaseFileName}", DATABASE_FILENAME);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Impossible to create {_databaseFileName}", DATABASE_FILENAME);
                    return false;
                }
            }
            else
            {
                _logger.LogError("Home directory for local storage should have been created, check for access");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Applies additive schema migrations. Existing tables and rows are never dropped.
        /// Unexpected or malformed structures fail startup without changing the database.
        /// </summary>
        private void ManageDataBase()
        {
            using var connection = GetConnection();
            if (connection == null)
            {
                throw new InvalidOperationException("Unable to open the WellBore database.");
            }

            List<string> tableNames = [];
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";
                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read()) tableNames.Add(reader.GetString(0));
            }

            using SqliteCommand versionCommand = connection.CreateCommand();
            versionCommand.CommandText = "PRAGMA user_version";
            int schemaVersion = Convert.ToInt32(versionCommand.ExecuteScalar());
            bool seedAllDefaults = schemaVersion == 0;
            if (schemaVersion > CURRENT_SCHEMA_VERSION)
                throw new InvalidOperationException($"WellBore database schema version {schemaVersion} is newer than supported version {CURRENT_SCHEMA_VERSION}.");

            string[] legacyTables = ["WellBoreTable"];
            IEnumerable<string> permittedTables = schemaVersion == 0 ? legacyTables : _tableStructureDict.Keys;
            List<string> unexpected = tableNames.Except(permittedTables, StringComparer.Ordinal).ToList();
            if (unexpected.Count > 0)
                throw new InvalidOperationException($"Unexpected WellBore database tables. No data was changed: [{string.Join(',', unexpected)}].");

            if (tableNames.Contains("WellBoreTable", StringComparer.Ordinal) &&
                !CheckDatabaseStructure(new KeyValuePair<string, string[]>("WellBoreTable", _tableStructureDict["WellBoreTable"])))
                throw new InvalidOperationException("The existing WellBoreTable is malformed. No data was changed.");

            if (schemaVersion == 0)
            {
                using SqliteTransaction transaction = connection.BeginTransaction();
                try
                {
                    foreach (KeyValuePair<string, string[]> table in _tableStructureDict.Where(item => !tableNames.Contains(item.Key, StringComparer.Ordinal)))
                    {
                        using SqliteCommand create = connection.CreateCommand();
                        create.Transaction = transaction;
                        create.CommandText = $"CREATE TABLE {table.Key} ({string.Join(',', table.Value)})";
                        create.ExecuteNonQuery();
                        using SqliteCommand index = connection.CreateCommand();
                        index.Transaction = transaction;
                        index.CommandText = $"CREATE UNIQUE INDEX {table.Key}Index ON {table.Key} (ID)";
                        index.ExecuteNonQuery();
                    }
                    using SqliteCommand setVersion = connection.CreateCommand();
                    setVersion.Transaction = transaction;
                    setVersion.CommandText = "PRAGMA user_version = 1";
                    setVersion.ExecuteNonQuery();
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
                tableNames = _tableStructureDict.Keys.ToList();
                schemaVersion = 1;
            }

            if (schemaVersion == 1)
            {
                WellBoreSidetrackClassification.MigrateVersion1To2(connection, seedAllDefaults);
                schemaVersion = CURRENT_SCHEMA_VERSION;
            }

            List<string> missing = _tableStructureDict.Keys.Except(tableNames, StringComparer.Ordinal).ToList();
            List<string> malformed = _tableStructureDict
                .Where(table => tableNames.Contains(table.Key, StringComparer.Ordinal) && !CheckDatabaseStructure(table))
                .Select(table => table.Key).ToList();
            if (missing.Count > 0 || malformed.Count > 0)
                throw new InvalidOperationException($"Unexpected WellBore database structure. No data was changed. Missing=[{string.Join(',', missing)}], malformed=[{string.Join(',', malformed)}].");
        }

        /// <summary>
        /// Check that expected fields (in tableStructure.Value) exactly match those of the stored database
        /// </summary>
        /// <param name="tableStructure"></param>
        /// <returns>true if the expected fields exactly match fields of the stored database</returns>
        private bool CheckDatabaseStructure(KeyValuePair<string, string[]> tableStructure)
        {
            using var connection = GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                string key = tableStructure.Key;
                StringBuilder sb = new StringBuilder();
                sb.Append($"SELECT * FROM {key}");
                command.CommandText = sb.ToString();
                try
                {
                    using (var reader = command.ExecuteReader(CommandBehavior.SchemaOnly))
                    {
                        var schema = reader.GetSchemaTable();
                        if (tableStructure.Value.Length != schema.Rows.Count)
                            return false; // unexpected number of fields in table
                        foreach (string field in tableStructure.Value)
                        {
                            bool tmpSuccess = false;
                            foreach (DataRow col in schema.Rows)
                            {
                                if (field.Split(" ").ElementAt(0) == col.Field<string>("ColumnName"))
                                {
                                    tmpSuccess = true;
                                    break;
                                }
                            }
                            if (!tmpSuccess)
                                return false; // at least one expected field is not found in stored database
                        }
                    }
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to retrieve schema from table {key}", key);
                    return false;
                }
            }
            else
            {
                _logger.LogError("Problem opening a new connection while checking database structure");
                return false;
            }
            return true;
        }

    }
}
