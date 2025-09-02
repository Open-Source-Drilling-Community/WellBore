using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OSDC.DotnetLibraries.General.DataManagement;
using NORCE.Drilling.WellBore.Service.Managers;
using NORCE.Drilling.WellBore.Model;

namespace NORCE.Drilling.WellBore.Service.Controllers
{
    [Produces("application/json")]
    [Route("[controller]")]
    [ApiController]
    public class WellBoreController : ControllerBase
    {
        private readonly ILogger<WellBoreManager> _logger;
        private readonly WellBoreManager _wellBoreManager;

        public WellBoreController(ILogger<WellBoreManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _wellBoreManager = WellBoreManager.GetInstance(_logger, connectionManager);
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

        /// <summary>
        /// Performs calculation on the given WellBore and adds it to the microservice database, at the endpoint WellBore/api/WellBore
        /// </summary>
        /// <param name="wellBore"></param>
        /// <returns>true if the given WellBore has been added successfully to the microservice database, at the endpoint WellBore/api/WellBore</returns>
        [HttpPost(Name = "PostWellBore")]
        public ActionResult PostWellBore([FromBody] Model.WellBore? data)
        {
            UsageStatisticsWellBore.Instance.IncrementPostWellBorePerDay();
            // Check if wellBore exists in the database through ID
            if (data != null && data.MetaInfo != null && data.MetaInfo.ID != Guid.Empty)
            {
                var existingData = _wellBoreManager.GetWellBoreById(data.MetaInfo.ID);
                if (existingData == null)
                {   
                    //  If wellBore was not found, call AddWellBore, where the wellBore.Calculate()
                    // method is called. 
                    if (_wellBoreManager.AddWellBore(data))
                    {
                        return Ok(); // status=OK is used rather than status=Created because NSwag auto-generated controllers use 200 (OK) rather than 201 (Created) as return codes
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError);
                    }
                }
                else
                {
                    _logger.LogWarning("The given WellBore already exists and will not be added");
                    return StatusCode(StatusCodes.Status409Conflict);
                }
            }
            else
            {
                _logger.LogWarning("The given WellBore is null, badly formed, or its ID is empty");
                return BadRequest();
            }
        }

        /// <summary>
        /// Performs calculation on the given WellBore and updates it in the microservice database, at the endpoint WellBore/api/WellBore/id
        /// </summary>
        /// <param name="wellBore"></param>
        /// <returns>true if the given WellBore has been updated successfully to the microservice database, at the endpoint WellBore/api/WellBore/id</returns>
        [HttpPut("{id}", Name = "PutWellBoreById")]
        public ActionResult PutWellBoreById(Guid id, [FromBody] Model.WellBore? data)
        {
            UsageStatisticsWellBore.Instance.IncrementPutWellBoreByIdPerDay();
            // Check if WellBore is in the data base
            if (data != null && data.MetaInfo != null && data.MetaInfo.ID.Equals(id))
            {
                var existingData = _wellBoreManager.GetWellBoreById(id);
                if (existingData != null)
                {
                    if (_wellBoreManager.UpdateWellBoreById(id, data))
                    {
                        return Ok();
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError);
                    }
                }
                else
                {
                    _logger.LogWarning("The given WellBore has not been found in the database");
                    return NotFound();
                }
            }
            else
            {
                _logger.LogWarning("The given WellBore is null, badly formed, or its does not match the ID to update");
                return BadRequest();
            }
        }

        /// <summary>
        /// Deletes the WellBore of given ID from the microservice database, at the endpoint WellBore/api/WellBore/id
        /// </summary>
        /// <param name="guid"></param>
        /// <returns>true if the WellBore was deleted from the microservice database, at the endpoint WellBore/api/WellBore/id</returns>
        [HttpDelete("{id}", Name = "DeleteWellBoreById")]
        public ActionResult DeleteWellBoreById(Guid id)
        {
            UsageStatisticsWellBore.Instance.IncrementDeleteWellBoreByIdPerDay();
            if (_wellBoreManager.GetWellBoreById(id) != null)
            {
                if (_wellBoreManager.DeleteWellBoreById(id))
                {
                    return Ok();
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }
            }
            else
            {
                _logger.LogWarning("The WellBore of given ID does not exist");
                return NotFound();
            }
        }
    }
}
