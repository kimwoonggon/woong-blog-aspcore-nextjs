using MediatR;
using WoongBlog.Api.Modules.AI.Application;
using WoongBlog.Api.Modules.AI.Application.Abstractions;

namespace WoongBlog.Api.Modules.AI.Application.BatchJobs;

public sealed class GetBlogFixBatchJobQueryHandler(IAiBatchJobQueryStore jobQueryStore)
    : IRequestHandler<GetBlogFixBatchJobQuery, BlogFixBatchJobDetailResponse?>
{
    public async Task<BlogFixBatchJobDetailResponse?> Handle(GetBlogFixBatchJobQuery request, CancellationToken cancellationToken)
    {
        var job = await jobQueryStore.GetBlogJobAsync(request.JobId, cancellationToken);
        if (job is null)
        {
            return null;
        }

        var items = await jobQueryStore.GetJobItemsAsync(request.JobId, cancellationToken);
        return AiBatchJobResponseMapper.ToJobDetailResponse(job, items, includeHtml: true);
    }
}
