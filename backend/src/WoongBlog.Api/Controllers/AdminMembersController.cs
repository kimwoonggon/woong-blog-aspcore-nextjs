using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WoongBlog.Api.Application.Admin.GetAdminMembers;

namespace WoongBlog.Api.Controllers;

[ApiController]
[Route("api/admin/members")]
[Authorize(Policy = "AdminOnly")]
public class AdminMembersController : ControllerBase
{
    private readonly ISender _sender;

    public AdminMembersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var members = await _sender.Send(new GetAdminMembersQuery(), cancellationToken);
        return Ok(members);
    }
}
