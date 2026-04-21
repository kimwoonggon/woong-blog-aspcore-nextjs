using MediatR;
using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

public sealed class AddYouTubeWorkVideoCommandHandler(
    IWorkVideoCommandStore commandStore,
    IWorkVideoQueryStore queryStore)
    : IRequestHandler<AddYouTubeWorkVideoCommand, WorkVideoResult<WorkVideosMutationResult>>
{
    public async Task<WorkVideoResult<WorkVideosMutationResult>> Handle(
        AddYouTubeWorkVideoCommand request,
        CancellationToken cancellationToken)
    {
        var work = await commandStore.GetWorkForUpdateAsync(request.WorkId, cancellationToken);
        if (work is null)
        {
            return WorkVideoResult<WorkVideosMutationResult>.NotFound("Work not found.");
        }

        if (work.VideosVersion != request.ExpectedVideosVersion)
        {
            return WorkVideoResult<WorkVideosMutationResult>.Conflict("Videos changed. Refresh and retry.");
        }

        if (await commandStore.CountVideosAsync(request.WorkId, cancellationToken) >= WorkVideoPolicy.MaxVideosPerWork)
        {
            return WorkVideoResult<WorkVideosMutationResult>.BadRequest("Each work supports up to 10 videos.");
        }

        var videoId = WorkVideoPolicy.NormalizeYouTubeVideoId(request.YoutubeUrlOrId);
        if (videoId is null)
        {
            return WorkVideoResult<WorkVideosMutationResult>.BadRequest("Provide a valid YouTube video URL or ID.");
        }

        commandStore.AddWorkVideo(new WorkVideo
        {
            Id = Guid.NewGuid(),
            WorkId = request.WorkId,
            SourceType = WorkVideoSourceTypes.YouTube,
            SourceKey = videoId,
            SortOrder = await commandStore.GetNextSortOrderAsync(request.WorkId, cancellationToken),
            CreatedAt = DateTimeOffset.UtcNow
        });

        work.VideosVersion += 1;
        await commandStore.SaveChangesAsync(cancellationToken);
        return WorkVideoResult<WorkVideosMutationResult>.Ok(
            await queryStore.GetMutationResultAsync(request.WorkId, cancellationToken));
    }
}
