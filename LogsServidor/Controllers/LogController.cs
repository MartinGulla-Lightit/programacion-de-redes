using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using LogsServidor.Service;
using LogsServidor.Data;
using LogsServidor.Filters;

namespace LogsServidor.Controllers;

[ApiController]
[Route("logs")]
public class LogController : ControllerBase
{

    private readonly ILogger<LogController> _logger;

    public LogController(ILogger<LogController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Get([FromQuery] LogsSearchCriteria criteria)
    {
        try {
            var data = LogsDataAccess.GetInstance();
            var logs = data.GetLogs();
            var filteredLogs = logs.Where(log => criteria.Filter(log));

            return Ok(filteredLogs);
        } catch(Exception e) {
            return BadRequest(e.Message);
        }
        
    }
}
