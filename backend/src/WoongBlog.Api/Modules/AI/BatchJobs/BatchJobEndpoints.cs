using Microsoft.AspNetCore.Routing;
using MediatR;
using WoongBlog.Application.Modules.AI.BatchJobs;

namespace WoongBlog.Api.Modules.AI.BatchJobs;

internal static class BatchJobEndpoints
{
    internal static void MapCreateBlogFixBatchJob(this IEndpointRouteBuilder app)
    {
        app.MapPost(AiApiPaths.BlogFixBatchJobs, async (
                BlogFixBatchJobCreateRequest request,
                ISender sender,
                CancellationToken cancellationToken) => (await sender.Send(new CreateBlogFixBatchJobCommand(
                    request.BlogIds,
                    request.All,
                    request.SelectionMode,
                    request.SelectionLabel,
                    request.AutoApply,
                    request.WorkerCount,
                    request.Provider,
                    request.CodexModel,
                    request.CodexReasoningEffort,
                    request.CustomPrompt), cancellationToken)).ToHttpResult())
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin AI")
            .WithName("AdminAiCreateBlogFixBatchJob");
    }

    internal static void MapListBlogFixBatchJobs(this IEndpointRouteBuilder app)
    {
        app.MapGet(AiApiPaths.BlogFixBatchJobs, async (
                ISender sender,
                CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new ListBlogFixBatchJobsQuery(), cancellationToken)))
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin AI")
            .WithName("AdminAiListBlogFixBatchJobs");
    }

    internal static void MapGetBlogFixBatchJob(this IEndpointRouteBuilder app)
    {
        app.MapGet(AiApiPaths.BlogFixBatchJobById, async (
                Guid jobId,
                ISender sender,
                CancellationToken cancellationToken) =>
            await sender.Send(new GetBlogFixBatchJobQuery(jobId), cancellationToken) is { } response
                ? Results.Ok(response)
                : Results.NotFound())
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin AI")
            .WithName("AdminAiGetBlogFixBatchJob");
    }

    internal static void MapApplyBlogFixBatchJob(this IEndpointRouteBuilder app)
    {
        app.MapPost(AiApiPaths.ApplyBlogFixBatchJob, async (
                Guid jobId,
                BlogFixBatchJobApplyRequest request,
                ISender sender,
                CancellationToken cancellationToken) => (await sender.Send(new ApplyBlogFixBatchJobCommand(
                    jobId,
                    request.JobItemIds), cancellationToken)).ToHttpResult())
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin AI")
            .WithName("AdminAiApplyBlogFixBatchJob");
    }

    internal static void MapCancelBlogFixBatchJob(this IEndpointRouteBuilder app)
    {
        app.MapPost(AiApiPaths.CancelBlogFixBatchJob, async (
                Guid jobId,
                ISender sender,
                CancellationToken cancellationToken) =>
            (await sender.Send(new CancelBlogFixBatchJobCommand(jobId), cancellationToken)).ToHttpResult())
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin AI")
            .WithName("AdminAiCancelBlogFixBatchJob");
    }

    internal static void MapCancelQueuedBlogFixBatchJobs(this IEndpointRouteBuilder app)
    {
        app.MapPost(AiApiPaths.CancelQueuedBlogFixBatchJobs, async (
                ISender sender,
                CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new CancelQueuedBlogFixBatchJobsCommand(), cancellationToken)))
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin AI")
            .WithName("AdminAiCancelQueuedBlogFixBatchJobs");
    }

    internal static void MapClearCompletedBlogFixBatchJobs(this IEndpointRouteBuilder app)
    {
        app.MapPost(AiApiPaths.ClearCompletedBlogFixBatchJobs, async (
                ISender sender,
                CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new ClearCompletedBlogFixBatchJobsCommand(), cancellationToken)))
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin AI")
            .WithName("AdminAiClearCompletedBlogFixBatchJobs");
    }

    internal static void MapRemoveBlogFixBatchJob(this IEndpointRouteBuilder app)
    {
        app.MapDelete(AiApiPaths.BlogFixBatchJobById, async (
                Guid jobId,
                ISender sender,
                CancellationToken cancellationToken) =>
            (await sender.Send(new RemoveBlogFixBatchJobCommand(jobId), cancellationToken)).ToHttpResult())
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin AI")
            .WithName("AdminAiRemoveBlogFixBatchJob");
    }
}
