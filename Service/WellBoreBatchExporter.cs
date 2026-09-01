using OSDC.Drilling.WellBore.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OSDC.Drilling.WellBore.Service;

public enum WellBoreBatchExportFailureKind { None, InvalidRequest, WellNotFound, StorageFailure }

public sealed class WellBoreBatchExportOutcome
{
    public WellBoreBatchExportDocument? Document { get; init; }
    public WellBoreBatchErrorEnvelope? Error { get; init; }
    public WellBoreBatchExportFailureKind FailureKind { get; init; }
    public bool IsSuccess => Document != null && FailureKind == WellBoreBatchExportFailureKind.None;
}

public static class WellBoreBatchExporter
{
    public static WellBoreBatchExportOutcome Create(WellBoreBatchExportRequest? request,
        IEnumerable<Model.WellBore?> snapshot, DateTimeOffset exportedAtUtc,
        IEnumerable<WellBoreIdentity> identities, IEnumerable<WellBoreFeatureCategory> categories)
    {
        List<WellBoreBatchError> errors = ValidateRequest(request);
        if (errors.Count != 0) return Failure(WellBoreBatchExportFailureKind.InvalidRequest,
            "invalid_batch_export_request", "The WellBore batch-export request is invalid.", errors);

        Dictionary<Guid, Model.WellBore> byId = [];
        int position = 0;
        foreach (Model.WellBore? wellBore in snapshot)
        {
            Guid? id = wellBore?.MetaInfo?.ID;
            if (wellBore == null || id == null || id == Guid.Empty || !byId.TryAdd(id.Value, wellBore))
                return Failure(WellBoreBatchExportFailureKind.StorageFailure, "well_export_failed",
                    "A stored WellBore could not be represented in the export.",
                    [Error(position, "WellBores", "invalid_stored_well", "A stored WellBore is null, has no UUID, or duplicates another UUID.")]);
            position++;
        }

        List<Model.WellBore> selected;
        if (request!.Scope == WellBoreBatchExportScope.All)
            selected = byId.OrderBy(pair => pair.Key).Select(pair => pair.Value).ToList();
        else
        {
            selected = [];
            for (int index = 0; index < request.WellBoreIDs!.Count; index++)
            {
                Guid id = request.WellBoreIDs[index];
                if (byId.TryGetValue(id, out Model.WellBore? wellBore)) selected.Add(wellBore);
                else errors.Add(Error(index, "WellBoreIDs", "well_not_found", $"No stored WellBore has UUID '{id}'."));
            }
            if (errors.Count != 0) return Failure(WellBoreBatchExportFailureKind.WellNotFound,
                "well_not_found", "One or more selected WellBores do not exist.", errors);
        }

        WellBoreBatchCatalogDependencies dependencies = BuildDependencies(selected, identities, categories, errors);
        if (errors.Count != 0) return Failure(WellBoreBatchExportFailureKind.StorageFailure,
            "well_export_dependency_missing", "The export could not include every referenced local catalog definition.", errors);

        return new WellBoreBatchExportOutcome
        {
            Document = new WellBoreBatchExportDocument
            {
                ExportedAtUtc = exportedAtUtc.ToUniversalTime(),
                CatalogDependencies = dependencies,
                WellBores = selected
            }
        };
    }

    public static WellBoreBatchExportOutcome StorageFailure(string message) => Failure(
        WellBoreBatchExportFailureKind.StorageFailure, "well_export_failed", message,
        [Error(null, "Document", "storage_failure", "The export snapshot could not be produced.")]);

