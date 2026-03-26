using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Api.Application.Admin.CreateBlog;
using Portfolio.Api.Application.Admin.DeleteBlog;
using Portfolio.Api.Application.Admin.GetAdminBlogById;
using Portfolio.Api.Application.Admin.GetAdminBlogs;
using Portfolio.Api.Application.Admin.UpdateBlog;
using Portfolio.Api.Controllers.Models;

namespace Portfolio.Api.Controllers;

[ApiController]
[Route("api/admin/blogs")]
[Authorize(Policy = "AdminOnly")]
public class AdminBlogsController : ControllerBase
{
    private readonly ISender _sender;

    public AdminBlogsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var blogs = await _sender.Send(new GetAdminBlogsQuery(), cancellationToken);
        return Ok(blogs);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var blog = await _sender.Send(new GetAdminBlogByIdQuery(id), cancellationToken);

        return blog is null
            ? NotFound()
            : Ok(blog);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveBlogRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateBlogCommand(request.Title, request.Tags, request.Published, request.ContentJson),
            cancellationToken);

        return Ok(new { result.Id, result.Slug });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveBlogRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateBlogCommand(id, request.Title, request.Tags, request.Published, request.ContentJson),
            cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(new { result.Id, result.Slug });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _sender.Send(new DeleteBlogCommand(id), cancellationToken);
        if (!deleted.Found)
        {
            return NotFound();
        }
        return NoContent();
    }
}
