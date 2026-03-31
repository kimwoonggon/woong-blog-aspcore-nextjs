using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Modules.AI.Application;

namespace WoongBlog.Api.Modules.AI.Api.BatchJobs;

internal static class BatchJobEndpoints
{
    internal static void MapCreateBlogFixBatchJob(this IEndpointRouteBuilder app)
    {
        app.MapPost(AiApiPaths.BlogFixBatchJobs, (
                BlogFixBatchJobCreateRequest request,
                IAiAdminService service,
                CancellationToken cancellationToken) =>
            service.CreateBlogFixBatchJobAsync(request, cancellationToken))
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin AI")
            .WithName("AdminAiCreateBlogFixBatchJob");
    }

    internal static void MapListBlogFixBatchJobs(this IEndpointRouteBuilder app)
    {
        app.MapGet(AiApiPaths.BlogFixBatchJobs, (
                IAiAdminService service,
                CancellationToken cancellationToken) =>
            service.ListBlogFixBatchJobsAsync(cancellationToken))
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin AI")
            .WithName("AdminAiListBlogFixBatchJobs");
    }

    internal static void MapGetBlogFixBatchJob(this IEndpointRouteBuilder app)
    {
        app.MapGet(AiApiPaths.BlogFixBatchJobById, (
                Guid jobId,
                IAiAdminService service,
                CancellationToken cancellationToken) =>
            service.GetBlogFixBatchJobAsync(jobId, cancellationToken))
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin AI")
            .WithName("AdminAiGetBlogFixBatchJob");
    }

    internal static void MapApplyBlogFixBatchJob(this IEndpointRouteBuilder app)
    {
        app.MapPost(AiApiPaths.ApplyBlogFixBatchJob, (
                Guid jobId,
                BlogFixBatchJobApplyRequest request,
                IAiAdminService service,
                CancellationToken cancellationToken) =>
            service.ApplyBlogFixBatchJobAsync(jobId, request, cancellationToken))
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin AI")
            .WithName("AdminAiApplyBlogFixBatchJob");
    }

    internal static void MapCancelBlogFixBatchJob(this IEndpointRouteBuilder app)
    {
        app.MapPost(AiApiPaths.CancelBlogFixBatchJob, (
                Guid jobId,
                IAiAdminService service,
                CancellationToken cancellationToken) =>
            service.CancelBlogFixBatchJobAsync(jobId, cancellationToken))
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin AI")
            .WithName("AdminAiCancelBlogFixBatchJob");
    }

    internal static void MapCancelQueuedBlogFixBatchJobs(this IEndpointRouteBuilder app)
    {
        app.MapPost(AiApiPaths.CancelQueuedBlogFixBatchJobs, (
                IAiAdminService service,
                CancellationToken cancellationToken) =>
            service.CancelQueuedBlogFixBatchJobsAsync(cancellationToken))
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin AI")
            .WithName("AdminAiCancelQueuedBlogFixBatchJobs");
    }

    internal static void MapClearCompletedBlogFixBatchJobs(this IEndpointRouteBuilder app)
    {
        app.MapPost(AiApiPaths.ClearCompletedBlogFixBatchJobs, (
                IAiAdminService service,
                CancellationToken cancellationToken) =>
            service.ClearCompletedBlogFixBatchJobsAsync(cancellationToken))
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin AI")
            .WithName("AdminAiClearCompletedBlogFixBatchJobs");
    }

    internal static void MapRemoveBlogFixBatchJob(this IEndpointRouteBuilder app)
    {
        app.MapDelete(AiApiPaths.BlogFixBatchJobById, (
                Guid jobId,
                IAiAdminService service,
                CancellationToken cancellationToken) =>
            service.RemoveBlogFixBatchJobAsync(jobId, cancellationToken))
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin AI")
            .WithName("AdminAiRemoveBlogFixBatchJob");
    }
}
