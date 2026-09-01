using Microsoft.Data.Sqlite;
using OSDC.Drilling.WellBore.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace OSDC.Drilling.WellBore.Service.Managers;

internal static class WellBoreReferenceIntegrityValidator
{
    private sealed record CategoryDefinition(bool IsExclusive, bool HasValidityPeriod, HashSet<Guid> Options);

    public static List<WellBoreMutationError> ValidateWellBore(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Model.WellBore well)
    {
        Dictionary<Guid, CategoryDefinition> categories = ReadCategoryDefinitions(connection, transaction);
        HashSet<Guid> identities = ReadDefinitionIds<WellBoreIdentity>(connection, transaction, "WellBoreIdentityTable", "WellBoreIdentity", value => value.MetaInfo?.ID);

        List<WellBoreMutationError> errors = [];
        if (well.WellID == Guid.Empty)
            errors.Add(Error("WellID", "empty_uuid", "WellID must be null or a non-empty UUID."));
        if (well.RigID == Guid.Empty)
            errors.Add(Error("RigID", "empty_uuid", "RigID must be null or a non-empty UUID."));
        if (well.ParentWellBoreID == Guid.Empty)
            errors.Add(Error("ParentWellBoreID", "empty_uuid", "ParentWellBoreID must be null or a non-empty UUID."));

        HashSet<Guid> assignmentIds = [];
        for (int index = 0; index < (well.WellBoreFeatureAssignments?.Count ?? 0); index++)
        {
            WellBoreFeatureAssignment? assignment = well.WellBoreFeatureAssignments![index];
            string path = $"WellBoreFeatureAssignments[{index}]";
            if (assignment is null)
            {
                errors.Add(Error(path, "null_assignment", "Assignments cannot be null."));
                continue;
            }
            ValidateAssignmentId(assignment.ID, assignmentIds, $"{path}.ID", errors);
            ValidateFeatureAssignment(assignment, categories, path, errors);
        }
        for (int index = 0; index < (well.WellBoreIdentityAssignments?.Count ?? 0); index++)
        {
            WellBoreIdentityAssignment? assignment = well.WellBoreIdentityAssignments![index];
            string path = $"WellBoreIdentityAssignments[{index}]";
            if (assignment is null)
            {
                errors.Add(Error(path, "null_assignment", "Assignments cannot be null."));
                continue;
            }
            ValidateAssignmentId(assignment.ID, assignmentIds, $"{path}.ID", errors);
            ValidateRequiredReference(assignment.IdentityID, identities, $"{path}.IdentityID", "well_identity_not_found", errors);
            if (string.IsNullOrWhiteSpace(assignment.Value))
                errors.Add(Error($"{path}.Value", "value_required", "An identity assignment requires a non-blank value."));
        }

        ValidateExclusiveCategoryPeriods(well.WellBoreFeatureAssignments ?? [], categories, errors);
        return errors;
    }

    public static WellBoreMutationError? FindFeatureCategoryReferences(SqliteConnection connection, SqliteTransaction transaction,
        Guid categoryId, IReadOnlyCollection<Guid>? permittedOptionIds = null) =>
        FindReferences(connection, transaction,
            well => (well.WellBoreFeatureAssignments ?? [])
                .Where(value => value.FeatureCategoryID == categoryId &&
                    (permittedOptionIds == null || value.FeatureOptionID is Guid optionId && !permittedOptionIds.Contains(optionId)))
                .Any(),
            permittedOptionIds == null ? "WellBoreFeatureAssignments.FeatureCategoryID" : "WellBoreFeatureAssignments.FeatureOptionID",
            permittedOptionIds == null ? "catalog_in_use" : "catalog_option_in_use",
            permittedOptionIds == null
                ? "The feature category is referenced by one or more WellBores."
                : "The update removes a feature option referenced by one or more WellBores.");

    public static WellBoreMutationError? FindIdentityReferences(SqliteConnection connection, SqliteTransaction transaction, Guid identityId) =>
        FindReferences(connection, transaction,
            well => (well.WellBoreIdentityAssignments ?? []).Any(value => value.IdentityID == identityId),
            "WellBoreIdentityAssignments.IdentityID", "catalog_in_use",
            "The WellBore identity is referenced by one or more WellBores.");

    private static void ValidateFeatureAssignment(WellBoreFeatureAssignment assignment,
        IReadOnlyDictionary<Guid, CategoryDefinition> categories, string path, List<WellBoreMutationError> errors)
    {
        if (assignment.FeatureCategoryID is not Guid category || category == Guid.Empty)
        {
            errors.Add(Error($"{path}.FeatureCategoryID", "category_id_required", "A non-empty category UUID is required."));
            return;
        }
        if (!categories.TryGetValue(category, out CategoryDefinition? definition))
        {
            errors.Add(Error($"{path}.FeatureCategoryID", "category_not_found", $"No local category has UUID {category}."));
            return;
        }
        if (assignment.FeatureOptionID is not Guid option || option == Guid.Empty)
        {
            errors.Add(Error($"{path}.FeatureOptionID", "option_id_required", "A non-empty option UUID is required."));
            return;
        }
        if (!definition.Options.Contains(option))
        {
            errors.Add(Error($"{path}.FeatureOptionID", "option_not_in_category", $"Option UUID {option} does not belong to category UUID {category}."));
        }
        if (assignment.FromDate > assignment.ToDate)
            errors.Add(Error($"{path}.FromDate", "invalid_validity_period", "FromDate must be earlier than or equal to ToDate."));
        if (!definition.HasValidityPeriod && (assignment.FromDate is not null || assignment.ToDate is not null))
            errors.Add(Error(path, "validity_period_not_allowed", "This category does not support a validity period."));
    }

