using WoongBlog.Api.Common.Api;

namespace WoongBlog.Api.Modules.AI;

internal static class AiApiPaths
{
    private const string Root = $"{ApiPaths.Root}/admin/ai";

    internal const string RuntimeConfig = $"{Root}/runtime-config";
    internal const string BlogFix = $"{Root}/blog-fix";
    internal const string BlogFixBatch = $"{Root}/blog-fix-batch";
    internal const string BlogFixBatchJobs = $"{Root}/blog-fix-batch-jobs";
    internal const string BlogFixBatchJobById = $"{BlogFixBatchJobs}/{{jobId:guid}}";
    internal const string ApplyBlogFixBatchJob = $"{BlogFixBatchJobById}/apply";
    internal const string CancelBlogFixBatchJob = $"{BlogFixBatchJobById}/cancel";
    internal const string CancelQueuedBlogFixBatchJobs = $"{BlogFixBatchJobs}/cancel-queued";
    internal const string ClearCompletedBlogFixBatchJobs = $"{BlogFixBatchJobs}/clear-completed";
    internal const string WorkEnrich = $"{Root}/work-enrich";
}
