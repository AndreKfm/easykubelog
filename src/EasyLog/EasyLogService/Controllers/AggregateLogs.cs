using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EasyLogService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.Extensions.Logging;

namespace EasyLogService.Controllers
{


    [Route("api/[controller]")]
    [ApiController]
    public class AggregateLogs : ControllerBase
    {
        ILogger<AggregateLogs> _logger;
        ICentralLogService _centralLog;

        public AggregateLogs(ILogger<AggregateLogs> logger, ICentralLogService centralLog)
        {
            _logger = logger;
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
