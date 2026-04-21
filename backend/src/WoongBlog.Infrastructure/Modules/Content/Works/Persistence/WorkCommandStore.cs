using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Infrastructure.Persistence;
using WoongBlog.Api.Modules.Content.Works.Application.Abstractions;

namespace WoongBlog.Api.Modules.Content.Works.Persistence;

public sealed class WorkCommandStore(WoongBlogDbContext dbContext) : IWorkCommandStore
{
    public Task<bool> SlugExistsAsync(string slug, Guid? excludedWorkId, CancellationToken cancellationToken)
    {
        return dbContext.Works.AnyAsync(
            x => x.Slug == slug && (!excludedWorkId.HasValue || x.Id != excludedWorkId.Value),
            cancellationToken);
    }

    public Task<Work?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Works.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<WorkVideo>> GetVideosForWorkAsync(Guid workId, CancellationToken cancellationToken)
    {
        return await dbContext.WorkVideos
            .Where(x => x.WorkId == workId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkVideoUploadSession>> GetUploadSessionsForWorkAsync(Guid workId, CancellationToken cancellationToken)
    {
        return await dbContext.WorkVideoUploadSessions
            .Where(x => x.WorkId == workId)
            .ToListAsync(cancellationToken);
    }

    public void Add(Work work)
    {
        dbContext.Works.Add(work);
    }

    public void Remove(Work work)
    {
        dbContext.Works.Remove(work);
    }

    public void RemoveVideos(IEnumerable<WorkVideo> videos)
    {
        dbContext.WorkVideos.RemoveRange(videos);
    }

    public void RemoveUploadSessions(IEnumerable<WorkVideoUploadSession> uploadSessions)
    {
        dbContext.WorkVideoUploadSessions.RemoveRange(uploadSessions);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
