using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NORCE.Drilling.WellBore.Model;

namespace NORCE.Drilling.WellBore.Service.Controllers
{
    [Produces("application/json")]
    [Route("[controller]")]
    [ApiController]
    public class WellBoreUsageStatisticsController : ControllerBase
    {
        private readonly ILogger _logger;

        public WellBoreUsageStatisticsController(ILogger<WellBoreUsageStatisticsController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Returns the usage statistics present in the microservice database at endpoint WellBore/api/WellBoreUsageStatistics
        /// </summary>
        /// <returns></returns>
        [HttpGet(Name = "GetWellBoreUsageStatistics")]
        public ActionResult<UsageStatisticsWellBore> GetWellBoreUsageStatistics()
        {
            if (UsageStatisticsWellBore.Instance != null)
            {
                return Ok(UsageStatisticsWellBore.Instance);
            }
            else
            {
                return NotFound();
            }
        }
    }
}
