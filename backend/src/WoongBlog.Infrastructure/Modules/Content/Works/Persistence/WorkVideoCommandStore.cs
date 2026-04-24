using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Infrastructure.Persistence;
using WoongBlog.Infrastructure.Modules.Content.Works.WorkVideos;

namespace WoongBlog.Infrastructure.Modules.Content.Works.Persistence;

public sealed class WorkVideoCommandStore(WoongBlogDbContext dbContext) : IWorkVideoCommandStore
{
    public Task<Work?> GetWorkForUpdateAsync(Guid workId, CancellationToken cancellationToken)
    {
        return dbContext.Works.SingleOrDefaultAsync(x => x.Id == workId, cancellationToken);
    }

    public Task<WorkVideoUploadSession?> GetUploadSessionForUpdateAsync(
        Guid workId,
        Guid uploadSessionId,
        CancellationToken cancellationToken)
    {
        return dbContext.WorkVideoUploadSessions.SingleOrDefaultAsync(
            x => x.Id == uploadSessionId && x.WorkId == workId,
            cancellationToken);
    }

    public Task<WorkVideo?> GetVideoForUpdateAsync(Guid workId, Guid videoId, CancellationToken cancellationToken)
    {
        return dbContext.WorkVideos.SingleOrDefaultAsync(
            x => x.Id == videoId && x.WorkId == workId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<WorkVideo>> GetVideosForWorkAsync(Guid workId, CancellationToken cancellationToken)
    {
        return await dbContext.WorkVideos
            .Where(x => x.WorkId == workId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountVideosAsync(Guid workId, CancellationToken cancellationToken)
    {
        return dbContext.WorkVideos.CountAsync(x => x.WorkId == workId, cancellationToken);
    }

    public async Task<int> GetNextSortOrderAsync(Guid workId, CancellationToken cancellationToken)
    {
        var maxSortOrder = await dbContext.WorkVideos
            .Where(x => x.WorkId == workId)
            .Select(x => (int?)x.SortOrder)
            .MaxAsync(cancellationToken);

        return (maxSortOrder ?? -1) + 1;
    }

    public void AddUploadSession(WorkVideoUploadSession session)
    {
        dbContext.WorkVideoUploadSessions.Add(session);
    }

    public void AddWorkVideo(WorkVideo video)
    {
        dbContext.WorkVideos.Add(video);
    }

    public void RemoveWorkVideo(WorkVideo video)
    {
        dbContext.WorkVideos.Remove(video);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
