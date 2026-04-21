using MediatR;

namespace WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

public sealed class DeleteWorkVideoCommandHandler(
    IWorkVideoCommandStore commandStore,
    IWorkVideoQueryStore queryStore)
    : IRequestHandler<DeleteWorkVideoCommand, WorkVideoResult<WorkVideosMutationResult>>
{
    public async Task<WorkVideoResult<WorkVideosMutationResult>> Handle(
        DeleteWorkVideoCommand request,
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

        var video = await commandStore.GetVideoForUpdateAsync(request.WorkId, request.VideoId, cancellationToken);
        if (video is null)
        {
            return WorkVideoResult<WorkVideosMutationResult>.NotFound("Video not found.");
        }

        await commandStore.EnqueueCleanupAsync(
            request.WorkId,
            video.Id,
            video.SourceType,
            video.SourceKey,
            DateTimeOffset.UtcNow,
            cancellationToken);
        commandStore.RemoveWorkVideo(video);

        var remainingVideos = (await commandStore.GetVideosForWorkAsync(request.WorkId, cancellationToken))
            .Where(x => x.Id != request.VideoId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .ToList();

        for (var index = 0; index < remainingVideos.Count; index += 1)
        {
            remainingVideos[index].SortOrder = index;
        }

        work.VideosVersion += 1;
        await commandStore.SaveChangesAsync(cancellationToken);
        return WorkVideoResult<WorkVideosMutationResult>.Ok(
            await queryStore.GetMutationResultAsync(request.WorkId, cancellationToken));
    }
}
