using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Api.Application.Admin.CreateWork;
using Portfolio.Api.Application.Admin.DeleteWork;
using Portfolio.Api.Application.Admin.GetAdminWorkById;
using Portfolio.Api.Application.Admin.GetAdminWorks;
using Portfolio.Api.Application.Admin.UpdateWork;
using Portfolio.Api.Controllers.Models;

namespace Portfolio.Api.Controllers;

[ApiController]
[Route("api/admin/works")]
[Authorize(Policy = "AdminOnly")]
public class AdminWorksController : ControllerBase
{
    private readonly ISender _sender;

    public AdminWorksController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var works = await _sender.Send(new GetAdminWorksQuery(), cancellationToken);
        return Ok(works);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var work = await _sender.Send(new GetAdminWorkByIdQuery(id), cancellationToken);

        return work is null
            ? NotFound()
            : Ok(work);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveWorkRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateWorkCommand(
                request.Title,
                request.Category,
                request.Period,
                request.Tags,
                request.Published,
                request.ContentJson,
                request.AllPropertiesJson,
                request.ThumbnailAssetId,
                request.IconAssetId),
            cancellationToken);

        return Ok(new { result.Id, result.Slug });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveWorkRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateWorkCommand(
                id,
                request.Title,
                request.Category,
                request.Period,
                request.Tags,
                request.Published,
                request.ContentJson,
                request.AllPropertiesJson,
                request.ThumbnailAssetId,
                request.IconAssetId),
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
        var deleted = await _sender.Send(new DeleteWorkCommand(id), cancellationToken);
        if (!deleted.Found)
        {
            return NotFound();
        }
        return NoContent();
    }
}
