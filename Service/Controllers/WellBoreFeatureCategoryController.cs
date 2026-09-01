using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using OSDC.Drilling.WellBore.Model;
using OSDC.Drilling.WellBore.Service.Managers;
using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;

namespace OSDC.Drilling.WellBore.Service.Controllers
{
    [Produces("application/json")]
    [Route("[controller]")]
    [ApiController]
    public class WellBoreFeatureCategoryController : ControllerBase
    {
        private readonly ILogger<WellBoreFeatureCategoryManager> _logger;
        private readonly WellBoreFeatureCategoryManager _manager;
        private readonly SqlConnectionManager _connectionManager;

        public WellBoreFeatureCategoryController(ILogger<WellBoreFeatureCategoryManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _connectionManager = connectionManager;
            _manager = WellBoreFeatureCategoryManager.GetInstance(_logger, connectionManager);
        }

        [HttpGet(Name = "GetAllWellBoreFeatureCategoryId")]
        public ActionResult<IEnumerable<Guid>> GetAllWellBoreFeatureCategoryId()
        {
            var ids = _manager.GetAllWellBoreFeatureCategoryId();
            return ids != null ? Ok(ids) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("MetaInfo", Name = "GetAllWellBoreFeatureCategoryMetaInfo")]
        public ActionResult<IEnumerable<MetaInfo?>> GetAllWellBoreFeatureCategoryMetaInfo()
        {
            var metaInfos = _manager.GetAllWellBoreFeatureCategoryMetaInfo();
            return metaInfos != null ? Ok(metaInfos) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("{id}", Name = "GetWellBoreFeatureCategoryById")]
        public ActionResult<Model.WellBoreFeatureCategory?> GetWellBoreFeatureCategoryById(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest();
            }

            var data = _manager.GetWellBoreFeatureCategoryById(id);
            return data != null ? Ok(data) : NotFound();
        }

        [HttpGet("HeavyData", Name = "GetAllWellBoreFeatureCategory")]
        public ActionResult<IEnumerable<Model.WellBoreFeatureCategory?>> GetAllWellBoreFeatureCategory()
        {
            var data = _manager.GetAllWellBoreFeatureCategory();
            return data != null ? Ok(data) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPost(Name = "PostWellBoreFeatureCategory")]
        [ProducesResponseType<Model.WellBoreFeatureCategory>(StatusCodes.Status200OK)]
        public ActionResult PostWellBoreFeatureCategory([FromBody] Model.WellBoreFeatureCategory? data)
        {
            if (data?.MetaInfo == null || data.MetaInfo.ID == Guid.Empty)
            {
                return BadRequest();
            }

            if (_manager.GetWellBoreFeatureCategoryById(data.MetaInfo.ID) != null)
            {
                return StatusCode(StatusCodes.Status409Conflict);
            }

            return _manager.AddWellBoreFeatureCategory(data)
                ? Ok(data)
                : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPut("{id}", Name = "PutWellBoreFeatureCategoryById")]
        [ProducesResponseType<Model.WellBoreFeatureCategory>(StatusCodes.Status200OK)]
        [ProducesResponseType<WellBoreMutationErrorEnvelope>(StatusCodes.Status409Conflict)]
        public ActionResult PutWellBoreFeatureCategoryById(Guid id, [FromQuery, BindRequired] DateTimeOffset expectedModifiedUtc, [FromBody] Model.WellBoreFeatureCategory? data)
        {
            if (expectedModifiedUtc == default)
            {
                return BadRequest(new WellBoreMutationErrorEnvelope { Error = "invalid_request", Message = "expectedModifiedUtc is required." });
            }
            return this.ToActionResult(WellBoreCatalogMutationManager.UpdateFeatureCategory(_connectionManager, _logger, id, expectedModifiedUtc, data), data);
        }

        [HttpDelete("{id}", Name = "DeleteWellBoreFeatureCategoryById")]
        public ActionResult DeleteWellBoreFeatureCategoryById(Guid id)
        {
            return this.ToActionResult(WellBoreCatalogMutationManager.DeleteFeatureCategory(_connectionManager, _logger, id));
        }
    }
}


