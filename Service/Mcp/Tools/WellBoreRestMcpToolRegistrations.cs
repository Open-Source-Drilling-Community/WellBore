using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OSDC.Drilling.WellBore.Service.Controllers;
using OSDC.Drilling.WellBore.Service.Managers;
using WellBoreModel = OSDC.Drilling.WellBore.Model.WellBore;
using WellBoreIdentityModel = OSDC.Drilling.WellBore.Model.WellBoreIdentity;
using WellBoreFeatureCategoryModel = OSDC.Drilling.WellBore.Model.WellBoreFeatureCategory;
using WellBoreBatchExportRequestModel = OSDC.Drilling.WellBore.Model.WellBoreBatchExportRequest;
using WellBoreBatchRestoreRequestModel = OSDC.Drilling.WellBore.Model.WellBoreBatchRestoreRequest;
using WellBoreDetailsUpdateModel = OSDC.Drilling.WellBore.Model.WellBoreDetailsUpdate;
using WellBoreTopologyUpdateModel = OSDC.Drilling.WellBore.Model.WellBoreTopologyUpdate;
using WellBoreIdentityAssignmentModel = OSDC.Drilling.WellBore.Model.WellBoreIdentityAssignment;
using WellBoreFeatureAssignmentModel = OSDC.Drilling.WellBore.Model.WellBoreFeatureAssignment;
using WellBoreExternalReferenceAuditRequestModel = OSDC.Drilling.WellBore.Model.WellBoreExternalReferenceAuditRequest;
using WellBoreExternalReferenceAuditResultModel = OSDC.Drilling.WellBore.Model.WellBoreExternalReferenceAuditResult;

namespace OSDC.Drilling.WellBore.Service.Mcp.Tools;

