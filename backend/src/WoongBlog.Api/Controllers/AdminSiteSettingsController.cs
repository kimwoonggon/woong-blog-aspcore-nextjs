using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WoongBlog.Api.Application.Admin.GetAdminSiteSettings;
using WoongBlog.Api.Application.Admin.UpdateSiteSettings;
using WoongBlog.Api.Controllers.Models;

namespace WoongBlog.Api.Controllers;

[ApiController]
[Route("api/admin/site-settings")]
[Authorize(Policy = "AdminOnly")]
public class AdminSiteSettingsController : ControllerBase
{
    private readonly ISender _sender;

    public AdminSiteSettingsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var settings = await _sender.Send(new GetAdminSiteSettingsQuery(), cancellationToken);

        if (settings is null)
        {
            return NotFound();
        }

        return Ok(settings);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateSiteSettingsRequest request, CancellationToken cancellationToken)
    {
        var updated = await _sender.Send(
            new UpdateSiteSettingsCommand(
                request.OwnerName,
                request.Tagline,
                request.FacebookUrl,
                request.InstagramUrl,
                request.TwitterUrl,
                request.LinkedInUrl,
                request.GitHubUrl,
                request.ResumeAssetId,
                request.HasResumeAssetId),
            cancellationToken);

        if (!updated.Found)
        {
            return NotFound();
        }

        return Ok(new { success = true });
    }
}
