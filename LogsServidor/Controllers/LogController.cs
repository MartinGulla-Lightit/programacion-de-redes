using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LogsServidor.Controllers;

[ApiController]
[Route("[controller]")]
public class LogController : ControllerBase
{

    private readonly ILogger<LogController> _logger;

    public LogController(ILogger<LogController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IEnumerable<Logs> Get()
    {
        var data = LogsDataAccess.GetInstance();
        return data.GetLogs();
    }
}
