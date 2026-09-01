using Microsoft.Data.Sqlite;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.Drilling.WellBore.Model;
using OSDC.Drilling.WellBore.Service.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using WellBoreModel = OSDC.Drilling.WellBore.Model.WellBore;

namespace OSDC.Drilling.WellBore.Service;

public enum WellBoreBatchRestoreFailureKind { None, InvalidRequest, Conflict, StorageFailure }

public sealed class WellBoreBatchRestoreOutcome
{
    public WellBoreBatchRestoreResponse? Response { get; init; }
    public WellBoreBatchErrorEnvelope? Error { get; init; }
    public WellBoreBatchRestoreFailureKind FailureKind { get; init; }
    public bool IsSuccess => Response != null && FailureKind == WellBoreBatchRestoreFailureKind.None;
}

/// <summary>Validates, maps catalogs, and restores the complete batch in one transaction.</summary>
public static class WellBoreBatchRestorer
{
    public static WellBoreBatchRestoreOutcome Restore(SqliteConnection connection,
        WellBoreBatchRestoreRequest? request, DateTimeOffset restoredAtUtc)
    {
        List<WellBoreBatchError> validationErrors = ValidateRequest(request);
        if (validationErrors.Count != 0) return Failure(WellBoreBatchRestoreFailureKind.InvalidRequest,
            "invalid_batch_restore_request", "The WellBore batch-restore request is invalid. No changes were made.", validationErrors);

        using SqliteTransaction transaction = connection.BeginTransaction();
        try
        {
            CatalogState catalogs = CatalogState.Load(connection, transaction);
            List<WellBoreModel> wellBores = CloneWellBores(request!.Document!.WellBores);
            List<WellBoreBatchCatalogMapping> mappings = [];
            List<WellBoreBatchError> mappingErrors = [];
            int createdDefinitions = 0;
            int createdOptions = 0;
            bool createMissing = request.CatalogPolicy == WellBoreBatchCatalogRestorePolicy.MapOrCreateMissing;

            ResolveDependencies(request.Document.CatalogDependencies, catalogs, createMissing, mappings,
                mappingErrors, restoredAtUtc, ref createdDefinitions, ref createdOptions);
            if (mappingErrors.Count != 0)
            {
                transaction.Rollback();
                return Failure(WellBoreBatchRestoreFailureKind.Conflict, "catalog_restore_conflict",
                    "Catalog references could not be resolved unambiguously. No changes were made.", mappingErrors);
            }
            RewriteReferences(wellBores, mappings);

            List<PreparedWellBore> prepared = PrepareWellBores(wellBores);
            List<bool> exists = prepared.Select(value => RowExists(connection, transaction, value.ID)).ToList();
            if (request.ConflictPolicy == WellBoreBatchRestoreConflictPolicy.FailIfExists)
            {
                List<WellBoreBatchError> conflicts = prepared.Select((value, index) => (value, index))
                    .Where(value => exists[value.index])
                    .Select(value => Error(value.index, "Document.WellBores", "well_already_exists",
                        $"A stored WellBore already has UUID '{value.value.ID}'."))
                    .ToList();
                if (conflicts.Count != 0)
                {
                    transaction.Rollback();
                    return Failure(WellBoreBatchRestoreFailureKind.Conflict, "well_restore_conflict",
                        "One or more WellBore UUIDs already exist. No changes were made.", conflicts);
                }
            }

            catalogs.Save(connection, transaction);
            List<WellBoreBatchError> assignmentErrors = [];
            for (int index = 0; index < wellBores.Count; index++)
            {
                assignmentErrors.AddRange(WellBoreReferenceIntegrityValidator
                    .ValidateWellBore(connection, transaction, wellBores[index])
                    .Select(error => Error(index, $"Document.WellBores[{index}].{error.Property}", error.Code, error.Message)));
            }
            if (assignmentErrors.Count != 0)
            {
                transaction.Rollback();
                return Failure(WellBoreBatchRestoreFailureKind.InvalidRequest, "invalid_wellbore_assignments",
                    "One or more restored WellBores contain invalid identity or feature assignments. No changes were made.",
                    assignmentErrors);
            }
            SaveWellBores(connection, transaction, prepared, request.ConflictPolicy);
            transaction.Commit();
            return new WellBoreBatchRestoreOutcome
            {
                Response = new WellBoreBatchRestoreResponse
                {
                    RestoredAtUtc = restoredAtUtc.ToUniversalTime(),
                    CreatedCount = exists.Count(value => !value),
                    ReplacedCount = exists.Count(value => value),
                    CreatedCatalogDefinitionCount = createdDefinitions,
                    CreatedCatalogOptionCount = createdOptions,
                    CatalogMappings = mappings,
                    WellBoreIDs = prepared.Select(value => value.ID).ToList()
                }
            };
        }
        catch (Exception exception) when (exception is SqliteException or JsonException or InvalidOperationException or KeyNotFoundException)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return StorageFailure($"The WellBore database rejected the batch. No changes were committed. {exception.Message}");
        }
    }

    public static WellBoreBatchRestoreOutcome StorageFailure(string message) => Failure(
        WellBoreBatchRestoreFailureKind.StorageFailure, "well_restore_failed", message,
        [Error(null, "Document.WellBores", "storage_failure", "The complete restore transaction was rolled back.")]);

    public static List<WellBoreBatchError> ValidateRequest(WellBoreBatchRestoreRequest? request)
    {
        if (request == null) return [Error(null, "Request", "required", "A batch-restore request is required.")];
        List<WellBoreBatchError> errors = [];
        if (request.ConflictPolicy is not WellBoreBatchRestoreConflictPolicy.FailIfExists and not WellBoreBatchRestoreConflictPolicy.ReplaceExisting)
            errors.Add(Error(null, "ConflictPolicy", "invalid_conflict_policy", "ConflictPolicy must be FailIfExists or ReplaceExisting."));
        if (request.CatalogPolicy is not WellBoreBatchCatalogRestorePolicy.MapExisting and not WellBoreBatchCatalogRestorePolicy.MapOrCreateMissing)
            errors.Add(Error(null, "CatalogPolicy", "invalid_catalog_policy", "CatalogPolicy must be MapExisting or MapOrCreateMissing."));
        WellBoreBatchExportDocument? document = request.Document;
        if (document == null)
        {
            errors.Add(Error(null, "Document", "required", "A batch-export document is required."));
            return errors;
        }
        if (document.FormatIdentifier != WellBoreBatchExportDocument.CurrentFormatIdentifier)
            errors.Add(Error(null, "Document.FormatIdentifier", "unsupported_format", $"FormatIdentifier must be '{WellBoreBatchExportDocument.CurrentFormatIdentifier}'."));
        if (document.SchemaVersion != WellBoreBatchExportDocument.CurrentSchemaVersion)
            errors.Add(Error(null, "Document.SchemaVersion", "unsupported_schema_version", $"SchemaVersion must be {WellBoreBatchExportDocument.CurrentSchemaVersion}."));
        if (document.ExportedAtUtc == default || document.ExportedAtUtc.Offset != TimeSpan.Zero)
            errors.Add(Error(null, "Document.ExportedAtUtc", "invalid_export_timestamp", "ExportedAtUtc must be a non-default UTC timestamp."));
        ValidateDependencies(document.CatalogDependencies, errors);
        if (document.WellBores == null || document.WellBores.Count == 0)
        {
            errors.Add(Error(null, "Document.WellBores", "required", "At least one WellBore is required for restore."));
            return errors;
        }
        ValidateReferences(document.WellBores, document.CatalogDependencies, errors);
        Dictionary<Guid, int> positions = [];
        for (int index = 0; index < document.WellBores.Count; index++)
        {
            WellBoreModel? wellBore = document.WellBores[index];
            Guid? id = wellBore?.MetaInfo?.ID;
            if (wellBore == null) errors.Add(Error(index, "Document.WellBores", "null_well", "A restored WellBore must not be null."));
            else if (id == null || id == Guid.Empty) errors.Add(Error(index, "Document.WellBores.MetaInfo.ID", "empty_uuid", "Every restored WellBore must have a non-empty UUID."));
            else if (positions.TryGetValue(id.Value, out int first)) errors.Add(Error(index, "Document.WellBores.MetaInfo.ID", "duplicate_uuid", $"WellBore UUID '{id}' duplicates position {first}."));
            else positions.Add(id.Value, index);
            if (wellBore?.WellID == Guid.Empty) errors.Add(Error(index, "Document.WellBores.WellID", "empty_uuid", "WellID must be omitted or a non-empty UUID."));
            if (wellBore?.RigID == Guid.Empty) errors.Add(Error(index, "Document.WellBores.RigID", "empty_uuid", "RigID must be omitted or a non-empty UUID."));
            if (wellBore?.ParentWellBoreID == Guid.Empty) errors.Add(Error(index, "Document.WellBores.ParentWellBoreID", "empty_uuid", "ParentWellBoreID must be omitted or a non-empty UUID."));
        }
        return errors;
    }

    private static void ValidateDependencies(WellBoreBatchCatalogDependencies? dependencies, List<WellBoreBatchError> errors)
    {
        if (dependencies == null)
        {
            errors.Add(Error(null, "Document.CatalogDependencies", "required", "CatalogDependencies is required."));
            return;
        }
        HashSet<Guid> ids = [];
        void Check(Guid id, string? name, string property)
        {
            if (id == Guid.Empty) errors.Add(Error(null, property, "empty_uuid", "Catalog UUIDs must be non-empty."));
            else if (!ids.Add(id)) errors.Add(Error(null, property, "duplicate_uuid", $"Catalog UUID '{id}' occurs more than once."));
            if (string.IsNullOrWhiteSpace(name)) errors.Add(Error(null, property + ".Name", "required", "Catalog names must not be empty."));
        }
        foreach (WellBoreIdentity? identity in dependencies.Identities ?? [])
            Check(identity?.MetaInfo?.ID ?? Guid.Empty, identity?.Name, "Document.CatalogDependencies.Identities");
        foreach (WellBoreFeatureCategory? category in dependencies.FeatureCategories ?? [])
        {
            Check(category?.MetaInfo?.ID ?? Guid.Empty, category?.Name, "Document.CatalogDependencies.FeatureCategories");
            foreach (WellBoreFeatureOption option in category?.Options ?? [])
                Check(option.ID, option.Name, "Document.CatalogDependencies.FeatureCategories.Options");
        }
    }

    private static void ValidateReferences(List<WellBoreModel> wellBores, WellBoreBatchCatalogDependencies? dependencies,
        List<WellBoreBatchError> errors)
    {
        if (dependencies == null) return;
        HashSet<Guid> identityIds = (dependencies.Identities ?? [])
            .Where(value => value?.MetaInfo?.ID is Guid id && id != Guid.Empty)
            .Select(value => value.MetaInfo!.ID).ToHashSet();
        Dictionary<Guid, HashSet<Guid>> categoryOptions = [];
        foreach (WellBoreFeatureCategory? category in dependencies.FeatureCategories ?? [])
        {
            if (category?.MetaInfo?.ID is not Guid categoryId || categoryId == Guid.Empty || categoryOptions.ContainsKey(categoryId))
                continue;
            categoryOptions.Add(categoryId, (category.Options ?? []).Where(option => option != null).Select(option => option.ID).ToHashSet());
        }
        for (int index = 0; index < wellBores.Count; index++)
        {
            foreach (WellBoreIdentityAssignment? assignment in wellBores[index]?.WellBoreIdentityAssignments ?? [])
            {
                if (assignment?.IdentityID is not Guid id || id == Guid.Empty || !identityIds.Contains(id))
                    errors.Add(Error(index, "Document.WellBores.WellBoreIdentityAssignments.IdentityID", "catalog_dependency_missing", $"Referenced identity '{assignment?.IdentityID}' is absent from CatalogDependencies."));
            }
            foreach (WellBoreFeatureAssignment? assignment in wellBores[index]?.WellBoreFeatureAssignments ?? [])
            {
                if (assignment?.FeatureCategoryID is not Guid categoryId || !categoryOptions.TryGetValue(categoryId, out HashSet<Guid>? options))
                    errors.Add(Error(index, "Document.WellBores.WellBoreFeatureAssignments.FeatureCategoryID", "catalog_dependency_missing", $"Referenced category '{assignment?.FeatureCategoryID}' is absent from CatalogDependencies."));
                else if (assignment.FeatureOptionID is not Guid optionId || !options.Contains(optionId))
                    errors.Add(Error(index, "Document.WellBores.WellBoreFeatureAssignments.FeatureOptionID", "catalog_dependency_missing", $"Referenced option '{assignment.FeatureOptionID}' is absent from category '{categoryId}'."));
            }
        }
    }

    private static void ResolveDependencies(WellBoreBatchCatalogDependencies dependencies, CatalogState local,
        bool createMissing, List<WellBoreBatchCatalogMapping> mappings, List<WellBoreBatchError> errors,
        DateTimeOffset now, ref int createdDefinitions, ref int createdOptions)
    {
        foreach (WellBoreIdentity source in dependencies.Identities ?? [])
        {
            Guid sourceId = source.MetaInfo!.ID;
            WellBoreIdentity? target = ResolveFlat(sourceId, source.Name, local.Identities, createMissing, errors);
            bool created = false;
            if (target == null && createMissing && !HasErrorFor(errors, sourceId))
            {
                target = new WellBoreIdentity { MetaInfo = new MetaInfo { ID = Guid.NewGuid() }, Name = source.Name,
                    CreationDate = now, LastModificationDate = now };
                local.Identities.Add(target); local.DirtyIdentities.Add(target); createdDefinitions++; created = true;
            }
            if (target != null) AddMapping(mappings, "Identity", source.Name, sourceId, target.MetaInfo!.ID,
                sourceId == target.MetaInfo.ID ? "exact_uuid" : created ? "created" : "normalized_name");
        }
        foreach (WellBoreFeatureCategory source in dependencies.FeatureCategories ?? [])
            ResolveCategory(source, local, createMissing, mappings, errors, now, ref createdDefinitions, ref createdOptions);
    }

    private static void ResolveCategory(WellBoreFeatureCategory source, CatalogState local, bool createMissing,
        List<WellBoreBatchCatalogMapping> mappings, List<WellBoreBatchError> errors, DateTimeOffset now,
        ref int createdDefinitions, ref int createdOptions)
    {
        Guid sourceId = source.MetaInfo!.ID;
        WellBoreFeatureCategory? target = local.Features.FirstOrDefault(value => value.MetaInfo!.ID == sourceId);
        bool created = false;
        if (target != null && (!SameName(target.Name, source.Name) || target.IsExclusive != source.IsExclusive || target.HasValidityPeriod != source.HasValidityPeriod))
        {
            AddSemanticConflict(errors, "feature category", sourceId, source.Name); return;
        }
        if (target == null)
        {
            List<WellBoreFeatureCategory> matches = local.Features.Where(value => SameName(value.Name, source.Name)).ToList();
            if (matches.Count > 1) { AddAmbiguous(errors, "feature category", sourceId, source.Name); return; }
            if (matches.Count == 1)
            {
                target = matches[0];
                if (target.IsExclusive != source.IsExclusive || target.HasValidityPeriod != source.HasValidityPeriod)
                { AddSemanticConflict(errors, "feature category", sourceId, source.Name); return; }
            }
            else if (createMissing)
            {
                target = new WellBoreFeatureCategory { MetaInfo = new MetaInfo { ID = Guid.NewGuid() }, Name = source.Name,
                    IsExclusive = source.IsExclusive, HasValidityPeriod = source.HasValidityPeriod, Options = [],
                    CreationDate = now, LastModificationDate = now };
                local.Features.Add(target); local.DirtyFeatures.Add(target); createdDefinitions++; created = true;
            }
            else { AddMissing(errors, "feature category", sourceId, source.Name); return; }
        }
        AddMapping(mappings, "FeatureCategory", source.Name, sourceId, target.MetaInfo!.ID,
            sourceId == target.MetaInfo.ID ? "exact_uuid" : created ? "created" : "normalized_name");
        foreach (WellBoreFeatureOption sourceOption in source.Options ?? [])
        {
            WellBoreFeatureOption? targetOption = (target.Options ?? []).FirstOrDefault(value => value.ID == sourceOption.ID);
            bool optionCreated = false;
            if (targetOption != null && !SameName(targetOption.Name, sourceOption.Name))
            { AddSemanticConflict(errors, "feature option", sourceOption.ID, sourceOption.Name); continue; }
            if (targetOption == null)
            {
                List<WellBoreFeatureOption> matches = (target.Options ?? []).Where(value => SameName(value.Name, sourceOption.Name)).ToList();
                if (matches.Count > 1) { AddAmbiguous(errors, "feature option", sourceOption.ID, sourceOption.Name); continue; }
                if (matches.Count == 1) targetOption = matches[0];
                else if (createMissing)
                {
                    targetOption = new WellBoreFeatureOption { ID = Guid.NewGuid(), Name = sourceOption.Name };
                    target.Options ??= []; target.Options.Add(targetOption); target.LastModificationDate = now;
                    local.DirtyFeatures.Add(target); createdOptions++; optionCreated = true;
                }
                else { AddMissing(errors, "feature option", sourceOption.ID, sourceOption.Name); continue; }
            }
            AddMapping(mappings, "FeatureOption", sourceOption.Name, sourceOption.ID, targetOption.ID,
                sourceOption.ID == targetOption.ID ? "exact_uuid" : optionCreated ? "created" : "normalized_name");
        }
    }

    private static WellBoreIdentity? ResolveFlat(Guid sourceId, string? sourceName, List<WellBoreIdentity> local,
        bool createMissing, List<WellBoreBatchError> errors)
    {
        WellBoreIdentity? exact = local.FirstOrDefault(value => value.MetaInfo!.ID == sourceId);
        if (exact != null)
        {
            if (!SameName(exact.Name, sourceName)) AddSemanticConflict(errors, "identity", sourceId, sourceName);
            return HasErrorFor(errors, sourceId) ? null : exact;
        }
        List<WellBoreIdentity> matches = local.Where(value => SameName(value.Name, sourceName)).ToList();
        if (matches.Count == 1) return matches[0];
        if (matches.Count > 1) AddAmbiguous(errors, "identity", sourceId, sourceName);
        else if (!createMissing) AddMissing(errors, "identity", sourceId, sourceName);
        return null;
    }

    private static void RewriteReferences(List<WellBoreModel> wellBores, List<WellBoreBatchCatalogMapping> mappings)
    {
        Dictionary<Guid, Guid> map = mappings.ToDictionary(value => value.SourceID, value => value.LocalID);
        foreach (WellBoreModel wellBore in wellBores)
        {
            foreach (WellBoreIdentityAssignment assignment in wellBore.WellBoreIdentityAssignments ?? [])
                if (assignment.IdentityID is Guid id) assignment.IdentityID = map[id];
            foreach (WellBoreFeatureAssignment assignment in wellBore.WellBoreFeatureAssignments ?? [])
            {
                if (assignment.FeatureCategoryID is Guid categoryId) assignment.FeatureCategoryID = map[categoryId];
                if (assignment.FeatureOptionID is Guid optionId) assignment.FeatureOptionID = map[optionId];
            }
        }
    }

    private static List<WellBoreModel> CloneWellBores(List<WellBoreModel> values) => JsonSerializer.Deserialize<List<WellBoreModel>>(
        JsonSerializer.Serialize(values, JsonSettings.Options), JsonSettings.Options) ?? throw new JsonException("WellBores could not be cloned.");
    private static List<PreparedWellBore> PrepareWellBores(List<WellBoreModel> values) => values.Select(value => new PreparedWellBore(
        value.MetaInfo!.ID, JsonSerializer.Serialize(value.MetaInfo, JsonSettings.Options),
        value.WellID?.ToString(), value.RigID?.ToString(), value.IsSidetrack,
        value.ParentWellBoreID?.ToString(),
        JsonSerializer.Serialize(value, JsonSettings.Options))).ToList();
    private static bool RowExists(SqliteConnection connection, SqliteTransaction transaction, Guid id)
    { using SqliteCommand command = connection.CreateCommand(); command.Transaction = transaction; command.CommandText = "SELECT COUNT(*) FROM WellBoreTable WHERE ID=$id"; command.Parameters.AddWithValue("$id", id.ToString()); return Convert.ToInt64(command.ExecuteScalar()) != 0; }
    private static void SaveWellBores(SqliteConnection connection, SqliteTransaction transaction,
        List<PreparedWellBore> wellBores, WellBoreBatchRestoreConflictPolicy policy)
    {
        foreach (PreparedWellBore wellBore in wellBores)
        {
            using SqliteCommand command = connection.CreateCommand(); command.Transaction = transaction;
            command.CommandText = policy == WellBoreBatchRestoreConflictPolicy.ReplaceExisting
                ? "INSERT INTO WellBoreTable (ID,MetaInfo,WellID,RigID,IsSidetrack,ParentWellBoreID,WellBore) VALUES ($id,$meta,$well,$rig,$sidetrack,$parent,$doc) ON CONFLICT(ID) DO UPDATE SET MetaInfo=excluded.MetaInfo,WellID=excluded.WellID,RigID=excluded.RigID,IsSidetrack=excluded.IsSidetrack,ParentWellBoreID=excluded.ParentWellBoreID,WellBore=excluded.WellBore"
                : "INSERT INTO WellBoreTable (ID,MetaInfo,WellID,RigID,IsSidetrack,ParentWellBoreID,WellBore) VALUES ($id,$meta,$well,$rig,$sidetrack,$parent,$doc)";
            command.Parameters.AddWithValue("$id", wellBore.ID.ToString());
            command.Parameters.AddWithValue("$meta", wellBore.MetaInfoJson);
            command.Parameters.AddWithValue("$well", (object?)wellBore.WellID ?? DBNull.Value);
            command.Parameters.AddWithValue("$rig", (object?)wellBore.RigID ?? DBNull.Value);
            command.Parameters.AddWithValue("$sidetrack", wellBore.IsSidetrack ? 1 : 0);
            command.Parameters.AddWithValue("$parent", (object?)wellBore.ParentWellBoreID ?? DBNull.Value);
            command.Parameters.AddWithValue("$doc", wellBore.WellBoreJson);
            command.ExecuteNonQuery();
        }
    }

    private static string Normalize(string? value) => string.Join(' ', (value ?? "").Normalize(NormalizationForm.FormKC)
        .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();
    private static bool SameName(string? left, string? right) => Normalize(left) == Normalize(right);
    private static bool HasErrorFor(List<WellBoreBatchError> errors, Guid id) => errors.Any(error => error.Message.Contains(id.ToString(), StringComparison.OrdinalIgnoreCase));
    private static void AddMissing(List<WellBoreBatchError> errors, string kind, Guid id, string? name) => errors.Add(Error(null, $"Document.CatalogDependencies[{id}]", "catalog_definition_missing", $"No compatible local {kind} exists for '{name}' ({id}), and creation is disabled."));
    private static void AddAmbiguous(List<WellBoreBatchError> errors, string kind, Guid id, string? name) => errors.Add(Error(null, $"Document.CatalogDependencies[{id}]", "ambiguous_catalog_match", $"More than one local {kind} has normalized name '{name}' for source UUID '{id}'."));
    private static void AddSemanticConflict(List<WellBoreBatchError> errors, string kind, Guid id, string? name) => errors.Add(Error(null, $"Document.CatalogDependencies[{id}]", "catalog_semantic_conflict", $"The local {kind} corresponding to '{name}' ({id}) has incompatible semantics."));
    private static void AddMapping(List<WellBoreBatchCatalogMapping> mappings, string catalog, string? name, Guid source, Guid local, string resolution) => mappings.Add(new() { Catalog = catalog, Name = name ?? "", SourceID = source, LocalID = local, Resolution = resolution });
    private static WellBoreBatchRestoreOutcome Failure(WellBoreBatchRestoreFailureKind kind, string error, string message, List<WellBoreBatchError> errors) => new() { FailureKind = kind, Error = new() { Error = error, Message = message, Errors = errors } };
    private static WellBoreBatchError Error(int? index, string property, string code, string message) => new() { PositionIndex = index, Property = property, Code = code, Message = message };
    private sealed record PreparedWellBore(Guid ID, string MetaInfoJson, string? WellID, string? RigID,
        bool IsSidetrack, string? ParentWellBoreID, string WellBoreJson);

    private sealed class CatalogState
    {
        public List<WellBoreIdentity> Identities { get; } = [];
        public List<WellBoreFeatureCategory> Features { get; } = [];
        public HashSet<WellBoreIdentity> DirtyIdentities { get; } = [];
        public HashSet<WellBoreFeatureCategory> DirtyFeatures { get; } = [];

        public static CatalogState Load(SqliteConnection connection, SqliteTransaction transaction)
        {
            CatalogState state = new();
            state.Identities.AddRange(Read<WellBoreIdentity>(connection, transaction, "WellBoreIdentityTable", "WellBoreIdentity"));
            state.Features.AddRange(Read<WellBoreFeatureCategory>(connection, transaction, "WellBoreFeatureCategoryTable", "WellBoreFeatureCategory"));
            return state;
        }
        private static List<T> Read<T>(SqliteConnection connection, SqliteTransaction transaction, string table, string column)
        {
            using SqliteCommand command = connection.CreateCommand(); command.Transaction = transaction;
            command.CommandText = $"SELECT {column} FROM {table}";
            using SqliteDataReader reader = command.ExecuteReader(); List<T> result = [];
            while (reader.Read()) result.Add(JsonSerializer.Deserialize<T>(reader.GetString(0), JsonSettings.Options) ?? throw new JsonException($"Invalid {table} document."));
            return result;
        }
        public void Save(SqliteConnection connection, SqliteTransaction transaction)
        {
            foreach (WellBoreIdentity value in DirtyIdentities)
            {
                using SqliteCommand command = connection.CreateCommand(); command.Transaction = transaction;
                command.CommandText = "INSERT INTO WellBoreIdentityTable (ID,MetaInfo,Name,CreationDate,LastModificationDate,WellBoreIdentity) VALUES ($id,$meta,$name,$created,$modified,$doc)";
                AddCommon(command, value.MetaInfo!, value.Name, value.CreationDate, value.LastModificationDate, value); command.ExecuteNonQuery();
            }
            foreach (WellBoreFeatureCategory value in DirtyFeatures)
            {
                using SqliteCommand command = connection.CreateCommand(); command.Transaction = transaction;
                command.CommandText = "INSERT INTO WellBoreFeatureCategoryTable (ID,MetaInfo,Name,IsExclusive,HasValidityPeriod,CreationDate,LastModificationDate,WellBoreFeatureCategory) VALUES ($id,$meta,$name,$exclusive,$validity,$created,$modified,$doc) ON CONFLICT(ID) DO UPDATE SET MetaInfo=excluded.MetaInfo,Name=excluded.Name,IsExclusive=excluded.IsExclusive,HasValidityPeriod=excluded.HasValidityPeriod,CreationDate=excluded.CreationDate,LastModificationDate=excluded.LastModificationDate,WellBoreFeatureCategory=excluded.WellBoreFeatureCategory";
                AddCommon(command, value.MetaInfo!, value.Name, value.CreationDate, value.LastModificationDate, value);
                command.Parameters.AddWithValue("$exclusive", value.IsExclusive ? 1 : 0);
                command.Parameters.AddWithValue("$validity", value.HasValidityPeriod ? 1 : 0); command.ExecuteNonQuery();
            }
        }
        private static void AddCommon(SqliteCommand command, MetaInfo metaInfo, string? name,
            DateTimeOffset? created, DateTimeOffset? modified, object document)
        {
            command.Parameters.AddWithValue("$id", metaInfo.ID.ToString());
            command.Parameters.AddWithValue("$meta", JsonSerializer.Serialize(metaInfo, JsonSettings.Options));
            command.Parameters.AddWithValue("$name", name ?? "");
            command.Parameters.AddWithValue("$created", created?.ToString(Managers.SqlConnectionManager.DATE_TIME_FORMAT) ?? "");
            command.Parameters.AddWithValue("$modified", modified?.ToString(Managers.SqlConnectionManager.DATE_TIME_FORMAT) ?? "");
            command.Parameters.AddWithValue("$doc", JsonSerializer.Serialize(document, JsonSettings.Options));
        }
    }
}
