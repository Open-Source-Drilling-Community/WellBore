using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.Drilling.WellBore.Model;
using OSDC.Drilling.WellBore.Service;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text.Json;

namespace OSDC.Drilling.WellBore.Service.Managers
{
    /// <summary>
    /// A manager for WellBore. The manager implements the singleton pattern as defined by 
    /// Gamma, Erich, et al. "Design patterns: Abstraction and reuse of object-oriented design." 
    /// European Conference on Object-Oriented Programming. Springer, Berlin, Heidelberg, 1993.
    /// </summary>
    public class WellBoreManager
    {
        private static WellBoreManager? _instance = null;
        private readonly ILogger<WellBoreManager> _logger;
        private readonly object _lock = new();
        private readonly SqlConnectionManager _connectionManager;

        private WellBoreManager(ILogger<WellBoreManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _connectionManager = connectionManager;
        }

        public static WellBoreManager GetInstance(ILogger<WellBoreManager> logger, SqlConnectionManager connectionManager)
        {
            _instance ??= new WellBoreManager(logger, connectionManager);
            return _instance;
        }

        public int Count
        {
            get
            {
                int count = 0;
                using var connection = _connectionManager.GetConnection();
                if (connection != null)
                {
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT COUNT(*) FROM WellBoreTable";
                    try
                    {
                        using SqliteDataReader reader = command.ExecuteReader();
                        if (reader.Read())
                        {
                            count = (int)reader.GetInt64(0);
                        }
                    }
                    catch (SqliteException ex)
                    {
                        _logger.LogError(ex, "Impossible to count records in the WellBoreTable");
                    }
                }
                else
                {
                    _logger.LogWarning("Impossible to access the SQLite database");
                }
                return count;
            }
        }

        /// <summary>Creates a dependency-closed WellBore backup from one SQLite snapshot.</summary>
        public WellBoreBatchExportOutcome ExportBatch(WellBoreBatchExportRequest? request)
        {
            using SqliteConnection? connection = _connectionManager.GetConnection();
            if (connection == null) return WellBoreBatchExporter.StorageFailure("The WellBore database is unavailable.");
            using SqliteTransaction transaction = connection.BeginTransaction();
            try
            {
                List<Model.WellBore?> wellBores = ReadDocuments<Model.WellBore>(connection, transaction, "WellBoreTable", "WellBore");
                List<WellBoreIdentity> identities = ReadDocuments<WellBoreIdentity>(connection, transaction,
                    "WellBoreIdentityTable", "WellBoreIdentity").Where(value => value != null).Cast<WellBoreIdentity>().ToList();
                List<WellBoreFeatureCategory> categories = ReadDocuments<WellBoreFeatureCategory>(connection, transaction,
                    "WellBoreFeatureCategoryTable", "WellBoreFeatureCategory").Where(value => value != null).Cast<WellBoreFeatureCategory>().ToList();
                WellBoreBatchExportOutcome outcome = WellBoreBatchExporter.Create(request, wellBores,
                    DateTimeOffset.UtcNow, identities, categories);
                transaction.Commit();
                return outcome;
            }
            catch (Exception exception) when (exception is SqliteException or JsonException or InvalidOperationException)
            {
                try { transaction.Rollback(); } catch (InvalidOperationException) { }
                _logger.LogError(exception, "Unable to create a dependency-closed WellBore backup");
                return WellBoreBatchExporter.StorageFailure("The stored WellBores or catalog dependencies could not be read.");
            }
        }

        /// <summary>Validates and restores a WellBore backup in one transaction.</summary>
        public WellBoreBatchRestoreOutcome RestoreBatch(WellBoreBatchRestoreRequest? request)
        {
            try
            {
                using SqliteConnection? connection = _connectionManager.GetConnection();
                if (connection == null) return WellBoreBatchRestorer.StorageFailure("The WellBore database is unavailable.");
                return WellBoreBatchRestorer.Restore(connection, request, DateTimeOffset.UtcNow);
            }
            catch (SqliteException exception)
            {
                _logger.LogError(exception, "Unable to open the WellBore database for batch restore");
                return WellBoreBatchRestorer.StorageFailure("The WellBore database is unavailable.");
            }
        }

        private static List<T?> ReadDocuments<T>(SqliteConnection connection, SqliteTransaction transaction,
            string table, string documentColumn)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $"SELECT {documentColumn} FROM {table} ORDER BY ID";
            using SqliteDataReader reader = command.ExecuteReader();
            List<T?> result = [];
            while (reader.Read())
            {
                if (reader.IsDBNull(0)) throw new JsonException($"{table} contains a null document.");
                T? value = JsonSerializer.Deserialize<T>(reader.GetString(0), JsonSettings.Options);
                if (value == null) throw new JsonException($"{table} contains an invalid document.");
                result.Add(value);
            }
            return result;
        }

