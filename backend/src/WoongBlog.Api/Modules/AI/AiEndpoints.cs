using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Modules.AI.BatchJobs;
using WoongBlog.Api.Modules.AI.BlogFix;
using WoongBlog.Api.Modules.AI.RuntimeConfig;
using WoongBlog.Api.Modules.AI.WorkEnrich;

namespace WoongBlog.Api.Modules.AI;

internal static class AiEndpoints
{
    internal static void MapAiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapAiRuntimeConfig();
        app.MapBlogFix();
        app.MapBlogFixBatch();
        app.MapCreateBlogFixBatchJob();
        app.MapListBlogFixBatchJobs();
        app.MapGetBlogFixBatchJob();
        app.MapApplyBlogFixBatchJob();
        app.MapCancelBlogFixBatchJob();
        app.MapCancelQueuedBlogFixBatchJobs();
        app.MapClearCompletedBlogFixBatchJobs();
        app.MapRemoveBlogFixBatchJob();
        app.MapWorkEnrich();
    }
}
