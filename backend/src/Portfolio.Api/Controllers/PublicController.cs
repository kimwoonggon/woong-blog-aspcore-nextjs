using MediatR;
using Portfolio.Api.Application.Public.GetBlogBySlug;
using Portfolio.Api.Application.Public.GetBlogs;
using Portfolio.Api.Application.Public.GetHome;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Api.Application.Public.GetPageBySlug;
using Portfolio.Api.Application.Public.GetResume;
using Portfolio.Api.Application.Public.GetSiteSettings;
using Portfolio.Api.Application.Public.GetWorkBySlug;
using Portfolio.Api.Application.Public.GetWorks;

namespace Portfolio.Api.Controllers;

[ApiController]
[Route("api/public")]
public class PublicController : ControllerBase
{
    private readonly ISender _sender;

    public PublicController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("site-settings")]
    public async Task<IActionResult> GetSiteSettings(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetSiteSettingsQuery(), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("home")]
    public async Task<IActionResult> GetHome(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetHomeQuery(), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("pages/{slug}")]
    public async Task<IActionResult> GetPage(string slug, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetPageBySlugQuery(slug), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("works")]
    public async Task<IActionResult> GetWorks([FromQuery] int page = 1, [FromQuery] int pageSize = 6, CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetWorksQuery(page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("works/{slug}")]
    public async Task<IActionResult> GetWork(string slug, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetWorkBySlugQuery(slug), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("blogs")]
    public async Task<IActionResult> GetBlogs([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetBlogsQuery(page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("blogs/{slug}")]
    public async Task<IActionResult> GetBlog(string slug, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetBlogBySlugQuery(slug), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("resume")]
    public async Task<IActionResult> GetResume(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetResumeQuery(), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
