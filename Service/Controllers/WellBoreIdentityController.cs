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
    public class WellBoreIdentityController : ControllerBase
    {
        private readonly ILogger<WellBoreIdentityManager> _logger;
        private readonly WellBoreIdentityManager _manager;
        private readonly SqlConnectionManager _connectionManager;

        public WellBoreIdentityController(ILogger<WellBoreIdentityManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _connectionManager = connectionManager;
            _manager = WellBoreIdentityManager.GetInstance(_logger, connectionManager);
        }

        [HttpGet(Name = "GetAllWellBoreIdentityId")]
        public ActionResult<IEnumerable<Guid>> GetAllWellBoreIdentityId()
        {
            var ids = _manager.GetAllWellBoreIdentityId();
            return ids != null ? Ok(ids) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("MetaInfo", Name = "GetAllWellBoreIdentityMetaInfo")]
        public ActionResult<IEnumerable<MetaInfo?>> GetAllWellBoreIdentityMetaInfo()
        {
            var metaInfos = _manager.GetAllWellBoreIdentityMetaInfo();
            return metaInfos != null ? Ok(metaInfos) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("{id}", Name = "GetWellBoreIdentityById")]
        public ActionResult<Model.WellBoreIdentity?> GetWellBoreIdentityById(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest();
            }

            var data = _manager.GetWellBoreIdentityById(id);
            return data != null ? Ok(data) : NotFound();
        }

        [HttpGet("HeavyData", Name = "GetAllWellBoreIdentity")]
        public ActionResult<IEnumerable<Model.WellBoreIdentity?>> GetAllWellBoreIdentity()
        {
            var data = _manager.GetAllWellBoreIdentity();
            return data != null ? Ok(data) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPost(Name = "PostWellBoreIdentity")]
        [ProducesResponseType<Model.WellBoreIdentity>(StatusCodes.Status200OK)]
        public ActionResult PostWellBoreIdentity([FromBody] Model.WellBoreIdentity? data)
        {
            if (data?.MetaInfo == null || data.MetaInfo.ID == Guid.Empty)
            {
                return BadRequest();
            }

            if (_manager.GetWellBoreIdentityById(data.MetaInfo.ID) != null)
            {
                return StatusCode(StatusCodes.Status409Conflict);
            }

            return _manager.AddWellBoreIdentity(data)
                ? Ok(data)
                : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPut("{id}", Name = "PutWellBoreIdentityById")]
        [ProducesResponseType<Model.WellBoreIdentity>(StatusCodes.Status200OK)]
        [ProducesResponseType<WellBoreMutationErrorEnvelope>(StatusCodes.Status409Conflict)]
        public ActionResult PutWellBoreIdentityById(Guid id, [FromQuery, BindRequired] DateTimeOffset expectedModifiedUtc, [FromBody] Model.WellBoreIdentity? data)
        {
            if (expectedModifiedUtc == default)
            {
                return BadRequest(new WellBoreMutationErrorEnvelope { Error = "invalid_request", Message = "expectedModifiedUtc is required." });
            }
            return this.ToActionResult(WellBoreCatalogMutationManager.UpdateIdentity(_connectionManager, _logger, id, expectedModifiedUtc, data), data);
        }

        [HttpDelete("{id}", Name = "DeleteWellBoreIdentityById")]
        public ActionResult DeleteWellBoreIdentityById(Guid id)
        {
            return this.ToActionResult(WellBoreCatalogMutationManager.DeleteIdentity(_connectionManager, _logger, id));
        }
    }
}