    private static void ValidateRequiredReference(Guid? id, IReadOnlySet<Guid> knownIds, string property,
        string code, List<WellBoreMutationError> errors)
    {
        if (id is not Guid value || value == Guid.Empty)
        {
            errors.Add(Error(property, "identity_id_required", "A non-empty identity UUID is required."));
            return;
        }
        if (!knownIds.Contains(value))
        {
            errors.Add(Error(property, code, $"No local catalog definition has UUID {id}."));
        }
    }

    private static void ValidateAssignmentId(Guid id, HashSet<Guid> knownIds, string property, List<WellBoreMutationError> errors)
    {
        if (id == Guid.Empty)
            errors.Add(Error(property, "assignment_id_required", "A non-empty assignment UUID is required."));
        else if (!knownIds.Add(id))
            errors.Add(Error(property, "duplicate_assignment_id", $"Assignment UUID {id} is used more than once."));
    }

    private static void ValidateExclusiveCategoryPeriods(IReadOnlyCollection<WellBoreFeatureAssignment> assignments,
        IReadOnlyDictionary<Guid, CategoryDefinition> categories, List<WellBoreMutationError> errors)
    {
        foreach (IGrouping<Guid, WellBoreFeatureAssignment> group in assignments
            .Where(value => value is not null && value.FeatureCategoryID is Guid)
            .GroupBy(value => value.FeatureCategoryID!.Value))
        {
            if (!categories.TryGetValue(group.Key, out CategoryDefinition? definition) || !definition.IsExclusive)
                continue;
            WellBoreFeatureAssignment[] values = group.ToArray();
            for (int left = 0; left < values.Length; left++)
            for (int right = left + 1; right < values.Length; right++)
            {
                if (!definition.HasValidityPeriod || PeriodsOverlap(values[left], values[right]))
                    errors.Add(Error("WellBoreFeatureAssignments", "exclusive_category_overlap",
                        $"Exclusive category UUID {group.Key} has assignments with overlapping validity periods."));
            }
        }
    }

    private static bool PeriodsOverlap(WellBoreFeatureAssignment left, WellBoreFeatureAssignment right) =>
        (left.ToDate is null || right.FromDate is null || left.ToDate >= right.FromDate) &&
        (right.ToDate is null || left.FromDate is null || right.ToDate >= left.FromDate);

    private static WellBoreMutationError? FindReferences(SqliteConnection connection, SqliteTransaction transaction,
        Func<Model.WellBore, bool> predicate, string property, string code, string message)
    {
        List<Guid> wellIds = ReadWellBores(connection, transaction)
            .Where(pair => predicate(pair.Value))
            .Select(pair => pair.Key)
            .Distinct()
            .OrderBy(value => value)
            .ToList();
        return wellIds.Count == 0
            ? null
            : new WellBoreMutationError { Property = property, Code = code, Message = message, ReferencingWellBoreIDs = wellIds };
    }

    private static Dictionary<Guid, Model.WellBore> ReadWellBores(SqliteConnection connection, SqliteTransaction transaction)
    {
        Dictionary<Guid, Model.WellBore> result = [];
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT ID, WellBore FROM WellBoreTable";
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            Model.WellBore? well = JsonSerializer.Deserialize<Model.WellBore>(reader.GetString(1), JsonSettings.Options);
            if (well != null)
            {
                result[reader.GetGuid(0)] = well;
            }
        }
        return result;
    }

    private static HashSet<Guid> ReadDefinitionIds<T>(SqliteConnection connection, SqliteTransaction transaction,
        string table, string column, Func<T, Guid?> idSelector)
    {
        HashSet<Guid> result = [];
        foreach (T value in ReadDocuments<T>(connection, transaction, table, column))
        {
            if (idSelector(value) is Guid id && id != Guid.Empty)
            {
                result.Add(id);
            }
        }
        return result;
    }

    private static Dictionary<Guid, CategoryDefinition> ReadCategoryDefinitions(
        SqliteConnection connection, SqliteTransaction transaction)
    {
        Dictionary<Guid, CategoryDefinition> result = [];
        foreach (WellBoreFeatureCategory category in ReadDocuments<WellBoreFeatureCategory>(connection, transaction,
            "WellBoreFeatureCategoryTable", "WellBoreFeatureCategory"))
        {
            if (category.MetaInfo?.ID is not Guid id || id == Guid.Empty)
            {
                continue;
            }
            result[id] = new CategoryDefinition(category.IsExclusive, category.HasValidityPeriod,
                (category.Options ?? []).Select(value => value.ID).Where(value => value != Guid.Empty).ToHashSet());
        }
        return result;
    }

    private static List<T> ReadDocuments<T>(SqliteConnection connection, SqliteTransaction transaction, string table, string column)
    {
        List<T> result = [];
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT {column} FROM {table}";
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            T? value = JsonSerializer.Deserialize<T>(reader.GetString(0), JsonSettings.Options);
            if (value != null)
            {
                result.Add(value);
            }
        }
        return result;
    }

    private static WellBoreMutationError Error(string property, string code, string message) =>
        new() { Property = property, Code = code, Message = message };
}

