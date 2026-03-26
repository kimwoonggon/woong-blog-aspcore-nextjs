using Microsoft.AspNetCore.Mvc;

namespace Portfolio.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "ok",
            service = "portfolio-api",
            timestamp = DateTimeOffset.UtcNow
        });
    }
}
