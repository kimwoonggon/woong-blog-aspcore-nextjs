using System.Text.Json;
using MediatR;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Modules.AI.Application;
using WoongBlog.Api.Modules.AI.Application.Abstractions;
using WoongBlog.Api.Modules.Content.Common.Application.Support;

namespace WoongBlog.Api.Modules.AI.Application.BatchJobs;

public sealed class ApplyBlogFixBatchJobCommandHandler(
    IAiBatchJobQueryStore jobQueryStore,
    IAiBatchTargetQueryStore targetStore,
    IAiBatchJobCommandStore commandStore)
    : IRequestHandler<ApplyBlogFixBatchJobCommand, AiActionResult<BlogFixBatchJobDetailResponse>>
{
    public async Task<AiActionResult<BlogFixBatchJobDetailResponse>> Handle(
        ApplyBlogFixBatchJobCommand request,
        CancellationToken cancellationToken)
    {
        var job = await jobQueryStore.GetBlogJobAsync(request.JobId, cancellationToken);
        if (job is null)
        {
            return AiActionResult<BlogFixBatchJobDetailResponse>.NotFound();
        }

        var items = await jobQueryStore.GetSucceededUnappliedItemsAsync(request.JobId, request.JobItemIds, cancellationToken);
        var blogIds = items.Select(item => item.EntityId).ToArray();
        var blogs = await targetStore.GetBlogsForUpdateAsync(blogIds, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        foreach (var item in items)
        {
            if (!blogs.TryGetValue(item.EntityId, out var blog) || string.IsNullOrWhiteSpace(item.FixedHtml))
            {
                continue;
            }

            blog.ContentJson = $$"""{"html":{{JsonSerializer.Serialize(item.FixedHtml)}}}""";
            blog.Excerpt = AdminContentText.GenerateExcerpt(item.FixedHtml);
            blog.UpdatedAt = now;
            item.AppliedAt = now;
            item.Status = AiBatchJobItemStates.Applied;
        }

        await commandStore.SaveChangesAsync(cancellationToken);
        await RefreshJobAggregatesAsync(jobQueryStore, commandStore, job, cancellationToken);

        var refreshedItems = await jobQueryStore.GetJobItemsAsync(request.JobId, cancellationToken);
        return AiActionResult<BlogFixBatchJobDetailResponse>.Ok(
            AiBatchJobResponseMapper.ToJobDetailResponse(job, refreshedItems, includeHtml: true));
    }

    private static async Task RefreshJobAggregatesAsync(
        IAiBatchJobQueryStore jobQueryStore,
        IAiBatchJobCommandStore commandStore,
        AiBatchJob job,
        CancellationToken cancellationToken)
    {
        var items = await jobQueryStore.GetJobItemsAsync(job.Id, cancellationToken);
        AiBatchJobProgressPolicy.RefreshCounts(job, items, DateTimeOffset.UtcNow);
        await commandStore.SaveChangesAsync(cancellationToken);
    }
}
