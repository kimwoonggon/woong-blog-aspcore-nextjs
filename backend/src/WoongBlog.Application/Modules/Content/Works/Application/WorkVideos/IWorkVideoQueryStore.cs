namespace WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

public interface IWorkVideoQueryStore
{
    Task<WorkVideosMutationResult> GetMutationResultAsync(Guid workId, CancellationToken cancellationToken);
}
