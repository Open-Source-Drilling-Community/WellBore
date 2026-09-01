using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NORCE.Drilling.WellBore.Service.Controllers;
using NORCE.Drilling.WellBore.Service.Managers;
using WellBoreModel = NORCE.Drilling.WellBore.Model.WellBore;

namespace NORCE.Drilling.WellBore.Service.Mcp.Tools;

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
        services.AddLegacyMcpTool("well_bore_get_all_by_well_id", "Retrieve complete records for all wellbores belonging to one well UUID. On success, data is an array of WellBore objects; an empty array means that no wellbores currently reference the well.", McpToolArgumentHelpers.CreateGuidSchema("wellId", "Identifier of the well whose wellbores should be returned."),
            (sp, args, ct) => InvokeByGuid(args, "wellId", ct, id => Controller(sp).GetAllWellBoreByWellID(id)));
        services.AddLegacyMcpTool("well_bore_get_all_by_rig_id", "Retrieve complete records for all wellbores associated with one rig UUID. On success, data is an array of WellBore objects; an empty array means that no wellbores currently reference the rig.", McpToolArgumentHelpers.CreateGuidSchema("rigId", "Identifier of the rig whose wellbores should be returned."),
            (sp, args, ct) => InvokeByGuid(args, "rigId", ct, id => Controller(sp).GetAllWellBoreByRigId(id)));
        services.AddLegacyMcpTool("well_bore_get_all_by_parent_id", "Retrieve complete records for all wellbores whose ParentWellBoreID equals the supplied UUID. Use this to find the direct sidetrack children of a particular parent wellbore.", McpToolArgumentHelpers.CreateGuidSchema("parentId", "Identifier of the parent wellbore whose direct children should be returned."),
            (sp, args, ct) => InvokeByGuid(args, "parentId", ct, id => Controller(sp).GetAllWellBoreByParentWellBoreId(id)));
        services.AddLegacyMcpTool("well_bore_get_all_sidetracked", "Retrieve every stored wellbore marked as a sidetrack, regardless of parent. On success, data is an array of complete WellBore objects whose IsSidetrack flag is true. Use well_bore_get_all_by_parent_id instead to restrict the result to one parent.", McpToolArgumentHelpers.CreateEmptySchema(),
            (sp, _, ct) => Invoke(ct, () => Controller(sp).GetAllSidetrackedWellBore(Guid.Empty)));
        services.AddLegacyMcpTool("well_bore_create", "Create and persist a new wellbore. Supply the complete WellBore object using the documented PascalCase fields; wellBore.MetaInfo.ID must be a caller-generated, non-empty UUID and must not already exist. For sidetracks, set IsSidetrack and provide the applicable parent, tie-in depth, and sidetrack type fields. TieInPointAlongHoleDepth is always expressed in meters (SI) and referenced to the fixed WGS84 vertical datum. Returns status 200 on success, 400 for malformed data, and 409 when the ID already exists.", McpToolArgumentHelpers.CreateWellBoreSchema(),
            (sp, args, ct) => InvokeWithBody<WellBoreModel>(args, "wellBore", ct, data => Controller(sp).PostWellBore(data)));
        services.AddLegacyMcpTool("well_bore_update_by_id", "Replace the stored data for an existing wellbore. The top-level id and wellBore.MetaInfo.ID must be the same non-empty UUID; include the complete desired WellBore object because the operation is a full update rather than a partial patch. TieInPointAlongHoleDepth is always expressed in meters (SI) and referenced to the fixed WGS84 vertical datum. Returns status 200 on success, 400 for malformed or mismatched IDs, and 404 when the wellbore does not exist.", McpToolArgumentHelpers.CreateWellBoreSchema(includeId: true),
            (sp, args, ct) => InvokeWithIdAndBody<WellBoreModel>(args, "wellBore", ct, (id, data) => Controller(sp).PutWellBoreById(id, data)));
        services.AddLegacyMcpTool("well_bore_delete_by_id", "Permanently delete one stored wellbore by UUID. Confirm the target identifier and consider its sidetrack children before calling because this operation removes the record. Returns status 200 on success, 404 when the wellbore does not exist, and 500 if deletion fails.", McpToolArgumentHelpers.CreateGuidSchema("id", "Unique identifier of the wellbore to delete."),
            (sp, args, ct) => InvokeDelete(args, ct, id => Controller(sp).DeleteWellBoreById(id)));
        return services;
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

    private static Task<JsonNode?> InvokeDelete(JsonObject? args, CancellationToken ct, Func<Guid, ActionResult> action)
    {
        ct.ThrowIfCancellationRequested();
        return McpToolArgumentHelpers.TryParseGuid(args, "id", out Guid id, out JsonNode? error)
            ? Task.FromResult<JsonNode?>(McpActionResultConverter.FromActionResult(action(id))) : Task.FromResult(error);
    }

    private static Task<JsonNode?> InvokeWithBody<T>(JsonObject? args, string bodyName, CancellationToken ct, Func<T?, ActionResult> action)
    {
        ct.ThrowIfCancellationRequested();
        return TryDeserialize(args, bodyName, out T? data, out JsonNode? error)
            ? Task.FromResult<JsonNode?>(McpActionResultConverter.FromActionResult(action(data))) : Task.FromResult(error);
    }

    private static Task<JsonNode?> InvokeWithIdAndBody<T>(JsonObject? args, string bodyName, CancellationToken ct, Func<Guid, T?, ActionResult> action)
    {
        ct.ThrowIfCancellationRequested();
        if (!McpToolArgumentHelpers.TryParseGuid(args, "id", out Guid id, out JsonNode? idError)) return Task.FromResult(idError);
        return TryDeserialize(args, bodyName, out T? data, out JsonNode? error)
            ? Task.FromResult<JsonNode?>(McpActionResultConverter.FromActionResult(action(id, data))) : Task.FromResult(error);
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
        sp.GetRequiredService<ILogger<WellBoreManager>>(), sp.GetRequiredService<SqlConnectionManager>());
}
