namespace WoongBlog.Application.Modules.Content.Works.WorkVideos;

public interface IWorkVideoQueryStore
{
    Task<WorkVideosMutationResult> GetMutationResultAsync(Guid workId, CancellationToken cancellationToken);
}