public static class WellBoreRestMcpToolRegistrations
{
    public static IServiceCollection AddWellBoreRestMcpTools(this IServiceCollection services)
    {
        services.AddLegacyMcpTool("well_bore_get_all_ids", "List the identifiers of every stored wellbore. Use this lightweight operation when only UUIDs are needed. On success, data contains an array of UUID strings; the response also contains an HTTP-style status code.", McpToolArgumentHelpers.CreateEmptySchema(),
            (sp, _, ct) => Invoke(ct, () => Controller(sp).GetAllWellBoreId()));
        services.AddLegacyMcpTool("well_bore_get_all_meta_info", "List identity and HTTP location metadata for every stored wellbore without returning complete records. On success, data contains MetaInfo objects with ID and optional HttpHostName, HttpHostBasePath, and HttpEndPoint fields.", McpToolArgumentHelpers.CreateEmptySchema(),
            (sp, _, ct) => Invoke(ct, () => Controller(sp).GetAllWellBoreMetaInfo()));
        services.AddLegacyMcpTool("well_bore_get_by_id", "Retrieve one complete wellbore record by UUID. On success, data contains its metadata, well and rig associations, sidetrack relationship, tie-in depth property, and sidetrack type. Returns status 404 when no matching wellbore exists and 400 for an empty UUID.", McpToolArgumentHelpers.CreateGuidSchema("id", "Unique identifier of the wellbore to retrieve."),
            (sp, args, ct) => InvokeByGuid(args, "id", ct, id => Controller(sp).GetWellBoreById(id)));
        services.AddLegacyMcpTool("well_bore_get_all", "Retrieve every stored wellbore as a complete record. Use the ID or metadata listing tools instead when full data is unnecessary. On success, data contains an array of WellBore objects and the response contains an HTTP-style status code.", McpToolArgumentHelpers.CreateEmptySchema(),
            (sp, _, ct) => Invoke(ct, () => Controller(sp).GetAllWellBore()));
        services.AddLegacyMcpTool("well_bore_batch_export", "Create a read-only schema-version-1 JSON backup of all stored wellbores or an explicitly ordered selection. The result contains complete WellBore records and only the identity definitions, feature categories, and feature options referenced by those records. Well, Rig, and parent-WellBore UUIDs remain external references. A missing or invalid selected WellBore rejects the complete export.", McpToolArgumentHelpers.CreateWellBoreBatchExportSchema(),
            (sp, args, ct) => InvokeWithBodyResult<WellBoreBatchExportRequestModel, OSDC.Drilling.WellBore.Model.WellBoreBatchExportDocument>(args, "request", ct, request => Controller(sp).BatchExportWellBores(request)));
        services.AddLegacyMcpTool("well_bore_batch_restore", "Validate and atomically restore a schema-version-1 WellBore backup. Source catalogue UUIDs map to compatible local definitions by exact UUID or unique normalized name; MapOrCreateMissing can create missing definitions and options. ReplaceExisting can replace matching WellBore UUIDs. Catalogue mapping, reference rewriting, catalogue creation, and every WellBore write use one transaction, so a validation, conflict, or storage failure changes nothing.", McpToolArgumentHelpers.CreateWellBoreBatchRestoreSchema(),
            (sp, args, ct) => InvokeWithBodyResult<WellBoreBatchRestoreRequestModel, OSDC.Drilling.WellBore.Model.WellBoreBatchRestoreResponse>(args, "request", ct, request => Controller(sp).BatchRestoreWellBores(request)));
        services.AddLegacyMcpTool("well_bore_get_all_by_well_id", "Retrieve complete records for all wellbores belonging to one well UUID. On success, data is an array of WellBore objects; an empty array means that no wellbores currently reference the well.", McpToolArgumentHelpers.CreateGuidSchema("wellId", "Identifier of the well whose wellbores should be returned."),
            (sp, args, ct) => InvokeByGuid(args, "wellId", ct, id => Controller(sp).GetAllWellBoreByWellID(id)));
        services.AddLegacyMcpTool("well_bore_get_all_by_rig_id", "Retrieve complete records for all wellbores associated with one rig UUID. On success, data is an array of WellBore objects; an empty array means that no wellbores currently reference the rig.", McpToolArgumentHelpers.CreateGuidSchema("rigId", "Identifier of the rig whose wellbores should be returned."),
            (sp, args, ct) => InvokeByGuid(args, "rigId", ct, id => Controller(sp).GetAllWellBoreByRigId(id)));
        services.AddLegacyMcpTool("well_bore_get_all_by_parent_id", "Retrieve complete records for all wellbores whose ParentWellBoreID equals the supplied UUID. Use this to find the direct sidetrack children of a particular parent wellbore.", McpToolArgumentHelpers.CreateGuidSchema("parentId", "Identifier of the parent wellbore whose direct children should be returned."),
            (sp, args, ct) => InvokeByGuid(args, "parentId", ct, id => Controller(sp).GetAllWellBoreByParentWellBoreId(id)));
        services.AddLegacyMcpTool("well_bore_get_all_sidetracked", "Retrieve every stored wellbore marked as a sidetrack, regardless of parent. On success, data is an array of complete WellBore objects whose IsSidetrack flag is true. Use well_bore_get_all_by_parent_id instead to restrict the result to one parent.", McpToolArgumentHelpers.CreateEmptySchema(),
            (sp, _, ct) => Invoke(ct, () => Controller(sp).GetAllSidetrackedWellBore(Guid.Empty)));
        services.AddLegacyMcpTool("well_bore_search", "Return one deterministic page of complete WellBores with a total match count. Optional filters support case-insensitive name and identity-value matching, exact Well, Rig, parent, sidetrack state, identity, feature-category and feature-option values, and inclusive modification timestamps. Limit is capped at 200.", McpToolArgumentHelpers.CreateWellBoreSearchSchema(),
            (sp, args, ct) => InvokeSearch(sp, args, ct));
        services.AddLegacyMcpTool("well_bore_validate_external_references", "Check one stored WellBore's Well and Rig UUIDs against the configured external services without changing data. Valid confirms all supplied references exist; Invalid identifies missing resources; Unavailable distinguishes configuration, transport, malformed-response, timeout, and dependency failures.", McpToolArgumentHelpers.CreateGuidSchema("wellBoreId", "UUID of the stored WellBore whose external references should be checked."),
            (sp, args, ct) => InvokeByGuidAsync(args, "wellBoreId", ct,
                (id, token) => Controller(sp).ValidateWellBoreExternalReferences(id, token)));
        services.AddLegacyMcpTool("well_bore_audit_external_references", "Check a deterministic, bounded page of all or selected stored WellBores against the configured Well and Rig services without changing data. Each result and the page counts distinguish valid references, invalid references, and checks that could not complete because a dependency was unavailable.", McpToolArgumentHelpers.CreateWellBoreExternalReferenceAuditSchema(),
            (sp, args, ct) => InvokeWithBodyResultAsync<WellBoreExternalReferenceAuditRequestModel, WellBoreExternalReferenceAuditResultModel>(
                args, "request", ct, (request, token) => Controller(sp).AuditWellBoreExternalReferences(request, token)));
        services.AddLegacyMcpTool("well_bore_create", "Create and persist a new wellbore. Supply the complete WellBore object using the documented PascalCase fields; wellBore.MetaInfo.ID must be a caller-generated, non-empty UUID and must not already exist. For sidetracks, set IsSidetrack and provide the applicable parent and tie-in depth; classification belongs in an exclusive SidetrackClassification feature assignment. The deprecated SidetrackType field is accepted and mapped for compatibility. TieInPointAlongHoleDepth is always expressed in meters (SI) against WGS84. Returns 200 on success, 400 for malformed data, and 409 for an existing ID.", McpToolArgumentHelpers.CreateWellBoreSchema(),
            (sp, args, ct) => InvokeWithBody<WellBoreModel>(args, "wellBore", ct, data => Controller(sp).PostWellBore(data)));
        services.AddLegacyMcpTool("well_bore_update_by_id", "Replace the stored data for an existing wellbore. The top-level id and wellBore.MetaInfo.ID must be the same non-empty UUID, and expectedModifiedUtc must exactly match the LastModificationDate from the latest read. Include the complete desired WellBore object because this is a full replacement. A stale revision returns 409 without changing data.", McpToolArgumentHelpers.CreateWellBoreSchema(includeId: true),
            (sp, args, ct) => InvokeWithIdTimestampAndBody<WellBoreModel>(args, "wellBore", ct, (id, expected, data) => Controller(sp).PutWellBoreById(id, expected, data)));
        services.AddLegacyMcpTool("well_bore_details_update", "Replace only Name and Description without resending topology or assignment arrays. Both properties must be supplied and may be null. expectedModifiedUtc protects against stale edits, and the updated WellBore with its new revision is returned.", McpToolArgumentHelpers.CreateWellBoreDetailsMutationSchema(),
            (sp, args, ct) => InvokeWithIdTimestampAndBody<WellBoreDetailsUpdateModel>(args, "details", ct, (id, expected, data) => Controller(sp).PutWellBoreDetails(id, expected, data)));
        services.AddLegacyMcpTool("well_bore_topology_update", "Replace only WellID, RigID, and structural sidetrack topology without resending details or assignments. Classification belongs to the SidetrackClassification feature; the deprecated SidetrackType input remains a compatibility fallback. ParentWellBoreID is validated locally, cycles and cross-Well parent links are rejected, while WellID and RigID remain external references.", McpToolArgumentHelpers.CreateWellBoreTopologyMutationSchema(),
            (sp, args, ct) => InvokeWithIdTimestampAndBody<WellBoreTopologyUpdateModel>(args, "topology", ct, (id, expected, data) => Controller(sp).PutWellBoreTopology(id, expected, data)));
        services.AddLegacyMcpTool("well_bore_identity_assignment_add", "Add one identity assignment without resending the complete WellBore. Supply a caller-generated assignment.ID and the latest WellBore LastModificationDate as expectedModifiedUtc. The updated WellBore and its new revision are returned.", McpToolArgumentHelpers.CreateIdentityAssignmentMutationSchema(false, true),
            (sp, args, ct) => InvokeAssignmentAdd<WellBoreIdentityAssignmentModel>(args, ct, (id, expected, data) => Controller(sp).PostWellBoreIdentityAssignment(id, expected, data)));
        services.AddLegacyMcpTool("well_bore_identity_assignment_update_by_id", "Replace one identity assignment selected by assignmentId without resending other WellBore data. The body ID must match assignmentId and expectedModifiedUtc must match the latest WellBore revision.", McpToolArgumentHelpers.CreateIdentityAssignmentMutationSchema(true, true),
            (sp, args, ct) => InvokeAssignmentUpdate<WellBoreIdentityAssignmentModel>(args, ct, (id, assignmentId, expected, data) => Controller(sp).PutWellBoreIdentityAssignment(id, assignmentId, expected, data)));
        services.AddLegacyMcpTool("well_bore_identity_assignment_delete_by_id", "Remove one identity assignment selected by assignmentId. expectedModifiedUtc must match the latest WellBore revision; stale requests change nothing. The updated WellBore is returned.", McpToolArgumentHelpers.CreateIdentityAssignmentMutationSchema(true, false),
            (sp, args, ct) => InvokeAssignmentDelete(args, ct, (id, assignmentId, expected) => Controller(sp).DeleteWellBoreIdentityAssignment(id, assignmentId, expected)));
        services.AddLegacyMcpTool("well_bore_feature_assignment_add", "Add one feature assignment without resending the complete WellBore. Category, option, exclusivity, and validity-period rules are validated atomically. Supply the latest WellBore revision.", McpToolArgumentHelpers.CreateFeatureAssignmentMutationSchema(false, true),
            (sp, args, ct) => InvokeAssignmentAdd<WellBoreFeatureAssignmentModel>(args, ct, (id, expected, data) => Controller(sp).PostWellBoreFeatureAssignment(id, expected, data)));
        services.AddLegacyMcpTool("well_bore_feature_assignment_update_by_id", "Replace one feature assignment selected by assignmentId without resending other WellBore data. The body ID and route ID must match, and all feature-category rules remain enforced.", McpToolArgumentHelpers.CreateFeatureAssignmentMutationSchema(true, true),
            (sp, args, ct) => InvokeAssignmentUpdate<WellBoreFeatureAssignmentModel>(args, ct, (id, assignmentId, expected, data) => Controller(sp).PutWellBoreFeatureAssignment(id, assignmentId, expected, data)));
        services.AddLegacyMcpTool("well_bore_feature_assignment_delete_by_id", "Remove one feature assignment selected by assignmentId. expectedModifiedUtc must match the latest WellBore revision; stale requests change nothing. The updated WellBore is returned.", McpToolArgumentHelpers.CreateFeatureAssignmentMutationSchema(true, false),
            (sp, args, ct) => InvokeAssignmentDelete(args, ct, (id, assignmentId, expected) => Controller(sp).DeleteWellBoreFeatureAssignment(id, assignmentId, expected)));
        services.AddLegacyMcpTool("well_bore_delete_by_id", "Permanently delete one stored wellbore only when expectedModifiedUtc matches its latest LastModificationDate. Deletion is rejected when the WellBore has sidetrack children, and stale requests return 409 without changing data.", McpToolArgumentHelpers.CreateWellBoreDeleteSchema(),
            (sp, args, ct) => InvokeDeleteWithTimestamp(args, ct, (id, expected) => Controller(sp).DeleteWellBoreById(id, expected)));
        AddCatalogCrudTools<WellBoreIdentityModel>(services, "well_bore_identity", "wellBoreIdentity", "WellBore Identity",
            McpToolArgumentHelpers.CreateWellBoreIdentitySchema,
            sp => IdentityController(sp).GetAllWellBoreIdentityId(),
            sp => IdentityController(sp).GetAllWellBoreIdentityMetaInfo(),
            (sp, id) => IdentityController(sp).GetWellBoreIdentityById(id),
            sp => IdentityController(sp).GetAllWellBoreIdentity(),
            (sp, data) => IdentityController(sp).PostWellBoreIdentity(data),
            (sp, id, expected, data) => IdentityController(sp).PutWellBoreIdentityById(id, expected, data),
            (sp, id, expected) => IdentityController(sp).DeleteWellBoreIdentityById(id, expected));
        AddCatalogCrudTools<WellBoreFeatureCategoryModel>(services, "well_bore_feature_category", "wellBoreFeatureCategory", "WellBore Feature Category",
            McpToolArgumentHelpers.CreateWellBoreFeatureCategorySchema,
            sp => FeatureCategoryController(sp).GetAllWellBoreFeatureCategoryId(),
            sp => FeatureCategoryController(sp).GetAllWellBoreFeatureCategoryMetaInfo(),
            (sp, id) => FeatureCategoryController(sp).GetWellBoreFeatureCategoryById(id),
            sp => FeatureCategoryController(sp).GetAllWellBoreFeatureCategory(),
            (sp, data) => FeatureCategoryController(sp).PostWellBoreFeatureCategory(data),
            (sp, id, expected, data) => FeatureCategoryController(sp).PutWellBoreFeatureCategoryById(id, expected, data),
            (sp, id, expected) => FeatureCategoryController(sp).DeleteWellBoreFeatureCategoryById(id, expected));
        return services;
    }

