using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Api.Modules.Content.Works.Application.Abstractions;

public interface IWorkCommandStore
{
    Task<bool> SlugExistsAsync(string slug, Guid? excludedWorkId, CancellationToken cancellationToken);
    Task<Work?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkVideo>> GetVideosForWorkAsync(Guid workId, CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkVideoUploadSession>> GetUploadSessionsForWorkAsync(Guid workId, CancellationToken cancellationToken);
    void Add(Work work);
    void Remove(Work work);
    void RemoveVideos(IEnumerable<WorkVideo> videos);
    void RemoveUploadSessions(IEnumerable<WorkVideoUploadSession> uploadSessions);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
