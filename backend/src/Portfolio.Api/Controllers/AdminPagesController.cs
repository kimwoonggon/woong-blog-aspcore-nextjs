using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Api.Application.Admin.GetAdminPages;
using Portfolio.Api.Application.Admin.UpdatePage;
using Portfolio.Api.Controllers.Models;

namespace Portfolio.Api.Controllers;

[ApiController]
[Route("api/admin/pages")]
[Authorize(Policy = "AdminOnly")]
public class AdminPagesController : ControllerBase
{
    private readonly ISender _sender;

    public AdminPagesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string[]? slugs, CancellationToken cancellationToken)
    {
        var pages = await _sender.Send(new GetAdminPagesQuery(slugs), cancellationToken);
        return Ok(pages);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdatePageRequest request, CancellationToken cancellationToken)
    {
        var updated = await _sender.Send(
            new UpdatePageCommand(request.Id, request.Title, request.ContentJson),
            cancellationToken);

        if (!updated.Found)
        {
            return NotFound();
        }

        return Ok(new { success = true });
    }
}