    private static void AddCatalogCrudTools<TModel>(IServiceCollection services, string prefix, string bodyName,
        string entityName, Func<bool, JsonObject> inputSchema,
        Func<IServiceProvider, ActionResult<System.Collections.Generic.IEnumerable<Guid>>> getIds,
        Func<IServiceProvider, ActionResult<System.Collections.Generic.IEnumerable<OSDC.DotnetLibraries.General.DataManagement.MetaInfo?>>> getMetaInfo,
        Func<IServiceProvider, Guid, ActionResult<TModel?>> getById,
        Func<IServiceProvider, ActionResult<System.Collections.Generic.IEnumerable<TModel?>>> getAll,
        Func<IServiceProvider, TModel?, ActionResult> create,
        Func<IServiceProvider, Guid, DateTimeOffset, TModel?, ActionResult> update,
        Func<IServiceProvider, Guid, DateTimeOffset, ActionResult> delete)
    {
        services.AddLegacyMcpTool($"{prefix}_get_all_ids", $"List every stored {entityName} UUID without transferring complete definitions. Use these identifiers with the corresponding get-by-ID operation.", McpToolArgumentHelpers.CreateEmptySchema(),
            (sp, _, ct) => Invoke(ct, () => getIds(sp)));
        services.AddLegacyMcpTool($"{prefix}_get_all_meta_info", $"List identity and optional HTTP location metadata for every stored {entityName} without returning complete definitions.", McpToolArgumentHelpers.CreateEmptySchema(),
            (sp, _, ct) => Invoke(ct, () => getMetaInfo(sp)));
        services.AddLegacyMcpTool($"{prefix}_get_by_id", $"Retrieve one complete {entityName} definition by its non-empty UUID. Returns 404 when no matching definition exists.", McpToolArgumentHelpers.CreateGuidSchema("id", $"Unique identifier of the {entityName} to retrieve."),
            (sp, args, ct) => InvokeByGuid(args, "id", ct, id => getById(sp, id)));
        services.AddLegacyMcpTool($"{prefix}_get_all", $"Retrieve every stored {entityName} as a complete definition. Prefer the lighter ID or metadata operations for discovery.", McpToolArgumentHelpers.CreateEmptySchema(),
            (sp, _, ct) => Invoke(ct, () => getAll(sp)));
        services.AddLegacyMcpTool($"{prefix}_create", $"Create a new {entityName} using a caller-generated non-empty MetaInfo.ID. Duplicate identifiers are rejected with conflict status.", inputSchema(false),
            (sp, args, ct) => InvokeWithBody<TModel>(args, bodyName, ct, data => create(sp, data)));
        services.AddLegacyMcpTool($"{prefix}_update_by_id", $"Replace an existing {entityName}. The route id must match MetaInfo.ID and expectedModifiedUtc must match the latest LastModificationDate; stale writes are rejected.", inputSchema(true),
            (sp, args, ct) => InvokeWithIdTimestampAndBody<TModel>(args, bodyName, ct, (id, expected, data) => update(sp, id, expected, data)));
        services.AddLegacyMcpTool($"{prefix}_delete_by_id", $"Delete one {entityName} only when expectedModifiedUtc matches its latest LastModificationDate. Deletion is rejected while a stored WellBore references the definition, so stale callers and dangling assignments are both prevented.", McpToolArgumentHelpers.CreateWellBoreDeleteSchema(),
            (sp, args, ct) => InvokeDeleteWithTimestamp(args, ct, (id, expected) => delete(sp, id, expected)));
    }

