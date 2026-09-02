using Microsoft.Data.Sqlite;
using OSDC.Drilling.WellBore.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using WellBoreModel = OSDC.Drilling.WellBore.Model.WellBore;

namespace OSDC.Drilling.WellBore.Service.Managers;

internal static class WellBoreSidetrackClassification
{
    public const string CategoryName = "SidetrackClassification";

    public static void MigrateVersion1To2(SqliteConnection connection, bool seedAllDefaults)
    {
        using SqliteTransaction transaction = connection.BeginTransaction();
        try
        {
            IEnumerable<WellBoreFeatureCategoryManager.DefaultWellBoreFeatureCategory> definitions = seedAllDefaults
                ? WellBoreFeatureCategoryManager.DefaultCategories
                : WellBoreFeatureCategoryManager.DefaultCategories.Where(value => value.Name == CategoryName);
            foreach (WellBoreFeatureCategoryManager.DefaultWellBoreFeatureCategory definition in definitions)
                EnsureCategory(connection, transaction, definition);

            WellBoreFeatureCategory category = ReadCategory(connection, transaction)
                ?? throw new InvalidOperationException("The SidetrackClassification category could not be created.");
            BackfillWellBores(connection, transaction, category);

            using SqliteCommand version = connection.CreateCommand();
            version.Transaction = transaction;
            version.CommandText = "PRAGMA user_version = 2";
            version.ExecuteNonQuery();
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public static void Synchronize(SqliteConnection connection, SqliteTransaction transaction,
        WellBoreModel value, bool featureIsCanonical, bool legacyTypeIsExplicit = false)
    {
        WellBoreFeatureCategory? category = ReadCategory(connection, transaction);
        if (category?.MetaInfo == null) return;
        value.WellBoreFeatureAssignments ??= [];
        WellBoreFeatureAssignment? assignment = value.WellBoreFeatureAssignments
            .FirstOrDefault(item => item?.FeatureCategoryID == category.MetaInfo.ID);

        if (!value.IsSidetrack)
        {
            value.WellBoreFeatureAssignments.RemoveAll(item => item?.FeatureCategoryID == category.MetaInfo.ID);
            value.SidetrackType = SidetrackType.Undefined;
            return;
        }
        if (legacyTypeIsExplicit && value.SidetrackType != SidetrackType.Undefined)
        {
            WellBoreFeatureOption? legacyOption = category.Options?.FirstOrDefault(item =>
                string.Equals(item.Name, value.SidetrackType.ToString(), StringComparison.OrdinalIgnoreCase));
            if (legacyOption == null) return;
            if (assignment == null)
            {
                assignment = new WellBoreFeatureAssignment
                {
                    ID = Guid.NewGuid(), FeatureCategoryID = category.MetaInfo.ID, FeatureOptionID = legacyOption.ID
                };
                value.WellBoreFeatureAssignments.Add(assignment);
            }
            else
            {
                assignment.FeatureOptionID = legacyOption.ID;
            }
            value.WellBoreFeatureAssignments.RemoveAll(item => item?.FeatureCategoryID == category.MetaInfo.ID && item.ID != assignment.ID);
            return;
        }
        if (assignment != null)
        {
            string? optionName = category.Options?.FirstOrDefault(option => option.ID == assignment.FeatureOptionID)?.Name;
            value.SidetrackType = LegacyType(optionName);
            return;
        }
        if (featureIsCanonical)
        {
            value.SidetrackType = SidetrackType.Undefined;
            return;
        }
        if (value.SidetrackType == SidetrackType.Undefined) return;
        WellBoreFeatureOption? option = category.Options?.FirstOrDefault(item =>
            string.Equals(item.Name, value.SidetrackType.ToString(), StringComparison.OrdinalIgnoreCase));
        if (option != null)
            value.WellBoreFeatureAssignments.Add(new WellBoreFeatureAssignment
            {
                ID = Guid.NewGuid(), FeatureCategoryID = category.MetaInfo.ID, FeatureOptionID = option.ID
            });
    }

    public static bool HasAssignment(SqliteConnection connection, SqliteTransaction transaction, WellBoreModel value)
    {
        Guid? categoryId = ReadCategory(connection, transaction)?.MetaInfo?.ID;
        return categoryId != null && value.WellBoreFeatureAssignments?.Any(item => item?.FeatureCategoryID == categoryId) == true;
    }

    private static void EnsureCategory(SqliteConnection connection, SqliteTransaction transaction,
        WellBoreFeatureCategoryManager.DefaultWellBoreFeatureCategory definition)
    {
        using SqliteCommand exists = connection.CreateCommand();
        exists.Transaction = transaction;
        exists.CommandText = "SELECT WellBoreFeatureCategory FROM WellBoreFeatureCategoryTable WHERE Name=$name COLLATE NOCASE LIMIT 1";
        exists.Parameters.AddWithValue("$name", definition.Name);
        if (exists.ExecuteScalar() is string existingJson)
        {
            WellBoreFeatureCategory existing = JsonSerializer.Deserialize<WellBoreFeatureCategory>(existingJson, JsonSettings.Options)
                ?? throw new JsonException($"Feature category '{definition.Name}' could not be deserialized.");
            existing.Options ??= [];
            bool changed = existing.IsExclusive != definition.IsExclusive ||
                           existing.HasValidityPeriod != definition.HasValidityPeriod;
            existing.IsExclusive = definition.IsExclusive;
            existing.HasValidityPeriod = definition.HasValidityPeriod;
            foreach (string optionName in definition.Options.Where(name =>
                         !existing.Options.Any(option => string.Equals(option.Name, name, StringComparison.OrdinalIgnoreCase))))
            {
                existing.Options.Add(new WellBoreFeatureOption { ID = Guid.NewGuid(), Name = optionName });
                changed = true;
            }
            if (!changed) return;
            existing.LastModificationDate = DateTimeOffset.UtcNow;
            using SqliteCommand updateExisting = connection.CreateCommand();
            updateExisting.Transaction = transaction;
            updateExisting.CommandText = "UPDATE WellBoreFeatureCategoryTable SET IsExclusive=$exclusive,HasValidityPeriod=$validity," +
                                         "LastModificationDate=$modified,WellBoreFeatureCategory=$document WHERE ID=$id";
            updateExisting.Parameters.AddWithValue("$exclusive", existing.IsExclusive ? 1 : 0);
            updateExisting.Parameters.AddWithValue("$validity", existing.HasValidityPeriod ? 1 : 0);
            updateExisting.Parameters.AddWithValue("$modified", existing.LastModificationDate.Value.ToString(SqlConnectionManager.DATE_TIME_FORMAT));
            updateExisting.Parameters.AddWithValue("$document", JsonSerializer.Serialize(existing, JsonSettings.Options));
            updateExisting.Parameters.AddWithValue("$id", existing.MetaInfo!.ID.ToString());
            if (updateExisting.ExecuteNonQuery() != 1)
                throw new InvalidOperationException($"Could not upgrade feature category '{definition.Name}'.");
            return;
        }

        WellBoreFeatureCategory category = WellBoreFeatureCategoryManager.CreateDefaultCategory(definition);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        category.CreationDate = now;
        category.LastModificationDate = now;
        using SqliteCommand insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = "INSERT INTO WellBoreFeatureCategoryTable " +
            "(ID,MetaInfo,Name,IsExclusive,HasValidityPeriod,CreationDate,LastModificationDate,WellBoreFeatureCategory) " +
            "VALUES ($id,$meta,$name,$exclusive,$validity,$created,$modified,$document)";
        insert.Parameters.AddWithValue("$id", category.MetaInfo!.ID.ToString());
        insert.Parameters.AddWithValue("$meta", JsonSerializer.Serialize(category.MetaInfo, JsonSettings.Options));
        insert.Parameters.AddWithValue("$name", category.Name!);
        insert.Parameters.AddWithValue("$exclusive", category.IsExclusive ? 1 : 0);
        insert.Parameters.AddWithValue("$validity", category.HasValidityPeriod ? 1 : 0);
        insert.Parameters.AddWithValue("$created", now.ToString(SqlConnectionManager.DATE_TIME_FORMAT));
        insert.Parameters.AddWithValue("$modified", now.ToString(SqlConnectionManager.DATE_TIME_FORMAT));
        insert.Parameters.AddWithValue("$document", JsonSerializer.Serialize(category, JsonSettings.Options));
        if (insert.ExecuteNonQuery() != 1)
            throw new InvalidOperationException($"Could not seed feature category '{definition.Name}'.");
    }

    private static WellBoreFeatureCategory? ReadCategory(SqliteConnection connection, SqliteTransaction transaction)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT WellBoreFeatureCategory FROM WellBoreFeatureCategoryTable WHERE Name=$name COLLATE NOCASE LIMIT 1";
        command.Parameters.AddWithValue("$name", CategoryName);
        return command.ExecuteScalar() is string json
            ? JsonSerializer.Deserialize<WellBoreFeatureCategory>(json, JsonSettings.Options) : null;
    }

    private static void BackfillWellBores(SqliteConnection connection, SqliteTransaction transaction,
        WellBoreFeatureCategory category)
    {
        List<(string ID, string Json)> rows = [];
        using (SqliteCommand read = connection.CreateCommand())
        {
            read.Transaction = transaction;
            read.CommandText = "SELECT ID,WellBore FROM WellBoreTable";
            using SqliteDataReader reader = read.ExecuteReader();
            while (reader.Read()) rows.Add((reader.GetString(0), reader.GetString(1)));
        }
        foreach ((string id, string json) in rows)
        {
            WellBoreModel? value = JsonSerializer.Deserialize<WellBoreModel>(json, JsonSettings.Options);
            if (value == null) throw new JsonException($"WellBore '{id}' could not be deserialized.");
            int before = value.WellBoreFeatureAssignments?.Count ?? 0;
            Synchronize(connection, transaction, value, featureIsCanonical: false);
            if ((value.WellBoreFeatureAssignments?.Count ?? 0) == before) continue;
            DateTimeOffset revision = WellBoreDocumentMutationManager.RevisionOf(value);
            DateTimeOffset now = DateTimeOffset.UtcNow;
            value.CreationDate ??= revision;
            value.LastModificationDate = now.UtcTicks > revision.UtcTicks ? now : revision.AddTicks(1);
            using SqliteCommand update = connection.CreateCommand();
            update.Transaction = transaction;
            update.CommandText = "UPDATE WellBoreTable SET WellBore=$document WHERE ID=$id";
            update.Parameters.AddWithValue("$document", JsonSerializer.Serialize(value, JsonSettings.Options));
            update.Parameters.AddWithValue("$id", id);
            if (update.ExecuteNonQuery() != 1)
                throw new InvalidOperationException($"Could not backfill WellBore '{id}'.");
        }
    }

    private static SidetrackType LegacyType(string? optionName) =>
        Enum.TryParse(optionName, ignoreCase: true, out SidetrackType value) ? value : SidetrackType.Undefined;
}
