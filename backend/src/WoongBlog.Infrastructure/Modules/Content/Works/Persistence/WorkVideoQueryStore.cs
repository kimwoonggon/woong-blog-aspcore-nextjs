using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Infrastructure.Persistence;
using WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

namespace WoongBlog.Api.Modules.Content.Works.Persistence;

public sealed class WorkVideoQueryStore(
    WoongBlogDbContext dbContext,
    IWorkVideoPlaybackUrlBuilder playbackUrlBuilder) : IWorkVideoQueryStore
{
    public async Task<WorkVideosMutationResult> GetMutationResultAsync(
        Guid workId,
        CancellationToken cancellationToken)
    {
        var work = await dbContext.Works
            .AsNoTracking()
            .SingleAsync(x => x.Id == workId, cancellationToken);

        var videos = await dbContext.WorkVideos
            .AsNoTracking()
            .Where(x => x.WorkId == workId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return new WorkVideosMutationResult(
            work.VideosVersion,
            videos.Select(video => new WorkVideoDto(
                video.Id,
                video.SourceType,
                video.SourceKey,
                playbackUrlBuilder.BuildPlaybackUrl(video.SourceType, video.SourceKey),
                video.OriginalFileName,
                video.MimeType,
                video.FileSize,
                video.SortOrder,
                video.CreatedAt)).ToList());
    }
}