        public bool Clear()
        {
            using var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                bool success = false;
                lock (_lock)
                {
                    using var transaction = connection.BeginTransaction();
                    try
                    {
                        //empty WellBoreTable
                        var command = connection.CreateCommand();
                        command.CommandText = "DELETE FROM WellBoreTable";
                        command.ExecuteNonQuery();

                        transaction.Commit();
                        success = true;
                    }
                    catch (SqliteException ex)
                    {
                        transaction.Rollback();
                        _logger.LogError(ex, "Impossible to clear the WellBoreTable");
                    }
                }
                return success;
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return false;
            }
        }

        public bool Contains(Guid guid)
        {
            int count = 0;
            using var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = $"SELECT COUNT(*) FROM WellBoreTable WHERE ID = '{guid}'";
                try
                {
                    using SqliteDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        count = (int)reader.GetInt64(0);
                    }
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to count rows from WellBoreTable");
                }
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return count >= 1;
        }

        /// <summary>
        /// Returns the list of Guid of all WellBore present in the microservice database 
        /// </summary>
        /// <returns>the list of Guid of all WellBore present in the microservice database</returns>
        public List<Guid>? GetAllWellBoreId()
        {
            List<Guid> ids = [];
            using var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = "SELECT ID FROM WellBoreTable";
                try
                {
                    using var reader = command.ExecuteReader();
                    while (reader.Read() && !reader.IsDBNull(0))
                    {
                        Guid id = reader.GetGuid(0);
                        ids.Add(id);
                    }
                    _logger.LogInformation("Returning the list of ID of existing records from WellBoreTable");
                    return ids;
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to get IDs from WellBoreTable");
                }
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return null;
        }

        /// <summary>
        /// Returns the list of MetaInfo of all WellBore present in the microservice database 
        /// </summary>
        /// <returns>the list of MetaInfo of all WellBore present in the microservice database</returns>
        public List<MetaInfo?>? GetAllWellBoreMetaInfo()
        {
            List<MetaInfo?> metaInfos = new();
            using var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = "SELECT MetaInfo FROM WellBoreTable";
                try
                {
                    using var reader = command.ExecuteReader();
                    while (reader.Read() && !reader.IsDBNull(0))
                    {
                        string mInfo = reader.GetString(0);
                        MetaInfo? metaInfo = JsonSerializer.Deserialize<MetaInfo>(mInfo, JsonSettings.Options);
                        metaInfos.Add(metaInfo);
                    }
                    _logger.LogInformation("Returning the list of MetaInfo of existing records from WellBoreTable");
                    return metaInfos;
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to get IDs from WellBoreTable");
                }
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return null;
        }

        /// <summary>
        /// Returns the WellBore identified by its Guid from the microservice database 
        /// </summary>
        /// <param name="guid"></param>
        /// <returns>the WellBore identified by its Guid from the microservice database</returns>
        public Model.WellBore? GetWellBoreById(Guid guid)
        {
            if (!guid.Equals(Guid.Empty))
            {
                using var connection = _connectionManager.GetConnection();
                if (connection != null)
                {
                    Model.WellBore? wellBore;
                    var command = connection.CreateCommand();
                    command.CommandText = $"SELECT WellBore FROM WellBoreTable WHERE ID = '{guid}'";
                    try
                    {
                        using var reader = command.ExecuteReader();
                        if (reader.Read() && !reader.IsDBNull(0))
                        {
                            string data = reader.GetString(0);
                            wellBore = JsonSerializer.Deserialize<Model.WellBore>(data, JsonSettings.Options);
                            if (wellBore != null && wellBore.MetaInfo != null && !wellBore.MetaInfo.ID.Equals(guid))
                                throw new SqliteException("SQLite database corrupted: returned WellBore is null or has been jsonified with the wrong ID.", 1);
                        }
                        else
                        {
                            _logger.LogInformation("No WellBore of given ID in the database");
                            return null;
                        }
                    }
                    catch (SqliteException ex)
                    {
                        _logger.LogError(ex, "Impossible to get the WellBore with the given ID from WellBoreTable");
                        return null;
                    }
                    _logger.LogInformation("Returning the WellBore of given ID from WellBoreTable");
                    return wellBore;
                }
                else
                {
                    _logger.LogWarning("Impossible to access the SQLite database");
                }
            }
            else
            {
                _logger.LogWarning("The given WellBore ID is null or empty");
            }
            return null;
        }

        /// <summary>
        /// Returns the list of all WellBore present in the microservice database 
        /// </summary>
        /// <returns>the list of all WellBore present in the microservice database</returns>
        public List<Model.WellBore?>? GetAllWellBore()
        {
            List<Model.WellBore?> vals = [];
            using var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = "SELECT WellBore FROM WellBoreTable";
                try
                {
                    using var reader = command.ExecuteReader();
                    while (reader.Read() && !reader.IsDBNull(0))
                    {
                        string data = reader.GetString(0);
                        Model.WellBore? wellBore = JsonSerializer.Deserialize<Model.WellBore>(data, JsonSettings.Options);
                        vals.Add(wellBore);
                    }
                    _logger.LogInformation("Returning the list of existing WellBore from WellBoreTable");
                    return vals;
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to get WellBore from WellBoreTable");
                }
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return null;
        }

        /// <summary>
        /// Returns the list of all WellBore with given Well ID present in the microservice database 
        /// </summary>
        /// <returns>the list of all WellBore present with given Well ID in the microservice database</returns>
        public List<Model.WellBore?>? GetAllWellBoreByWellID(Guid wellID)
        {
            List<Model.WellBore?> vals = [];
            using var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = $"SELECT WellBore FROM WellBoreTable WHERE WellID = '{wellID}'";
                try
                {
                    using var reader = command.ExecuteReader();
                    while (reader.Read() && !reader.IsDBNull(0))
                    {
                        string data = reader.GetString(0);
                        Model.WellBore? wellBore = JsonSerializer.Deserialize<Model.WellBore>(data, JsonSettings.Options);
                        vals.Add(wellBore);
                    }
                    _logger.LogInformation("Returning the list of existing WellBore from WellBoreTable");
                    return vals;
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to get WellBore from WellBoreTable");
                }
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return null;
        }

        /// <summary>
        /// Returns the list of all WellBore with given Rig ID present in the microservice database 
        /// </summary>
        /// <returns>the list of all WellBore present with given Rig ID in the microservice database</returns>
        public List<Model.WellBore?>? GetAllWellBoreByRigID(Guid RigID)
        {
            List<Model.WellBore?> vals = [];
            using var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = $"SELECT WellBore FROM WellBoreTable WHERE RigID = '{RigID}'";
                try
                {
                    using var reader = command.ExecuteReader();
                    while (reader.Read() && !reader.IsDBNull(0))
                    {
                        string data = reader.GetString(0);
                        Model.WellBore? wellBore = JsonSerializer.Deserialize<Model.WellBore>(data, JsonSettings.Options);
                        vals.Add(wellBore);
                    }
                    _logger.LogInformation("Returning the list of existing WellBore from WellBoreTable");
                    return vals;
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to get WellBore from WellBoreTable");
                }
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return null;
        }

        /// <summary>
        /// Returns the list of all WellBore with given Parent Wellbore ID present in the microservice database 
        /// </summary>
        /// <returns>the list of all WellBore present with given Parent Wellbore ID in the microservice database</returns>
        public List<Model.WellBore?>? GetAllWellBoreByParentWellBoreID(Guid ParentID)
        {
            List<Model.WellBore?> vals = [];
            using var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = $"SELECT WellBore FROM WellBoreTable WHERE ParentWellBoreID = '{ParentID}'";
                try
                {
                    using var reader = command.ExecuteReader();
                    while (reader.Read() && !reader.IsDBNull(0))
                    {
                        string data = reader.GetString(0);
                        Model.WellBore? wellBore = JsonSerializer.Deserialize<Model.WellBore>(data, JsonSettings.Options);
                        vals.Add(wellBore);
                    }
                    _logger.LogInformation("Returning the list of existing WellBore from WellBoreTable");
                    return vals;
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to get WellBore from WellBoreTable");
                }
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return null;
        }

        /// <summary>
        /// Returns the list of all WellBore with given Parent Wellbore ID present in the microservice database 
        /// </summary>
        /// <returns>the list of all WellBore present with given Parent Wellbore ID in the microservice database</returns>
        public List<Model.WellBore?>? GetAllSideTrackedWellBore()
        {
            List<Model.WellBore?> vals = [];
            using var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = "SELECT WellBore FROM WellBoreTable WHERE IsSidetrack = true";
                try
                {
                    using var reader = command.ExecuteReader();
                    while (reader.Read() && !reader.IsDBNull(0))
                    {
                        string data = reader.GetString(0);
                        Model.WellBore? wellBore = JsonSerializer.Deserialize<Model.WellBore>(data, JsonSettings.Options);
                        vals.Add(wellBore);
                    }
                    _logger.LogInformation("Returning the list of existing WellBore from WellBoreTable");
                    return vals;
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to get WellBore from WellBoreTable");
                }
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return null;
        }
        /// <summary>
        /// Performs calculation on the given WellBore and adds it to the microservice database
        /// </summary>
        /// <param name="wellBore"></param>
        /// <returns>true if the given WellBore has been added successfully to the microservice database</returns>
        public bool AddWellBore(Model.WellBore? wellBore)
        {
            if (wellBore != null && wellBore.MetaInfo != null && wellBore.MetaInfo.ID != Guid.Empty)
            {
                //if successful, check if another parent data with the same ID was calculated/added during the calculation time
                Model.WellBore? newWellBore = GetWellBoreById(wellBore.MetaInfo.ID);
                if (newWellBore == null)
                {
                    //update WellBoreTable
                    using var connection = _connectionManager.GetConnection();
                    if (connection != null)
                    {
                        using SqliteTransaction transaction = connection.BeginTransaction();
                        bool success = true;
                        try
                        {
                            var validationErrors = WellBoreReferenceIntegrityValidator.ValidateWellBore(connection, transaction, wellBore);
                            if (validationErrors.Count > 0)
                            {
                                _logger.LogWarning("WellBore contains invalid identity or feature assignments: {Errors}",
                                    string.Join("; ", validationErrors.ConvertAll(error => $"{error.Property}:{error.Code}")));
                                transaction.Rollback();
                                return false;
                            }
                            //add the WellBore to the WellBoreTable
                            string metaInfo = JsonSerializer.Serialize(wellBore.MetaInfo, JsonSettings.Options);
                            string data = JsonSerializer.Serialize(wellBore, JsonSettings.Options);
                            var command = connection.CreateCommand();
                            command.Transaction = transaction;
                            command.CommandText = """
                                INSERT INTO WellBoreTable
                                    (ID, MetaInfo, WellID, RigID, IsSidetrack, ParentWellBoreID, WellBore)
                                VALUES
                                    ($id, $metaInfo, $wellId, $rigId, $isSidetrack, $parentWellBoreId, $wellBore)
                                """;
                            command.Parameters.AddWithValue("$id", wellBore.MetaInfo.ID.ToString());
                            command.Parameters.AddWithValue("$metaInfo", metaInfo);
                            command.Parameters.AddWithValue("$wellId", (object?)wellBore.WellID?.ToString() ?? DBNull.Value);
                            command.Parameters.AddWithValue("$rigId", (object?)wellBore.RigID?.ToString() ?? DBNull.Value);
                            command.Parameters.AddWithValue("$isSidetrack", wellBore.IsSidetrack ? 1 : 0);
                            command.Parameters.AddWithValue("$parentWellBoreId", (object?)wellBore.ParentWellBoreID?.ToString() ?? DBNull.Value);
                            command.Parameters.AddWithValue("$wellBore", data);
                            int count = command.ExecuteNonQuery();
                            if (count != 1)
                            {
                                _logger.LogWarning("Impossible to insert the given WellBore into the WellBoreTable");
                                success = false;
                            }
                        }
                        catch (SqliteException ex)
                        {
                            _logger.LogError(ex, "Impossible to add the given WellBore into WellBoreTable");
                            success = false;
                        }
                        //finalizing SQL transaction
                        if (success)
                        {
                            transaction.Commit();
                            _logger.LogInformation("Added the given WellBore of given ID into the WellBoreTable successfully");
                        }
                        else
                        {
                            transaction.Rollback();
                        }
                        return success;
                    }
                    else
                    {
                        _logger.LogWarning("Impossible to access the SQLite database");
                    }
                }
                else
                {
                    _logger.LogWarning("Impossible to post WellBore. ID already found in database.");
                    return false;
                }

            }
            else
            {
                _logger.LogWarning("The WellBore ID or the ID of its input are null or empty");
            }
            return false;
        }

        /// <summary>
        /// Performs calculation on the given WellBore and updates it in the microservice database
        /// </summary>
        /// <param name="wellBore"></param>
        /// <returns>true if the given WellBore has been updated successfully</returns>
        public bool UpdateWellBoreById(Guid guid, Model.WellBore? wellBore)
        {
            bool success = true;
            if (guid != Guid.Empty && wellBore != null && wellBore.MetaInfo != null && wellBore.MetaInfo.ID == guid)
            {
                //update WellBoreTable
                using var connection = _connectionManager.GetConnection();
                if (connection != null)
                {
                    using SqliteTransaction transaction = connection.BeginTransaction();
                    //update fields in WellBoreTable
                    try
                    {
                        var validationErrors = WellBoreReferenceIntegrityValidator.ValidateWellBore(connection, transaction, wellBore);
                        if (validationErrors.Count > 0)
                        {
                            _logger.LogWarning("WellBore contains invalid identity or feature assignments: {Errors}",
                                string.Join("; ", validationErrors.ConvertAll(error => $"{error.Property}:{error.Code}")));
                            transaction.Rollback();
                            return false;
                        }
                        string metaInfo = JsonSerializer.Serialize(wellBore.MetaInfo, JsonSettings.Options);
                        string data = JsonSerializer.Serialize(wellBore, JsonSettings.Options);
                        var command = connection.CreateCommand();
                        command.Transaction = transaction;
                        command.CommandText = """
                            UPDATE WellBoreTable
                            SET MetaInfo = $metaInfo,
                                WellID = $wellId,
                                RigID = $rigId,
                                IsSidetrack = $isSidetrack,
                                ParentWellBoreID = $parentWellBoreId,
                                WellBore = $wellBore
                            WHERE ID = $id
                            """;
                        command.Parameters.AddWithValue("$metaInfo", metaInfo);
                        command.Parameters.AddWithValue("$wellId", (object?)wellBore.WellID?.ToString() ?? DBNull.Value);
                        command.Parameters.AddWithValue("$rigId", (object?)wellBore.RigID?.ToString() ?? DBNull.Value);
                        command.Parameters.AddWithValue("$isSidetrack", wellBore.IsSidetrack ? 1 : 0);
                        command.Parameters.AddWithValue("$parentWellBoreId", (object?)wellBore.ParentWellBoreID?.ToString() ?? DBNull.Value);
                        command.Parameters.AddWithValue("$wellBore", data);
                        command.Parameters.AddWithValue("$id", guid.ToString());
                        int count = command.ExecuteNonQuery();
                        if (count != 1)
                        {
                            _logger.LogWarning("Impossible to update the WellBore");
                            success = false;
                        }
                    }
                    catch (SqliteException ex)
                    {
                        _logger.LogError(ex, "Impossible to update the WellBore");
                        success = false;
                    }

                    // Finalizing
                    if (success)
                    {
                        transaction.Commit();
                        _logger.LogInformation("Updated the given WellBore successfully");
                        return true;
                    }
                    else
                    {
                        transaction.Rollback();
                    }
                }
                else
                {
                    _logger.LogWarning("Impossible to access the SQLite database");
                }
            }
            else
            {
                _logger.LogWarning("The WellBore ID or the ID of some of its attributes are null or empty");
            }
            return false;
        }

        /// <summary>
        /// Deletes the WellBore of given ID from the microservice database
        /// </summary>
        /// <param name="guid"></param>
        /// <returns>true if the WellBore was deleted from the microservice database</returns>
        public bool DeleteWellBoreById(Guid guid)
        {
            if (!guid.Equals(Guid.Empty))
            {
                using var connection = _connectionManager.GetConnection();
                if (connection != null)
                {
                    using var transaction = connection.BeginTransaction();
                    bool success = true;
                    //delete WellBore from WellBoreTable
                    try
                    {
                        var command = connection.CreateCommand();
                        command.CommandText = $"DELETE FROM WellBoreTable WHERE ID = '{guid}'";
                        int count = command.ExecuteNonQuery();
                        if (count < 0)
                        {
                            _logger.LogWarning("Impossible to delete the WellBore of given ID from the WellBoreTable");
                            success = false;
                        }
                    }
                    catch (SqliteException ex)
                    {
                        _logger.LogError(ex, "Impossible to delete the WellBore of given ID from WellBoreTable");
                        success = false;
                    }
                    if (success)
                    {
                        transaction.Commit();
                        _logger.LogInformation("Removed the WellBore of given ID from the WellBoreTable successfully");
                    }
                    else
                    {
                        transaction.Rollback();
                    }
                    return success;
                }
                else
                {
                    _logger.LogWarning("Impossible to access the SQLite database");
                }
            }
            else
            {
                _logger.LogWarning("The WellBore ID is null or empty");
            }
            return false;
        }
    }
}
