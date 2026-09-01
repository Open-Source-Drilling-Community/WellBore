using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace OSDC.Drilling.WellBore.Service.Managers
{
    public class WellBoreIdentityManager
    {
        private static WellBoreIdentityManager? _instance;
        private readonly ILogger<WellBoreIdentityManager> _logger;
        private readonly SqlConnectionManager _connectionManager;
        private static readonly string[] DefaultIdentities =
        [
            "OfficialAuthorityName", "OperatorName", "CompanyInternalName", "PlanningName",
            "DataManagementName", "HistoricalName", "ShortName", "DisplayName",
            "ReportingName", "LegacyName", "ImportedName"
        ];
        private WellBoreIdentityManager(ILogger<WellBoreIdentityManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _connectionManager = connectionManager;
        }

        public static WellBoreIdentityManager GetInstance(ILogger<WellBoreIdentityManager> logger, SqlConnectionManager connectionManager)
        {
            _instance ??= new WellBoreIdentityManager(logger, connectionManager);
            return _instance;
        }

        public List<Guid>? GetAllWellBoreIdentityId()
        {
            EnsureDefaultIdentities();
            List<Guid> ids = [];
            using var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return null;
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT ID FROM WellBoreIdentityTable";
            try
            {
                using var reader = command.ExecuteReader();
                while (reader.Read() && !reader.IsDBNull(0))
                {
                    ids.Add(reader.GetGuid(0));
                }
                return ids;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get IDs from WellBoreIdentityTable");
                return null;
            }
        }

        public List<MetaInfo?>? GetAllWellBoreIdentityMetaInfo()
        {
            EnsureDefaultIdentities();
            List<MetaInfo?> metaInfos = [];
            using var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return null;
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT MetaInfo FROM WellBoreIdentityTable";
            try
            {
                using var reader = command.ExecuteReader();
                while (reader.Read() && !reader.IsDBNull(0))
                {
                    metaInfos.Add(JsonSerializer.Deserialize<MetaInfo>(reader.GetString(0), JsonSettings.Options));
                }
                return metaInfos;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get MetaInfo from WellBoreIdentityTable");
                return null;
            }
        }

        public Model.WellBoreIdentity? GetWellBoreIdentityById(Guid guid)
        {
            if (guid == Guid.Empty)
            {
                return null;
            }

            using var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return null;
            }

            var command = connection.CreateCommand();
            command.CommandText = $"SELECT WellBoreIdentity FROM WellBoreIdentityTable WHERE ID = '{guid}'";
            try
            {
                using var reader = command.ExecuteReader();
                if (reader.Read() && !reader.IsDBNull(0))
                {
                    Model.WellBoreIdentity? data = JsonSerializer.Deserialize<Model.WellBoreIdentity>(reader.GetString(0), JsonSettings.Options);
                    if (data != null && data.MetaInfo != null && data.MetaInfo.ID != guid)
                    {
                        throw new SqliteException("SQLite database corrupted: returned WellBoreIdentity has the wrong ID.", 1);
                    }
                    return data;
                }
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get WellBoreIdentity from WellBoreIdentityTable");
            }

            return null;
        }

        public List<Model.WellBoreIdentity?>? GetAllWellBoreIdentity()
        {
            EnsureDefaultIdentities();
            List<Model.WellBoreIdentity?> values = [];
            using var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return null;
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT WellBoreIdentity FROM WellBoreIdentityTable";
            try
            {
                using var reader = command.ExecuteReader();
                while (reader.Read() && !reader.IsDBNull(0))
                {
                    values.Add(JsonSerializer.Deserialize<Model.WellBoreIdentity>(reader.GetString(0), JsonSettings.Options));
                }
                return values;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get WellBoreIdentity from WellBoreIdentityTable");
                return null;
            }
        }

        public bool AddWellBoreIdentity(Model.WellBoreIdentity? data)
        {
            if (data?.MetaInfo == null || data.MetaInfo.ID == Guid.Empty)
            {
                return false;
            }
            if (GetWellBoreIdentityById(data.MetaInfo.ID) != null)
            {
                return false;
            }

            using var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return false;
            }

            using SqliteTransaction transaction = connection.BeginTransaction();
            try
            {
                DateTimeOffset now = DateTimeOffset.UtcNow;
                data.CreationDate = now;
                data.LastModificationDate = now;
                string metaInfo = JsonSerializer.Serialize(data.MetaInfo, JsonSettings.Options);
                string serialized = JsonSerializer.Serialize(data, JsonSettings.Options);
                string? creationDate = data.CreationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                string? lastModificationDate = data.LastModificationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "INSERT INTO WellBoreIdentityTable " +
                    "(ID, MetaInfo, Name, CreationDate, LastModificationDate, WellBoreIdentity) " +
                    "VALUES ($id, $meta, $name, $created, $modified, $document)";
                command.Parameters.AddWithValue("$id", data.MetaInfo.ID.ToString());
                command.Parameters.AddWithValue("$meta", metaInfo);
                command.Parameters.AddWithValue("$name", data.Name ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$created", creationDate ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$modified", lastModificationDate ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$document", serialized);
                int count = command.ExecuteNonQuery();
                if (count != 1)
                {
                    transaction.Rollback();
                    return false;
                }
                transaction.Commit();
                return true;
            }
            catch (SqliteException ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Impossible to add WellBoreIdentity");
                return false;
            }
        }

        public bool UpdateWellBoreIdentityById(Guid guid, Model.WellBoreIdentity? data)
        {
            if (guid == Guid.Empty || data?.MetaInfo == null || data.MetaInfo.ID != guid)
            {
                return false;
            }

            using var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return false;
            }

            using SqliteTransaction transaction = connection.BeginTransaction();
            try
            {
                data.LastModificationDate = DateTimeOffset.UtcNow;
                string metaInfo = JsonSerializer.Serialize(data.MetaInfo, JsonSettings.Options);
                string serialized = JsonSerializer.Serialize(data, JsonSettings.Options);
                string? creationDate = data.CreationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                string? lastModificationDate = data.LastModificationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                var command = connection.CreateCommand();
                command.CommandText = $"UPDATE WellBoreIdentityTable SET " +
                    $"MetaInfo = '{metaInfo}', " +
                    $"Name = '{data.Name}', " +
                    $"CreationDate = '{creationDate}', " +
                    $"LastModificationDate = '{lastModificationDate}', " +
                    $"WellBoreIdentity = '{serialized}' " +
                    $"WHERE ID = '{guid}'";
                int count = command.ExecuteNonQuery();
                if (count != 1)
                {
                    transaction.Rollback();
                    return false;
                }
                transaction.Commit();
                return true;
            }
            catch (SqliteException ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Impossible to update WellBoreIdentity");
                return false;
            }
        }

        public bool DeleteWellBoreIdentityById(Guid guid)
        {
            if (guid == Guid.Empty)
            {
                return false;
            }

            using var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return false;
            }

            using SqliteTransaction transaction = connection.BeginTransaction();
            try
            {
                var command = connection.CreateCommand();
                command.CommandText = $"DELETE FROM WellBoreIdentityTable WHERE ID = '{guid}'";
                command.ExecuteNonQuery();
                transaction.Commit();
                return true;
            }
            catch (SqliteException ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Impossible to delete WellBoreIdentity");
                return false;
            }
        }

        private void EnsureDefaultIdentities()
        {
            using var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return;
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM WellBoreIdentityTable";
            try
            {
                using SqliteDataReader reader = command.ExecuteReader();
                if (reader.Read() && reader.GetInt64(0) > 0)
                {
                    return;
                }
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to count WellBoreIdentityTable");
                return;
            }

            foreach (string name in DefaultIdentities)
            {
                AddWellBoreIdentity(new Model.WellBoreIdentity
                {
                    MetaInfo = new MetaInfo { ID = Guid.NewGuid() },
                    Name = name
                });
            }
        }
    }
}




