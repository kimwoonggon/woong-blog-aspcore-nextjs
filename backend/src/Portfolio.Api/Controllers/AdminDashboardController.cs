using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Api.Application.Admin.GetDashboardSummary;

namespace Portfolio.Api.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Policy = "AdminOnly")]
public class AdminDashboardController : ControllerBase
{
    private readonly ISender _sender;

    public AdminDashboardController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetDashboardSummaryQuery(), cancellationToken);
        return Ok(result);
    }
}
