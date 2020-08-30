using EasyKubeLogService.Services.CentralLogService;
using LogEntries;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace EasyKubeLogService.Controllers
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
