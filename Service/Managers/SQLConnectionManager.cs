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
    /// Prior to creating a database, existing database structure is checked for consistency with the structure defined in tableStructureDict_
    /// If the schema is inconsistent (table count, table names, field count, or field names), startup stops and leaves the database unchanged.
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
        /// Validates an existing database without modifying it, or creates the initial schema for an empty database.
        /// An unexpected schema aborts startup so persisted WellBore data is never dropped automatically.
        /// </summary>
        private void ManageDataBase()
        {
            using var connection = GetConnection() ??
                throw new InvalidOperationException("The WellBore database could not be opened.");
            List<string> tableNames = [];
            using (var command = new SqliteCommand(
                       "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%';", connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read()) tableNames.Add(reader.GetString(0));
            }

            if (tableNames.Count == 0)
            {
                _logger.LogInformation("Creating the initial WellBore database schema");
                foreach (var tableStructure in _tableStructureDict)
                {
                    if (!CreateTable(tableStructure) || !IndexTable(tableStructure.Key))
                        throw new InvalidOperationException("The initial WellBore database schema could not be created.");
                }
                return;
            }

            bool schemaMatches = tableNames.Count == _tableStructureDict.Count &&
                _tableStructureDict.All(tableStructure =>
                    tableNames.Contains(tableStructure.Key, StringComparer.Ordinal) &&
                    CheckDatabaseStructure(tableStructure));
            if (!schemaMatches)
            {
                _logger.LogCritical("Unexpected WellBore database schema. Startup is aborted and the database is left unchanged.");
                throw new InvalidOperationException(
                    "Unexpected WellBore database schema. No automatic destructive repair was attempted.");
            }
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

        private bool CreateTable(KeyValuePair<string, string[]> tabStruct)
        {
            using var connection = GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                string key = tabStruct.Key;
                StringBuilder sb = new StringBuilder();
                sb.Append($"CREATE TABLE {key} ()");
                foreach (string col in tabStruct.Value)
                {
                    sb.Insert(sb.Length - 1, col + ",");
                };
                sb.Remove(sb.Length - 2, 1);
                command.CommandText = sb.ToString();

                try
                {
                    int res = command.ExecuteNonQuery();
                    _logger.LogInformation("{key} has been successfully created", key);
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to create {key}", key);
                    return false;
                }
            }
            else
            {
                _logger.LogError("Problem opening a new connection while creating table");
                return false;
            }
            return true;
        }

        private bool IndexTable(string dbName)
        {
            using var connection = GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = $"CREATE UNIQUE INDEX {dbName}Index ON {dbName} (ID)";
                try
                {
                    int res = command.ExecuteNonQuery();
                    _logger.LogInformation("{dbName} has been successfully indexed", dbName);
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to index {dbName}", dbName);
                    return false;
                }
            }
            else
            {
                _logger.LogError("Problem opening a new connection while creating table");
                return false;
            }
            return true;
        }

    }
}