    private static Task<JsonNode?> Invoke<T>(CancellationToken ct, Func<ActionResult<T>> action)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult<JsonNode?>(McpActionResultConverter.FromActionResult(action()));
    }

    private static Task<JsonNode?> InvokeByGuid<T>(JsonObject? args, string key, CancellationToken ct, Func<Guid, ActionResult<T>> action)
    {
        ct.ThrowIfCancellationRequested();
        return McpToolArgumentHelpers.TryParseGuid(args, key, out Guid id, out JsonNode? error)
            ? Task.FromResult<JsonNode?>(McpActionResultConverter.FromActionResult(action(id))) : Task.FromResult(error);
    }

    private static async Task<JsonNode?> InvokeByGuidAsync<T>(JsonObject? args, string key, CancellationToken ct,
        Func<Guid, CancellationToken, Task<ActionResult<T>>> action)
    {
        ct.ThrowIfCancellationRequested();
        if (!McpToolArgumentHelpers.TryParseGuid(args, key, out Guid id, out JsonNode? error)) return error;
        return McpActionResultConverter.FromActionResult(await action(id, ct));
    }

    private static Task<JsonNode?> InvokeDelete(JsonObject? args, CancellationToken ct, Func<Guid, ActionResult> action)
    {
        ct.ThrowIfCancellationRequested();
        return McpToolArgumentHelpers.TryParseGuid(args, "id", out Guid id, out JsonNode? error)
            ? Task.FromResult<JsonNode?>(McpActionResultConverter.FromActionResult(action(id))) : Task.FromResult(error);
    }

    private static Task<JsonNode?> InvokeDeleteWithTimestamp(JsonObject? args, CancellationToken ct,
        Func<Guid, DateTimeOffset, ActionResult> action)
    {
        ct.ThrowIfCancellationRequested();
        if (!McpToolArgumentHelpers.TryParseGuid(args, "id", out Guid id, out JsonNode? idError))
            return Task.FromResult(idError);
        if (args?["expectedModifiedUtc"] is not JsonNode timestampNode ||
            !DateTimeOffset.TryParse(timestampNode.ToString(), out DateTimeOffset expected))
            return Task.FromResult<JsonNode?>(McpToolResponses.CreateValidationError(
                "Argument 'expectedModifiedUtc' must be an ISO 8601 timestamp."));
        return Task.FromResult<JsonNode?>(McpActionResultConverter.FromActionResult(action(id, expected)));
    }

    private static Task<JsonNode?> InvokeWithBody<T>(JsonObject? args, string bodyName, CancellationToken ct, Func<T?, ActionResult> action)
    {
        ct.ThrowIfCancellationRequested();
        return TryDeserialize(args, bodyName, out T? data, out JsonNode? error)
            ? Task.FromResult<JsonNode?>(McpActionResultConverter.FromActionResult(action(data))) : Task.FromResult(error);
    }

    private static Task<JsonNode?> InvokeWithBodyResult<TBody, TResult>(JsonObject? args, string bodyName,
        CancellationToken ct, Func<TBody?, ActionResult<TResult>> action)
    {
        ct.ThrowIfCancellationRequested();
        return TryDeserialize(args, bodyName, out TBody? data, out JsonNode? error)
            ? Task.FromResult<JsonNode?>(McpActionResultConverter.FromActionResult(action(data)))
            : Task.FromResult(error);
    }

    private static async Task<JsonNode?> InvokeWithBodyResultAsync<TBody, TResult>(JsonObject? args, string bodyName,
        CancellationToken ct, Func<TBody?, CancellationToken, Task<ActionResult<TResult>>> action)
    {
        ct.ThrowIfCancellationRequested();
        if (!TryDeserialize(args, bodyName, out TBody? data, out JsonNode? error)) return error;
        return McpActionResultConverter.FromActionResult(await action(data, ct));
    }

    private static Task<JsonNode?> InvokeWithIdAndBody<T>(JsonObject? args, string bodyName, CancellationToken ct, Func<Guid, T?, ActionResult> action)
    {
        ct.ThrowIfCancellationRequested();
        if (!McpToolArgumentHelpers.TryParseGuid(args, "id", out Guid id, out JsonNode? idError)) return Task.FromResult(idError);
        return TryDeserialize(args, bodyName, out T? data, out JsonNode? error)
            ? Task.FromResult<JsonNode?>(McpActionResultConverter.FromActionResult(action(id, data))) : Task.FromResult(error);
    }

    private static Task<JsonNode?> InvokeWithIdTimestampAndBody<T>(JsonObject? args, string bodyName,
        CancellationToken ct, Func<Guid, DateTimeOffset, T?, ActionResult> action)
    {
        ct.ThrowIfCancellationRequested();
        if (!McpToolArgumentHelpers.TryParseGuid(args, "id", out Guid id, out JsonNode? idError)) return Task.FromResult(idError);
        if (args?["expectedModifiedUtc"] is not JsonNode timestampNode ||
            !DateTimeOffset.TryParse(timestampNode.ToString(), out DateTimeOffset expected))
            return Task.FromResult<JsonNode?>(McpToolResponses.CreateValidationError("Argument 'expectedModifiedUtc' must be an ISO 8601 timestamp."));
        return TryDeserialize(args, bodyName, out T? data, out JsonNode? error)
            ? Task.FromResult<JsonNode?>(McpActionResultConverter.FromActionResult(action(id, expected, data)))
            : Task.FromResult(error);
    }

    private static Task<JsonNode?> InvokeAssignmentAdd<T>(JsonObject? args, CancellationToken ct,
        Func<Guid, DateTimeOffset, T?, ActionResult> action)
    {
        ct.ThrowIfCancellationRequested();
        if (!TryAssignmentHeader(args, false, out Guid id, out _, out DateTimeOffset expected, out JsonNode? error))
            return Task.FromResult(error);
        return TryDeserialize(args, "assignment", out T? data, out error)
            ? Task.FromResult<JsonNode?>(McpActionResultConverter.FromActionResult(action(id, expected, data)))
            : Task.FromResult(error);
    }

    private static Task<JsonNode?> InvokeAssignmentUpdate<T>(JsonObject? args, CancellationToken ct,
        Func<Guid, Guid, DateTimeOffset, T?, ActionResult> action)
    {
        ct.ThrowIfCancellationRequested();
        if (!TryAssignmentHeader(args, true, out Guid id, out Guid assignmentId, out DateTimeOffset expected, out JsonNode? error))
            return Task.FromResult(error);
        return TryDeserialize(args, "assignment", out T? data, out error)
            ? Task.FromResult<JsonNode?>(McpActionResultConverter.FromActionResult(action(id, assignmentId, expected, data)))
            : Task.FromResult(error);
    }

    private static Task<JsonNode?> InvokeAssignmentDelete(JsonObject? args, CancellationToken ct,
        Func<Guid, Guid, DateTimeOffset, ActionResult> action)
    {
        ct.ThrowIfCancellationRequested();
        return TryAssignmentHeader(args, true, out Guid id, out Guid assignmentId, out DateTimeOffset expected, out JsonNode? error)
            ? Task.FromResult<JsonNode?>(McpActionResultConverter.FromActionResult(action(id, assignmentId, expected)))
            : Task.FromResult(error);
    }

    private static bool TryAssignmentHeader(JsonObject? args, bool requireAssignmentId, out Guid id,
        out Guid assignmentId, out DateTimeOffset expected, out JsonNode? error)
    {
        assignmentId = Guid.Empty;
        expected = default;
        if (!McpToolArgumentHelpers.TryParseGuid(args, "wellBoreId", out id, out error)) return false;
        if (requireAssignmentId && !McpToolArgumentHelpers.TryParseGuid(args, "assignmentId", out assignmentId, out error)) return false;
        if (args?["expectedModifiedUtc"] is not JsonNode node || !DateTimeOffset.TryParse(node.ToString(), out expected))
        {
            error = McpToolResponses.CreateValidationError("Argument 'expectedModifiedUtc' must be an ISO 8601 timestamp.");
            return false;
        }
        error = null;
        return true;
    }

    private static Task<JsonNode?> InvokeSearch(IServiceProvider sp, JsonObject? args, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            string[] guidKeys = ["wellId", "rigId", "parentWellBoreId", "identityId", "featureCategoryId", "featureOptionId"];
            foreach (string key in guidKeys)
            {
                if (args?[key] is JsonNode node && !Guid.TryParse(node.ToString(), out _))
                    return Task.FromResult<JsonNode?>(McpToolResponses.CreateValidationError(
                        $"Argument '{key}' must be a UUID."));
            }
            string[] dateKeys = ["modifiedFromUtc", "modifiedToUtc"];
            foreach (string key in dateKeys)
            {
                if (args?[key] is JsonNode node && !DateTimeOffset.TryParse(node.ToString(), out _))
                    return Task.FromResult<JsonNode?>(McpToolResponses.CreateValidationError(
                        $"Argument '{key}' must be an ISO 8601 timestamp."));
            }
            int offset = args?["offset"]?.GetValue<int>() ?? 0;
            int limit = args?["limit"]?.GetValue<int>() ?? 50;
            string? name = args?["name"]?.GetValue<string>();
            string? identityValue = args?["identityValue"]?.GetValue<string>();
            bool? isSidetrack = args?["isSidetrack"]?.GetValue<bool>();
            Guid? OptionalGuid(string key) => args?[key] is JsonNode node && Guid.TryParse(node.ToString(), out Guid value) ? value : null;
            DateTimeOffset? OptionalDate(string key) => args?[key] is JsonNode node && DateTimeOffset.TryParse(node.ToString(), out DateTimeOffset value) ? value : null;
            return Task.FromResult<JsonNode?>(McpActionResultConverter.FromActionResult(Controller(sp).SearchWellBores(
                offset, limit, name, OptionalGuid("wellId"), OptionalGuid("rigId"), OptionalGuid("parentWellBoreId"),
                isSidetrack, OptionalGuid("identityId"), identityValue, OptionalGuid("featureCategoryId"),
                OptionalGuid("featureOptionId"), OptionalDate("modifiedFromUtc"), OptionalDate("modifiedToUtc"))));
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException)
        {
            return Task.FromResult<JsonNode?>(McpToolResponses.CreateValidationError("One or more search arguments have an invalid type or format."));
        }
    }

    private static bool TryDeserialize<T>(JsonObject? args, string bodyName, out T? data, out JsonNode? error)
    {
        data = default;
        error = null;
        if (args?[bodyName] is not JsonNode node)
        {
            error = McpToolResponses.CreateValidationError($"Argument '{bodyName}' is required.");
            return false;
        }
        try
        {
            data = node.Deserialize<T>(JsonSettings.Options);
            if (data is null) throw new InvalidOperationException();
            return true;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            error = McpToolResponses.CreateValidationError($"Argument '{bodyName}' could not be deserialized.");
            return false;
        }
    }

    private static WellBoreController Controller(IServiceProvider sp) => new(
        sp.GetRequiredService<ILogger<WellBoreManager>>(), sp.GetRequiredService<SqlConnectionManager>(),
        sp.GetRequiredService<IWellBoreExternalReferenceValidator>());

    private static WellBoreIdentityController IdentityController(IServiceProvider sp) => new(
        sp.GetRequiredService<ILogger<WellBoreIdentityManager>>(), sp.GetRequiredService<SqlConnectionManager>());

    private static WellBoreFeatureCategoryController FeatureCategoryController(IServiceProvider sp) => new(
        sp.GetRequiredService<ILogger<WellBoreFeatureCategoryManager>>(), sp.GetRequiredService<SqlConnectionManager>());
}
