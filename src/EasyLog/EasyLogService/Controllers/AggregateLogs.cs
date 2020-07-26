using System.Threading.Tasks;
using EasyLogService.Services.CentralLogService;
using LogEntries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EasyLogService.Controllers
{


    [Route("api/[controller]")]
    [ApiController]
    public class AggregateLogs : ControllerBase
    {
        readonly ICentralLogService _centralLog;

        public AggregateLogs(ICentralLogService centralLog)
        {
            _centralLog = centralLog;
        }

        [HttpPost] 
        public async Task<ActionResult> AddKubernetesJsonLogEntry([FromBody] LogEntry entryToAdd)
        {
            await _centralLog.AddLogEntry(entryToAdd);
            return Ok();
        }
    }
}
