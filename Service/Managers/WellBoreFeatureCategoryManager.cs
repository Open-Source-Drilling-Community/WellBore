using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace OSDC.Drilling.WellBore.Service.Managers
{
    public class WellBoreFeatureCategoryManager
    {
        private static WellBoreFeatureCategoryManager? _instance;
        private readonly ILogger<WellBoreFeatureCategoryManager> _logger;
        private readonly SqlConnectionManager _connectionManager;
        internal static readonly DefaultWellBoreFeatureCategory[] DefaultCategories =
        [
            new("WellboreRole", true, false, ["MainBore", "PilotBore", "Sidetrack", "Lateral", "MultilateralBranch", "MotherBore", "ParentBore", "ChildBore", "ReEntryBore", "ReliefInterceptBore", "BypassBore", "DrainBore", "ObservationBore", "Unknown"]),
            new("WellboreOrigin", true, false, ["OriginalBore", "PlannedSidetrack", "TechnicalSidetrack", "GeologicalSidetrack", "NaturalSidetrack", "MechanicalSidetrack", "ReDrill", "ReEntry", "WhipstockSidetrack", "OpenHoleSidetrack", "CasedHoleSidetrack", "WindowMilledSidetrack", "Unknown"]),
            new("SidetrackReason", false, false, ["GeologicalTargetChange", "CollisionAvoidance", "StuckPipe", "FishInHole", "LostBHA", "ExcessiveDogleg", "PoorHoleCondition", "WellControlEvent", "SevereLosses", "FormationInstability", "TrajectoryCorrection", "ReservoirOptimization", "SlotRecovery", "AbandonOriginalHole", "Unknown"]),
            new("WellboreGeometryClass", true, false, ["Vertical", "Deviated", "Directional", "Horizontal", "Tangential", "SShaped", "JShaped", "ExtendedReach", "Complex3D", "HighDogleg", "Unknown"]),
            new("WellboreTrajectoryIntent", true, false, ["StraightHole", "BuildAndHold", "BuildHoldDrop", "HorizontalDrain", "GeosteeredDrain", "TangentSection", "PilotThenSidetrack", "ReliefIntercept", "CollisionAvoidancePath", "MultiTargetPath", "Unknown"]),
            new("WellboreConstructionStatus", true, true, ["Proposed", "Planned", "Approved", "Drilling", "Suspended", "Completed", "AbandonedOpenHole", "PluggedBack", "PluggedAndAbandoned", "ReEntered", "ReCompleted", "Unknown"]),
            new("WellboreSectionContext", false, false, ["TopHole", "SurfaceSection", "IntermediateSection", "ProductionSection", "ReservoirSection", "OpenHoleSection", "CasedHoleSection", "LinerSection", "BuildSection", "TangentSection", "HorizontalSection", "Unknown"]),
            new("WellboreCompletionContext", false, true, ["NotCompleted", "OpenHoleCompletion", "CasedHoleCompletion", "PerforatedCompletion", "SlottedLiner", "ScreenCompletion", "GravelPack", "FracPack", "IntelligentCompletion", "MultizoneCompletion", "SelectiveCompletion", "CommingledCompletion", "AbandonedBeforeCompletion", "Unknown"]),
            new("WellboreDataAvailability", false, false, ["HasPlannedTrajectory", "HasActualTrajectory", "HasSurveyStations", "HasSurveyUncertainty", "HasDirectionalPlan", "HasAntiCollisionData", "HasMudLog", "HasLWD", "HasMWD", "HasWirelineLogs", "HasCasingProgram", "HasCementData", "HasCompletionData", "HasDrillingEvents", "HasDailyReports", "HasRealTimeData", "HasFinalWellReport"]),
            new("WellboreHazard", false, true, ["H2S", "CO2", "ShallowGas", "ShallowWaterFlow", "SevereLosses", "Gains", "NarrowMudWindow", "UnstableFormation", "SwellingShale", "DepletedZone", "FaultCrossing", "Salt", "Coal", "Karst", "HydrateRisk", "BallooningRisk", "CollisionRisk", "HighDoglegRisk", "StuckPipeRisk", "Unknown"]),
            new(WellBoreSidetrackClassification.CategoryName, true, false, ["Technical", "Production", "Appraisal", "Lateral", "Unknown"])
        ];
        private WellBoreFeatureCategoryManager(ILogger<WellBoreFeatureCategoryManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _connectionManager = connectionManager;
        }

        public static WellBoreFeatureCategoryManager GetInstance(ILogger<WellBoreFeatureCategoryManager> logger, SqlConnectionManager connectionManager)
        {
            _instance ??= new WellBoreFeatureCategoryManager(logger, connectionManager);
            return _instance;
        }

        public List<Guid>? GetAllWellBoreFeatureCategoryId()
        {
            EnsureDefaultCategories();
            List<Guid> ids = [];
            using var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return null;
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT ID FROM WellBoreFeatureCategoryTable";
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
                _logger.LogError(ex, "Impossible to get IDs from WellBoreFeatureCategoryTable");
                return null;
            }
        }

        public List<MetaInfo?>? GetAllWellBoreFeatureCategoryMetaInfo()
        {
            EnsureDefaultCategories();
            List<MetaInfo?> metaInfos = [];
            using var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return null;
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT MetaInfo FROM WellBoreFeatureCategoryTable";
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
                _logger.LogError(ex, "Impossible to get MetaInfo from WellBoreFeatureCategoryTable");
                return null;
            }
        }

        public Model.WellBoreFeatureCategory? GetWellBoreFeatureCategoryById(Guid guid)
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
            command.CommandText = $"SELECT WellBoreFeatureCategory FROM WellBoreFeatureCategoryTable WHERE ID = '{guid}'";
            try
            {
                using var reader = command.ExecuteReader();
                if (reader.Read() && !reader.IsDBNull(0))
                {
                    Model.WellBoreFeatureCategory? data = JsonSerializer.Deserialize<Model.WellBoreFeatureCategory>(reader.GetString(0), JsonSettings.Options);
                    if (data != null && data.MetaInfo != null && data.MetaInfo.ID != guid)
                    {
                        throw new SqliteException("SQLite database corrupted: returned WellBoreFeatureCategory has the wrong ID.", 1);
                    }
                    return data;
                }
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get WellBoreFeatureCategory from WellBoreFeatureCategoryTable");
            }

            return null;
        }

        public List<Model.WellBoreFeatureCategory?>? GetAllWellBoreFeatureCategory()
        {
            EnsureDefaultCategories();
            List<Model.WellBoreFeatureCategory?> values = [];
            using var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return null;
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT WellBoreFeatureCategory FROM WellBoreFeatureCategoryTable";
            try
            {
                using var reader = command.ExecuteReader();
                while (reader.Read() && !reader.IsDBNull(0))
                {
                    values.Add(JsonSerializer.Deserialize<Model.WellBoreFeatureCategory>(reader.GetString(0), JsonSettings.Options));
                }
                return values;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get WellBoreFeatureCategory from WellBoreFeatureCategoryTable");
                return null;
            }
        }

        public bool AddWellBoreFeatureCategory(Model.WellBoreFeatureCategory? data)
        {
            if (data?.MetaInfo == null || data.MetaInfo.ID == Guid.Empty)
            {
                return false;
            }
            if (GetWellBoreFeatureCategoryById(data.MetaInfo.ID) != null)
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
                PrepareCategory(data);
                DateTimeOffset now = DateTimeOffset.UtcNow;
                data.CreationDate = now;
                data.LastModificationDate = now;
                string metaInfo = JsonSerializer.Serialize(data.MetaInfo, JsonSettings.Options);
                string serialized = JsonSerializer.Serialize(data, JsonSettings.Options);
                string? creationDate = data.CreationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                string? lastModificationDate = data.LastModificationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "INSERT INTO WellBoreFeatureCategoryTable " +
                    "(ID, MetaInfo, Name, IsExclusive, HasValidityPeriod, CreationDate, LastModificationDate, WellBoreFeatureCategory) " +
                    "VALUES ($id, $meta, $name, $exclusive, $validity, $created, $modified, $document)";
                command.Parameters.AddWithValue("$id", data.MetaInfo.ID.ToString());
                command.Parameters.AddWithValue("$meta", metaInfo);
                command.Parameters.AddWithValue("$name", data.Name ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$exclusive", data.IsExclusive ? 1 : 0);
                command.Parameters.AddWithValue("$validity", data.HasValidityPeriod ? 1 : 0);
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
                _logger.LogError(ex, "Impossible to add WellBoreFeatureCategory");
                return false;
            }
        }

        public bool UpdateWellBoreFeatureCategoryById(Guid guid, Model.WellBoreFeatureCategory? data)
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
                PrepareCategory(data);
                data.LastModificationDate = DateTimeOffset.UtcNow;
                string metaInfo = JsonSerializer.Serialize(data.MetaInfo, JsonSettings.Options);
                string serialized = JsonSerializer.Serialize(data, JsonSettings.Options);
                string? creationDate = data.CreationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                string? lastModificationDate = data.LastModificationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                var command = connection.CreateCommand();
                command.CommandText = $"UPDATE WellBoreFeatureCategoryTable SET " +
                    $"MetaInfo = '{metaInfo}', " +
                    $"Name = '{data.Name}', " +
                    $"IsExclusive = {(data.IsExclusive ? 1 : 0)}, " +
                    $"HasValidityPeriod = {(data.HasValidityPeriod ? 1 : 0)}, " +
                    $"CreationDate = '{creationDate}', " +
                    $"LastModificationDate = '{lastModificationDate}', " +
                    $"WellBoreFeatureCategory = '{serialized}' " +
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
                _logger.LogError(ex, "Impossible to update WellBoreFeatureCategory");
                return false;
            }
        }

        public bool DeleteWellBoreFeatureCategoryById(Guid guid)
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
                command.CommandText = $"DELETE FROM WellBoreFeatureCategoryTable WHERE ID = '{guid}'";
                command.ExecuteNonQuery();
                transaction.Commit();
                return true;
            }
            catch (SqliteException ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Impossible to delete WellBoreFeatureCategory");
                return false;
            }
        }

        private void EnsureDefaultCategories()
        {
            using var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return;
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM WellBoreFeatureCategoryTable";
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
                _logger.LogError(ex, "Impossible to count WellBoreFeatureCategoryTable");
                return;
            }

            foreach (DefaultWellBoreFeatureCategory defaultCategory in DefaultCategories)
            {
                AddWellBoreFeatureCategory(CreateDefaultCategory(defaultCategory));
            }
        }

        internal static Model.WellBoreFeatureCategory CreateDefaultCategory(DefaultWellBoreFeatureCategory defaultCategory) =>
            new()
            {
                MetaInfo = new MetaInfo { ID = Guid.NewGuid() },
                Name = defaultCategory.Name,
                IsExclusive = defaultCategory.IsExclusive,
                HasValidityPeriod = defaultCategory.HasValidityPeriod,
                Options = defaultCategory.Options
                    .Select(option => new Model.WellBoreFeatureOption { ID = Guid.NewGuid(), Name = option })
                    .ToList()
            };

        private static void PrepareCategory(Model.WellBoreFeatureCategory category)
        {
            category.Options ??= [];
            foreach (Model.WellBoreFeatureOption option in category.Options)
            {
                if (option.ID == Guid.Empty)
                {
                    option.ID = Guid.NewGuid();
                }
            }
        }

        internal sealed record DefaultWellBoreFeatureCategory(
            string Name,
            bool IsExclusive,
            bool HasValidityPeriod,
            string[] Options);
    }
}