    private static WellBoreBatchCatalogDependencies BuildDependencies(IReadOnlyList<Model.WellBore> wellBores,
        IEnumerable<WellBoreIdentity> identities, IEnumerable<WellBoreFeatureCategory> categories,
        List<WellBoreBatchError> errors)
    {
        Dictionary<Guid, WellBoreIdentity> identityIndex = identities
            .Where(value => value?.MetaInfo?.ID is Guid id && id != Guid.Empty)
            .GroupBy(value => value.MetaInfo!.ID).ToDictionary(group => group.Key, group => group.First());
        Dictionary<Guid, WellBoreFeatureCategory> categoryIndex = categories
            .Where(value => value?.MetaInfo?.ID is Guid id && id != Guid.Empty)
            .GroupBy(value => value.MetaInfo!.ID).ToDictionary(group => group.Key, group => group.First());
        HashSet<Guid> identityIds = [];
        Dictionary<Guid, HashSet<Guid>> optionIdsByCategory = [];

        for (int index = 0; index < wellBores.Count; index++)
        {
            foreach (WellBoreIdentityAssignment? assignment in wellBores[index].WellBoreIdentityAssignments ?? [])
            {
                if (assignment?.IdentityID is Guid id && id != Guid.Empty) identityIds.Add(id);
                else errors.Add(Error(index, "WellBores.WellBoreIdentityAssignments.IdentityID", "invalid_catalog_reference", "Identity references must be non-empty UUIDs."));
            }
            foreach (WellBoreFeatureAssignment? assignment in wellBores[index].WellBoreFeatureAssignments ?? [])
            {
                if (assignment?.FeatureCategoryID is not Guid categoryId || categoryId == Guid.Empty ||
                    assignment.FeatureOptionID is not Guid optionId || optionId == Guid.Empty)
                {
                    errors.Add(Error(index, "WellBores.WellBoreFeatureAssignments", "invalid_catalog_reference", "Feature category and option references must be non-empty UUIDs."));
                    continue;
                }
                if (!optionIdsByCategory.TryGetValue(categoryId, out HashSet<Guid>? optionIds))
                    optionIdsByCategory.Add(categoryId, optionIds = []);
                optionIds.Add(optionId);
            }
        }

        WellBoreBatchCatalogDependencies result = new();
        foreach (Guid id in identityIds.Order())
        {
            if (identityIndex.TryGetValue(id, out WellBoreIdentity? identity)) result.Identities.Add(identity);
            else errors.Add(Error(null, "CatalogDependencies.Identities", "referenced_definition_missing", $"Referenced identity '{id}' does not exist."));
        }
        foreach ((Guid categoryId, HashSet<Guid> requiredOptions) in optionIdsByCategory.OrderBy(pair => pair.Key))
        {
            if (!categoryIndex.TryGetValue(categoryId, out WellBoreFeatureCategory? category))
            {
                errors.Add(Error(null, "CatalogDependencies.FeatureCategories", "referenced_definition_missing", $"Referenced feature category '{categoryId}' does not exist."));
                continue;
            }
            Dictionary<Guid, WellBoreFeatureOption> available = (category.Options ?? []).Where(value => value.ID != Guid.Empty)
                .GroupBy(value => value.ID).ToDictionary(group => group.Key, group => group.First());
            List<WellBoreFeatureOption> options = [];
            foreach (Guid optionId in requiredOptions.Order())
            {
                if (available.TryGetValue(optionId, out WellBoreFeatureOption? option)) options.Add(option);
                else errors.Add(Error(null, "CatalogDependencies.FeatureCategories.Options", "referenced_option_missing",
                    $"Referenced option '{optionId}' does not exist in category '{categoryId}'."));
            }
            result.FeatureCategories.Add(new WellBoreFeatureCategory
            {
                MetaInfo = category.MetaInfo, Name = category.Name, IsExclusive = category.IsExclusive,
                HasValidityPeriod = category.HasValidityPeriod, Options = options,
                CreationDate = category.CreationDate, LastModificationDate = category.LastModificationDate
            });
        }
        return result;
    }

    private static List<WellBoreBatchError> ValidateRequest(WellBoreBatchExportRequest? request)
    {
        if (request == null) return [Error(null, "Request", "required", "A batch-export request is required.")];
        List<WellBoreBatchError> errors = [];
        if (request.Scope == WellBoreBatchExportScope.All)
        {
            if (request.WellBoreIDs is { Count: > 0 }) errors.Add(Error(null, "WellBoreIDs", "forbidden", "WellBoreIDs must be omitted for an All export."));
        }
        else if (request.Scope == WellBoreBatchExportScope.Selected)
        {
            if (request.WellBoreIDs == null || request.WellBoreIDs.Count == 0) errors.Add(Error(null, "WellBoreIDs", "required", "Selected export requires at least one UUID."));
            else
            {
                HashSet<Guid> ids = [];
                for (int index = 0; index < request.WellBoreIDs.Count; index++)
                {
                    Guid id = request.WellBoreIDs[index];
                    if (id == Guid.Empty) errors.Add(Error(index, "WellBoreIDs", "empty_uuid", "WellBore UUIDs must be non-empty."));
                    else if (!ids.Add(id)) errors.Add(Error(index, "WellBoreIDs", "duplicate_uuid", $"WellBore UUID '{id}' occurs more than once."));
                }
            }
        }
        else errors.Add(Error(null, "Scope", "invalid_scope", "Scope must be All or Selected."));
        return errors;
    }

    private static WellBoreBatchExportOutcome Failure(WellBoreBatchExportFailureKind kind, string error,
        string message, List<WellBoreBatchError> errors) => new()
        { FailureKind = kind, Error = new() { Error = error, Message = message, Errors = errors } };
    private static WellBoreBatchError Error(int? index, string property, string code, string message) =>
        new() { PositionIndex = index, Property = property, Code = code, Message = message };
}

