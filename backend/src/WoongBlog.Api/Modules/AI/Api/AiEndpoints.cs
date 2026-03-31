using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Modules.AI.Api.BatchJobs;
using WoongBlog.Api.Modules.AI.Api.BlogFix;
using WoongBlog.Api.Modules.AI.Api.RuntimeConfig;
using WoongBlog.Api.Modules.AI.Api.WorkEnrich;

namespace WoongBlog.Api.Modules.AI.Api;

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
