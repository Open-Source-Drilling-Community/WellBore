using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.Drilling.WellBore.Service.Managers;
using OSDC.Drilling.WellBore.Model;

namespace OSDC.Drilling.WellBore.Service.Controllers
{
    [Produces("application/json")]
    [Route("[controller]")]
    [ApiController]
    public class WellBoreController : ControllerBase
    {
        private readonly ILogger<WellBoreManager> _logger;
        private readonly WellBoreManager _wellBoreManager;
        private readonly IWellBoreExternalReferenceValidator _externalReferenceValidator;

        public WellBoreController(ILogger<WellBoreManager> logger, SqlConnectionManager connectionManager,
            IWellBoreExternalReferenceValidator? externalReferenceValidator = null)
        {
            _logger = logger;
            _wellBoreManager = WellBoreManager.GetInstance(_logger, connectionManager);
            _externalReferenceValidator = externalReferenceValidator ?? new UnavailableWellBoreExternalReferenceValidator();
        }

        /// <summary>
        /// Returns the list of Guid of all WellBore present in the microservice database at endpoint WellBore/api/WellBore
        /// </summary>
        /// <returns>the list of Guid of all WellBore present in the microservice database at endpoint WellBore/api/WellBore</returns>
        [HttpGet(Name = "GetAllWellBoreId")]
        public ActionResult<IEnumerable<Guid>> GetAllWellBoreId()
        {
            UsageStatisticsWellBore.Instance.IncrementGetAllWellBoreIdPerDay();
            var ids = _wellBoreManager.GetAllWellBoreId();
            if (ids != null)
            {
                return Ok(ids);
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Returns the list of MetaInfo of all WellBore present in the microservice database, at endpoint WellBore/api/WellBore/MetaInfo
        /// </summary>
        /// <returns>the list of MetaInfo of all WellBore present in the microservice database, at endpoint WellBore/api/WellBore/MetaInfo</returns>
        [HttpGet("MetaInfo", Name = "GetAllWellBoreMetaInfo")]
        public ActionResult<IEnumerable<MetaInfo>> GetAllWellBoreMetaInfo()
        {
            UsageStatisticsWellBore.Instance.IncrementGetAllWellBoreMetaInfoPerDay();
            var vals = _wellBoreManager.GetAllWellBoreMetaInfo();
            if (vals != null)
            {
                return Ok(vals);
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Returns the WellBore identified by its Guid from the microservice database, at endpoint WellBore/api/WellBore/MetaInfo/id
        /// </summary>
        /// <param name="guid"></param>
        /// <returns>the WellBore identified by its Guid from the microservice database, at endpoint WellBore/api/WellBore/MetaInfo/id</returns>
        [HttpGet("{id}", Name = "GetWellBoreById")]
        public ActionResult<Model.WellBore?> GetWellBoreById(Guid id)
        {
            UsageStatisticsWellBore.Instance.IncrementGetWellBoreByIdPerDay();
            if (!id.Equals(Guid.Empty))
            {
                var val = _wellBoreManager.GetWellBoreById(id);
                if (val != null)
                {
                    return Ok(val);
                }
                else
                {
                    return NotFound();
                }
            }
            else
            {
                return BadRequest();
            }
        }


        /// <summary>
        /// Returns the list of all WellBore present in the microservice database, at endpoint WellBore/api/WellBore/HeavyData
        /// </summary>
        /// <returns>the list of all WellBore present in the microservice database, at endpoint WellBore/api/WellBore/HeavyData</returns>
        [HttpGet("HeavyData", Name = "GetAllWellBore")]
        public ActionResult<IEnumerable<Model.WellBore?>> GetAllWellBore()
        {
            UsageStatisticsWellBore.Instance.IncrementGetAllWellBorePerDay();
            var vals = _wellBoreManager.GetAllWellBore();
            if (vals != null)
            {
                return Ok(vals);
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>Returns one deterministic, bounded page of WellBores matching optional filters.</summary>
        [HttpGet("Search", Name = "SearchWellBores")]
        [ProducesResponseType<WellBoreSearchResult>(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(WellBoreMutationErrorEnvelope), StatusCodes.Status400BadRequest)]
        public ActionResult<WellBoreSearchResult> SearchWellBores(
            [FromQuery] int offset = 0, [FromQuery] int limit = 50,
            [FromQuery] string? name = null, [FromQuery] Guid? wellId = null,
            [FromQuery] Guid? rigId = null, [FromQuery] Guid? parentWellBoreId = null,
            [FromQuery] bool? isSidetrack = null, [FromQuery] Guid? identityId = null,
            [FromQuery] string? identityValue = null, [FromQuery] Guid? featureCategoryId = null,
            [FromQuery] Guid? featureOptionId = null, [FromQuery] DateTimeOffset? modifiedFromUtc = null,
            [FromQuery] DateTimeOffset? modifiedToUtc = null)
        {
            if (offset < 0 || limit is < 1 or > 200)
                return BadRequest(WellBoreMutationResult.Invalid("pagination", "invalid_range", "Offset must be non-negative and limit must be between 1 and 200.").Error);
            if (name?.Length > 200 || identityValue?.Length > 500)
                return BadRequest(WellBoreMutationResult.Invalid("filters", "value_too_long", "Name is limited to 200 characters and identityValue to 500 characters.").Error);
            if (new[] { wellId, rigId, parentWellBoreId, identityId, featureCategoryId, featureOptionId }.Any(value => value == Guid.Empty))
                return BadRequest(WellBoreMutationResult.Invalid("filters", "empty_uuid", "Optional UUID filters must be omitted or non-empty.").Error);
            if (modifiedFromUtc > modifiedToUtc)
                return BadRequest(WellBoreMutationResult.Invalid("modifiedFromUtc", "invalid_date_range", "modifiedFromUtc must be earlier than or equal to modifiedToUtc.").Error);
            WellBoreSearchResult? result = _wellBoreManager.SearchWellBores(offset, limit, name, wellId, rigId,
                parentWellBoreId, isSidetrack, identityId, identityValue, featureCategoryId, featureOptionId,
                modifiedFromUtc, modifiedToUtc);
            return result != null ? Ok(result) : StatusCode(StatusCodes.Status500InternalServerError,
                WellBoreMutationResult.StorageFailure().Error);
        }

        /// <summary>Checks one stored WellBore's external Well and Rig references without modifying data.</summary>
        [HttpGet("{id}/ExternalReferences", Name = "ValidateWellBoreExternalReferences")]
        [ProducesResponseType<WellBoreExternalReferenceValidation>(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(WellBoreMutationErrorEnvelope), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(WellBoreMutationErrorEnvelope), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<WellBoreExternalReferenceValidation>> ValidateWellBoreExternalReferences(
            Guid id, CancellationToken cancellationToken)
        {
            if (id == Guid.Empty)
                return BadRequest(WellBoreMutationResult.Invalid("id", "invalid_id", "A non-empty WellBore UUID is required.").Error);
            Model.WellBore? wellBore = _wellBoreManager.GetWellBoreById(id);
            if (wellBore == null)
                return NotFound(WellBoreMutationResult.NotFound("The WellBore does not exist.").Error);
            IReadOnlyList<WellBoreExternalReferenceValidation> results =
                await _externalReferenceValidator.ValidateAsync([wellBore], cancellationToken);
            return Ok(results.Single());
        }

        /// <summary>Checks a bounded page of stored WellBores for external Well and Rig consistency.</summary>
        [HttpPost("ExternalReferenceAudit", Name = "AuditWellBoreExternalReferences")]
        [ProducesResponseType<WellBoreExternalReferenceAuditResult>(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(WellBoreMutationErrorEnvelope), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(WellBoreMutationErrorEnvelope), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<WellBoreExternalReferenceAuditResult>> AuditWellBoreExternalReferences(
            [FromBody] WellBoreExternalReferenceAuditRequest? request, CancellationToken cancellationToken)
        {
            if (request == null)
                return BadRequest(WellBoreMutationResult.Invalid("request", "required", "An audit request is required.").Error);
            if (!Enum.IsDefined(request.Scope))
                return BadRequest(WellBoreMutationResult.Invalid("Scope", "invalid_value", "Scope must be All or Selected.").Error);
            if (request.Offset < 0 || request.Limit is < 1 or > 100)
                return BadRequest(WellBoreMutationResult.Invalid("pagination", "invalid_range",
                    "Offset must be non-negative and limit must be between 1 and 100.").Error);
            if (request.Scope == WellBoreExternalReferenceAuditScope.Selected &&
                (request.WellBoreIDs == null || request.WellBoreIDs.Count == 0))
                return BadRequest(WellBoreMutationResult.Invalid("WellBoreIDs", "required",
                    "Selected scope requires at least one WellBore UUID.").Error);
            if (request.WellBoreIDs?.Any(value => value == Guid.Empty) == true ||
                request.WellBoreIDs?.Distinct().Count() != request.WellBoreIDs?.Count)
                return BadRequest(WellBoreMutationResult.Invalid("WellBoreIDs", "invalid_ids",
                    "WellBore UUIDs must be non-empty and unique.").Error);

            List<Model.WellBore?>? stored = _wellBoreManager.GetAllWellBore();
            if (stored == null)
                return StatusCode(StatusCodes.Status500InternalServerError, WellBoreMutationResult.StorageFailure().Error);
            Dictionary<Guid, Model.WellBore> byId = stored.Where(value => value?.MetaInfo != null)
                .Cast<Model.WellBore>().ToDictionary(value => value.MetaInfo!.ID);
            IEnumerable<Model.WellBore> selected = byId.Values;
            if (request.Scope == WellBoreExternalReferenceAuditScope.Selected)
            {
                List<Guid> missing = request.WellBoreIDs!.Where(id => !byId.ContainsKey(id)).ToList();
                if (missing.Count != 0)
                    return NotFound(WellBoreMutationResult.NotFound(
                        $"Selected WellBore UUID '{missing[0]}' does not exist.").Error);
                selected = request.WellBoreIDs!.Select(id => byId[id]);
            }
            List<Model.WellBore> matches = selected.OrderBy(value => value.MetaInfo!.ID).ToList();
            List<Model.WellBore> page = matches.Skip(request.Offset).Take(request.Limit).ToList();
            IReadOnlyList<WellBoreExternalReferenceValidation> items =
                await _externalReferenceValidator.ValidateAsync(page, cancellationToken);
            return Ok(new WellBoreExternalReferenceAuditResult
            {
                CheckedAtUtc = items.FirstOrDefault()?.CheckedAtUtc ?? DateTimeOffset.UtcNow,
                Total = matches.Count,
                Offset = request.Offset,
                Limit = request.Limit,
                ValidCount = items.Count(value => value.Status == WellBoreExternalReferenceValidationStatus.Valid),
                InvalidCount = items.Count(value => value.Status == WellBoreExternalReferenceValidationStatus.Invalid),
                UnavailableCount = items.Count(value => value.Status == WellBoreExternalReferenceValidationStatus.Unavailable),
                Items = items.ToList()
            });
        }

        /// <summary>Exports all WellBores or an ordered selection with referenced local catalog definitions.</summary>
        [HttpPost("BatchExport", Name = "BatchExportWellBores")]
        [ProducesResponseType<WellBoreBatchExportDocument>(StatusCodes.Status200OK)]
        [ProducesResponseType<WellBoreBatchErrorEnvelope>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<WellBoreBatchErrorEnvelope>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<WellBoreBatchErrorEnvelope>(StatusCodes.Status500InternalServerError)]
        public ActionResult<WellBoreBatchExportDocument> BatchExportWellBores([FromBody] WellBoreBatchExportRequest? request)
        {
            WellBoreBatchExportOutcome outcome = _wellBoreManager.ExportBatch(request);
            if (outcome.IsSuccess) return Ok(outcome.Document);
            return outcome.FailureKind switch
            {
                WellBoreBatchExportFailureKind.InvalidRequest => BadRequest(outcome.Error),
                WellBoreBatchExportFailureKind.WellNotFound => NotFound(outcome.Error),
                _ => StatusCode(StatusCodes.Status500InternalServerError, outcome.Error)
            };
        }

        /// <summary>Validates and atomically restores WellBores and their local catalog dependencies.</summary>
        [HttpPost("BatchRestore", Name = "BatchRestoreWellBores")]
        [ProducesResponseType<WellBoreBatchRestoreResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<WellBoreBatchErrorEnvelope>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<WellBoreBatchErrorEnvelope>(StatusCodes.Status409Conflict)]
        [ProducesResponseType<WellBoreBatchErrorEnvelope>(StatusCodes.Status500InternalServerError)]
        public ActionResult<WellBoreBatchRestoreResponse> BatchRestoreWellBores([FromBody] WellBoreBatchRestoreRequest? request)
        {
            WellBoreBatchRestoreOutcome outcome = _wellBoreManager.RestoreBatch(request);
            if (outcome.IsSuccess) return Ok(outcome.Response);
            return outcome.FailureKind switch
            {
                WellBoreBatchRestoreFailureKind.InvalidRequest => BadRequest(outcome.Error),
                WellBoreBatchRestoreFailureKind.Conflict => Conflict(outcome.Error),
                _ => StatusCode(StatusCodes.Status500InternalServerError, outcome.Error)
            };
        }

        /// <summary>
        /// Returns the list of all WellBore with given Well ID present in the microservice database, at endpoint WellBore/api/WellBore/HeavyData
        /// </summary>
        /// <returns>the list of all WellBore with given Well ID present in the microservice database, at endpoint WellBore/api/WellBore/HeavyData</returns>
        [HttpGet("ByWellID", Name = "GetAllWellBoreByWellID")]
        public ActionResult<IEnumerable<Model.WellBore?>> GetAllWellBoreByWellID(Guid wellID)
        {
            UsageStatisticsWellBore.Instance.IncrementGetAllWellBoreByWellIDPerDay();
            var vals = _wellBoreManager.GetAllWellBoreByWellID(wellID);
            if (vals != null)
            {
                return Ok(vals);
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
        /// <summary>
        /// Returns the list of all WellBore with given Rig ID present in the microservice database, at endpoint WellBore/api/WellBore/HeavyData
        /// </summary>
        /// <returns>the list of all WellBore with given Rig ID present in the microservice database, at endpoint WellBore/api/WellBore/HeavyData</returns>
        [HttpGet("ByRigID", Name = "GetAllWellBoreByRigId")]
        public ActionResult<IEnumerable<Model.WellBore?>> GetAllWellBoreByRigId(Guid rigID)
        {
            UsageStatisticsWellBore.Instance.IncrementGetAllWellBoreByRigIDPerDay();
            var vals = _wellBoreManager.GetAllWellBoreByRigID(rigID);
            if (vals != null)
            {
                return Ok(vals);
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Returns the list of all WellBore with given Parent Wellbore ID present in the microservice database, at endpoint WellBore/api/WellBore/HeavyData
        /// </summary>
        /// <returns>the list of all WellBore with given Parent Wellbore ID present in the microservice database, at endpoint WellBore/api/WellBore/HeavyData</returns>
        [HttpGet("ByParentID", Name = "GetAllWellBoreByParentWellBoreId")]
        public ActionResult<IEnumerable<Model.WellBore?>> GetAllWellBoreByParentWellBoreId(Guid parentID)
        {
            UsageStatisticsWellBore.Instance.IncrementGetAllWellBoreParentWellBoreIDPerDay();
            var vals = _wellBoreManager.GetAllWellBoreByParentWellBoreID(parentID);
            if (vals != null)
            {
                return Ok(vals);
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Returns the list of all WellBore that are sidetracked present in the microservice database, at endpoint WellBore/api/WellBore/HeavyData
        /// </summary>
        /// <returns>the list of all WellBore that are sidetracked present in the microservice database, at endpoint WellBore/api/WellBore/HeavyData</returns>
        [HttpGet("IsSidetracked", Name = "GetAllSidetrackedWellBore")]
        public ActionResult<IEnumerable<Model.WellBore?>> GetAllSidetrackedWellBore(Guid parentID)
        {
            UsageStatisticsWellBore.Instance.IncrementGetAllSidetrackedWellBorePerDay();
            var vals = _wellBoreManager.GetAllSideTrackedWellBore();
            if (vals != null)
            {
                return Ok(vals);
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Performs calculation on the given WellBore and adds it to the microservice database, at the endpoint WellBore/api/WellBore
        /// </summary>
        /// <param name="wellBore"></param>
        /// <returns>true if the given WellBore has been added successfully to the microservice database, at the endpoint WellBore/api/WellBore</returns>
        [HttpPost(Name = "PostWellBore")]
        public ActionResult PostWellBore([FromBody] Model.WellBore? data)
        {
            UsageStatisticsWellBore.Instance.IncrementPostWellBorePerDay();
            return this.ToActionResult(_wellBoreManager.CreateWellBore(data));
        }

        /// <summary>
        /// Performs calculation on the given WellBore and updates it in the microservice database, at the endpoint WellBore/api/WellBore/id
        /// </summary>
        /// <param name="wellBore"></param>
        /// <returns>true if the given WellBore has been updated successfully to the microservice database, at the endpoint WellBore/api/WellBore/id</returns>
        [HttpPut("{id}", Name = "PutWellBoreById")]
        public ActionResult PutWellBoreById(Guid id, [FromQuery, BindRequired] DateTimeOffset expectedModifiedUtc,
            [FromBody] Model.WellBore? data)
        {
            UsageStatisticsWellBore.Instance.IncrementPutWellBoreByIdPerDay();
            return this.ToActionResult(_wellBoreManager.UpdateWellBore(id, expectedModifiedUtc, data));
        }

        [HttpPut("{id}/Details", Name = "PutWellBoreDetails")]
        public ActionResult PutWellBoreDetails(Guid id, [FromQuery, BindRequired] DateTimeOffset expectedModifiedUtc,
            [FromBody] WellBoreDetailsUpdate? details)
        {
            WellBoreMutationResult outcome = _wellBoreManager.UpdateWellBoreDetails(id, expectedModifiedUtc, details);
            return this.ToActionResult(outcome, outcome.Resource);
        }

        [HttpPut("{id}/Topology", Name = "PutWellBoreTopology")]
        public ActionResult PutWellBoreTopology(Guid id, [FromQuery, BindRequired] DateTimeOffset expectedModifiedUtc,
            [FromBody] WellBoreTopologyUpdate? topology)
        {
            WellBoreMutationResult outcome = _wellBoreManager.UpdateWellBoreTopology(id, expectedModifiedUtc, topology);
            return this.ToActionResult(outcome, outcome.Resource);
        }

        [HttpPost("{wellBoreId}/IdentityAssignments", Name = "PostWellBoreIdentityAssignment")]
        public ActionResult PostWellBoreIdentityAssignment(Guid wellBoreId,
            [FromQuery, BindRequired] DateTimeOffset expectedModifiedUtc, [FromBody] WellBoreIdentityAssignment? assignment)
        {
            WellBoreMutationResult outcome = _wellBoreManager.AddIdentityAssignment(wellBoreId, expectedModifiedUtc, assignment);
            return this.ToActionResult(outcome, outcome.Resource);
        }

        [HttpPut("{wellBoreId}/IdentityAssignments/{assignmentId}", Name = "PutWellBoreIdentityAssignment")]
        public ActionResult PutWellBoreIdentityAssignment(Guid wellBoreId, Guid assignmentId,
            [FromQuery, BindRequired] DateTimeOffset expectedModifiedUtc, [FromBody] WellBoreIdentityAssignment? assignment)
        {
            WellBoreMutationResult outcome = _wellBoreManager.UpdateIdentityAssignment(wellBoreId, assignmentId, expectedModifiedUtc, assignment);
            return this.ToActionResult(outcome, outcome.Resource);
        }

        [HttpDelete("{wellBoreId}/IdentityAssignments/{assignmentId}", Name = "DeleteWellBoreIdentityAssignment")]
        public ActionResult DeleteWellBoreIdentityAssignment(Guid wellBoreId, Guid assignmentId,
            [FromQuery, BindRequired] DateTimeOffset expectedModifiedUtc)
        {
            WellBoreMutationResult outcome = _wellBoreManager.DeleteIdentityAssignment(wellBoreId, assignmentId, expectedModifiedUtc);
            return this.ToActionResult(outcome, outcome.Resource);
        }

        [HttpPost("{wellBoreId}/FeatureAssignments", Name = "PostWellBoreFeatureAssignment")]
        public ActionResult PostWellBoreFeatureAssignment(Guid wellBoreId,
            [FromQuery, BindRequired] DateTimeOffset expectedModifiedUtc, [FromBody] WellBoreFeatureAssignment? assignment)
        {
            WellBoreMutationResult outcome = _wellBoreManager.AddFeatureAssignment(wellBoreId, expectedModifiedUtc, assignment);
            return this.ToActionResult(outcome, outcome.Resource);
        }

        [HttpPut("{wellBoreId}/FeatureAssignments/{assignmentId}", Name = "PutWellBoreFeatureAssignment")]
        public ActionResult PutWellBoreFeatureAssignment(Guid wellBoreId, Guid assignmentId,
            [FromQuery, BindRequired] DateTimeOffset expectedModifiedUtc, [FromBody] WellBoreFeatureAssignment? assignment)
        {
            WellBoreMutationResult outcome = _wellBoreManager.UpdateFeatureAssignment(wellBoreId, assignmentId, expectedModifiedUtc, assignment);
            return this.ToActionResult(outcome, outcome.Resource);
        }

        [HttpDelete("{wellBoreId}/FeatureAssignments/{assignmentId}", Name = "DeleteWellBoreFeatureAssignment")]
        public ActionResult DeleteWellBoreFeatureAssignment(Guid wellBoreId, Guid assignmentId,
            [FromQuery, BindRequired] DateTimeOffset expectedModifiedUtc)
        {
            WellBoreMutationResult outcome = _wellBoreManager.DeleteFeatureAssignment(wellBoreId, assignmentId, expectedModifiedUtc);
            return this.ToActionResult(outcome, outcome.Resource);
        }

        /// <summary>
        /// Deletes the WellBore of given ID from the microservice database, at the endpoint WellBore/api/WellBore/id
        /// </summary>
        /// <param name="guid"></param>
        /// <returns>true if the WellBore was deleted from the microservice database, at the endpoint WellBore/api/WellBore/id</returns>
        [HttpDelete("{id}", Name = "DeleteWellBoreById")]
        public ActionResult DeleteWellBoreById(Guid id, [FromQuery, BindRequired] DateTimeOffset expectedModifiedUtc)
        {
            UsageStatisticsWellBore.Instance.IncrementDeleteWellBoreByIdPerDay();
            return this.ToActionResult(_wellBoreManager.DeleteWellBore(id, expectedModifiedUtc));
        }
    }
}
