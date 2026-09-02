using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using OSDC.Drilling.WellBore.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace OSDC.Drilling.WellBore.Service.Managers;

internal static class WellBoreCatalogMutationManager
{
    public static WellBoreMutationResult UpdateFeatureCategory(SqlConnectionManager manager, ILogger logger, Guid id,
        DateTimeOffset expectedModifiedUtc, WellBoreFeatureCategory? value) =>
        UpdateCategory(manager, logger, id, expectedModifiedUtc, value,
            "WellBoreFeatureCategoryTable", "WellBoreFeatureCategory",
            category => category.MetaInfo, category => category.CreationDate, (category, date) => category.CreationDate = date,
            category => category.LastModificationDate, (category, date) => category.LastModificationDate = date,
            category => category.Options, option => option.ID, (option, optionId) => option.ID = optionId,
            (connection, transaction, categoryId, options) => WellBoreReferenceIntegrityValidator.FindFeatureCategoryReferences(connection, transaction, categoryId, options),
            (command, category) =>
            {
                command.CommandText = "UPDATE WellBoreFeatureCategoryTable SET MetaInfo=$meta, Name=$name, IsExclusive=$exclusive, HasValidityPeriod=$validity, CreationDate=$created, LastModificationDate=$modified, WellBoreFeatureCategory=$document WHERE ID=$id";
                command.Parameters.AddWithValue("$name", category.Name ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$exclusive", category.IsExclusive ? 1 : 0);
                command.Parameters.AddWithValue("$validity", category.HasValidityPeriod ? 1 : 0);
            });

    public static WellBoreMutationResult UpdateIdentity(SqlConnectionManager manager, ILogger logger, Guid id,
        DateTimeOffset expectedModifiedUtc, WellBoreIdentity? value) =>
        UpdateNamed(manager, logger, id, expectedModifiedUtc, value,
            "WellBoreIdentityTable", "WellBoreIdentity", identity => identity.MetaInfo,
            identity => identity.CreationDate, (identity, date) => identity.CreationDate = date,
            identity => identity.LastModificationDate, (identity, date) => identity.LastModificationDate = date,
            (command, identity) =>
            {
                command.CommandText = "UPDATE WellBoreIdentityTable SET MetaInfo=$meta, Name=$name, CreationDate=$created, LastModificationDate=$modified, WellBoreIdentity=$document WHERE ID=$id";
                command.Parameters.AddWithValue("$name", identity.Name ?? (object)DBNull.Value);
            });

    public static WellBoreMutationResult DeleteFeatureCategory(SqlConnectionManager manager, ILogger logger, Guid id,
        DateTimeOffset expectedModifiedUtc) =>
        Delete(manager, logger, id, expectedModifiedUtc, "WellBoreFeatureCategoryTable", "WellBoreFeatureCategory",
            (connection, transaction) => WellBoreReferenceIntegrityValidator.FindFeatureCategoryReferences(connection, transaction, id));

    public static WellBoreMutationResult DeleteIdentity(SqlConnectionManager manager, ILogger logger, Guid id,
        DateTimeOffset expectedModifiedUtc) =>
        Delete(manager, logger, id, expectedModifiedUtc, "WellBoreIdentityTable", "WellBoreIdentity",
            (connection, transaction) => WellBoreReferenceIntegrityValidator.FindIdentityReferences(connection, transaction, id));

    private static WellBoreMutationResult UpdateCategory<TCategory, TOption>(SqlConnectionManager manager, ILogger logger,
        Guid id, DateTimeOffset expectedModifiedUtc, TCategory? value, string table, string documentColumn,
        Func<TCategory, OSDC.DotnetLibraries.General.DataManagement.MetaInfo?> meta,
        Func<TCategory, DateTimeOffset?> creationDate, Action<TCategory, DateTimeOffset?> setCreationDate,
        Func<TCategory, DateTimeOffset?> modificationDate, Action<TCategory, DateTimeOffset?> setModificationDate,
        Func<TCategory, List<TOption>?> options, Func<TOption, Guid> optionId, Action<TOption, Guid> setOptionId,
        Func<SqliteConnection, SqliteTransaction, Guid, IReadOnlyCollection<Guid>, WellBoreMutationError?> findRemovedReferences,
        Action<SqliteCommand, TCategory> configure)
        where TCategory : class
    {
        if (value == null || meta(value)?.ID != id || id == Guid.Empty)
        {
            return WellBoreMutationResult.Invalid("MetaInfo.ID", "id_mismatch", "The route UUID must match MetaInfo.ID.");
        }
        List<TOption> categoryOptions = options(value) ?? [];
        foreach (TOption option in categoryOptions.Where(option => optionId(option) == Guid.Empty))
        {
            setOptionId(option, Guid.NewGuid());
        }
        List<Guid> optionIds = categoryOptions.Select(optionId).ToList();
        if (optionIds.Count != optionIds.Distinct().Count())
        {
            return WellBoreMutationResult.Invalid("Options", "duplicate_option_id", "Option UUIDs must be unique within a category.");
        }

        return ExecuteUpdate(manager, logger, id, expectedModifiedUtc, value, table, documentColumn,
            meta, creationDate, setCreationDate, modificationDate, setModificationDate,
            (connection, transaction) => findRemovedReferences(connection, transaction, id, optionIds), configure);
    }

    private static WellBoreMutationResult UpdateNamed<T>(SqlConnectionManager manager, ILogger logger, Guid id,
        DateTimeOffset expectedModifiedUtc, T? value, string table, string documentColumn,
        Func<T, OSDC.DotnetLibraries.General.DataManagement.MetaInfo?> meta,
        Func<T, DateTimeOffset?> creationDate, Action<T, DateTimeOffset?> setCreationDate,
        Func<T, DateTimeOffset?> modificationDate, Action<T, DateTimeOffset?> setModificationDate,
        Action<SqliteCommand, T> configure)
        where T : class =>
        value == null || meta(value)?.ID != id || id == Guid.Empty
            ? WellBoreMutationResult.Invalid("MetaInfo.ID", "id_mismatch", "The route UUID must match MetaInfo.ID.")
            : ExecuteUpdate(manager, logger, id, expectedModifiedUtc, value, table, documentColumn,
                meta, creationDate, setCreationDate, modificationDate, setModificationDate, null, configure);

    private static WellBoreMutationResult ExecuteUpdate<T>(SqlConnectionManager manager, ILogger logger, Guid id,
        DateTimeOffset expectedModifiedUtc, T value, string table, string documentColumn,
        Func<T, OSDC.DotnetLibraries.General.DataManagement.MetaInfo?> meta,
        Func<T, DateTimeOffset?> creationDate, Action<T, DateTimeOffset?> setCreationDate,
        Func<T, DateTimeOffset?> modificationDate, Action<T, DateTimeOffset?> setModificationDate,
        Func<SqliteConnection, SqliteTransaction, WellBoreMutationError?>? referenceCheck,
        Action<SqliteCommand, T> configure)
        where T : class
    {
        using SqliteConnection? connection = manager.GetConnection();
        if (connection == null)
        {
            return WellBoreMutationResult.StorageFailure();
        }
        using SqliteTransaction transaction = connection.BeginTransaction();
        try
        {
            T? stored = Read<T>(connection, transaction, table, documentColumn, id);
            if (stored == null)
            {
                transaction.Rollback();
                return WellBoreMutationResult.NotFound("The catalog definition does not exist.");
            }
            DateTimeOffset? storedModified = modificationDate(stored);
            if (storedModified == null || !SameInstant(storedModified.Value, expectedModifiedUtc))
            {
                transaction.Rollback();
                return WellBoreMutationResult.ConcurrencyConflict("expectedModifiedUtc",
                    $"Expected {expectedModifiedUtc:O}, but the stored definition was modified at {storedModified:O}.");
            }
            WellBoreMutationError? referenceError = referenceCheck?.Invoke(connection, transaction);
            if (referenceError != null)
            {
                transaction.Rollback();
                return WellBoreMutationResult.ReferenceConflict(referenceError);
            }

            setCreationDate(value, creationDate(stored));
            setModificationDate(value, DateTimeOffset.UtcNow);
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            configure(command, value);
            command.Parameters.AddWithValue("$id", id.ToString());
            command.Parameters.AddWithValue("$meta", JsonSerializer.Serialize(meta(value), JsonSettings.Options));
            command.Parameters.AddWithValue("$created", creationDate(value)?.ToString(SqlConnectionManager.DATE_TIME_FORMAT) ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("$modified", modificationDate(value)?.ToString(SqlConnectionManager.DATE_TIME_FORMAT) ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("$document", JsonSerializer.Serialize(value, JsonSettings.Options));
            if (command.ExecuteNonQuery() != 1)
            {
                transaction.Rollback();
                return WellBoreMutationResult.StorageFailure();
            }
            transaction.Commit();
            return WellBoreMutationResult.Success();
        }
        catch (Exception ex) when (ex is SqliteException or JsonException)
        {
            transaction.Rollback();
            logger.LogError(ex, "Unable to update {Table} record {RecordId}", table, id);
            return WellBoreMutationResult.StorageFailure();
        }
    }

    private static WellBoreMutationResult Delete(SqlConnectionManager manager, ILogger logger, Guid id,
        DateTimeOffset expectedModifiedUtc, string table, string documentColumn,
        Func<SqliteConnection, SqliteTransaction, WellBoreMutationError?> referenceCheck)
    {
        if (id == Guid.Empty)
        {
            return WellBoreMutationResult.Invalid("id", "invalid_id", "A non-empty UUID is required.");
        }
        if (expectedModifiedUtc == default)
            return WellBoreMutationResult.Invalid("expectedModifiedUtc", "required", "A non-default optimistic-concurrency timestamp is required.");
        using SqliteConnection? connection = manager.GetConnection();
        if (connection == null)
        {
            return WellBoreMutationResult.StorageFailure();
        }
        using SqliteTransaction transaction = connection.BeginTransaction();
        try
        {
            using (SqliteCommand read = connection.CreateCommand())
            {
                read.Transaction = transaction;
                read.CommandText = $"SELECT {documentColumn} FROM {table} WHERE ID=$id";
                read.Parameters.AddWithValue("$id", id.ToString());
                if (read.ExecuteScalar() is not string json)
                {
                    transaction.Rollback();
                    return WellBoreMutationResult.NotFound("The catalog definition does not exist.");
                }
                using JsonDocument document = JsonDocument.Parse(json);
                DateTimeOffset storedRevision = ReadRevision(document.RootElement);
                if (!SameInstant(storedRevision, expectedModifiedUtc))
                {
                    transaction.Rollback();
                    return WellBoreMutationResult.ConcurrencyConflict("expectedModifiedUtc",
                        $"Expected {expectedModifiedUtc:O}, but the stored definition was modified at {storedRevision:O}.");
                }
            }
            WellBoreMutationError? referenceError = referenceCheck(connection, transaction);
            if (referenceError != null)
            {
                transaction.Rollback();
                return WellBoreMutationResult.ReferenceConflict(referenceError);
            }
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $"DELETE FROM {table} WHERE ID=$id";
            command.Parameters.AddWithValue("$id", id.ToString());
            if (command.ExecuteNonQuery() != 1)
            {
                transaction.Rollback();
                return WellBoreMutationResult.StorageFailure();
            }
            transaction.Commit();
            return WellBoreMutationResult.Success();
        }
        catch (Exception ex) when (ex is SqliteException or JsonException)
        {
            transaction.Rollback();
            logger.LogError(ex, "Unable to delete {Table} record {RecordId}", table, id);
            return WellBoreMutationResult.StorageFailure();
        }
    }

    private static T? Read<T>(SqliteConnection connection, SqliteTransaction transaction, string table, string column, Guid id)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT {column} FROM {table} WHERE ID=$id";
        command.Parameters.AddWithValue("$id", id.ToString());
        return command.ExecuteScalar() is string json ? JsonSerializer.Deserialize<T>(json, JsonSettings.Options) : default;
    }

    private static bool SameInstant(DateTimeOffset left, DateTimeOffset right) => left.UtcTicks == right.UtcTicks;

    private static DateTimeOffset ReadRevision(JsonElement element)
    {
        foreach (string property in new[] { "LastModificationDate", "CreationDate" })
            if (element.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.String &&
                DateTimeOffset.TryParse(value.GetString(), out DateTimeOffset parsed)) return parsed;
        return DateTimeOffset.UnixEpoch;
    }
}

