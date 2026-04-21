using System.Text.Json;
using MediatR;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Modules.AI.Application;
using WoongBlog.Api.Modules.AI.Application.Abstractions;
using WoongBlog.Api.Modules.Content.Common.Application.Support;

namespace WoongBlog.Api.Modules.AI.Application.BatchJobs;

public sealed class ApplyBlogFixBatchJobCommandHandler(IAiBlogFixBatchStore store)
    : IRequestHandler<ApplyBlogFixBatchJobCommand, AiActionResult<BlogFixBatchJobDetailResponse>>
{
    public async Task<AiActionResult<BlogFixBatchJobDetailResponse>> Handle(
        ApplyBlogFixBatchJobCommand request,
        CancellationToken cancellationToken)
    {
        var job = await store.GetBlogJobAsync(request.JobId, cancellationToken);
        if (job is null)
        {
            return AiActionResult<BlogFixBatchJobDetailResponse>.NotFound();
        }

        var items = await store.GetSucceededUnappliedItemsAsync(request.JobId, request.JobItemIds, cancellationToken);
        var blogIds = items.Select(item => item.EntityId).ToArray();
        var blogs = await store.GetBlogsForUpdateAsync(blogIds, cancellationToken);
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

        await store.SaveChangesAsync(cancellationToken);
        await RefreshJobAggregatesAsync(store, job, cancellationToken);

        var refreshedItems = await store.GetJobItemsAsync(request.JobId, cancellationToken);
        return AiActionResult<BlogFixBatchJobDetailResponse>.Ok(
            AiBatchJobResponseMapper.ToJobDetailResponse(job, refreshedItems, includeHtml: true));
    }

    private static async Task RefreshJobAggregatesAsync(
        IAiBlogFixBatchStore store,
        AiBatchJob job,
        CancellationToken cancellationToken)
    {
        var items = await store.GetJobItemsAsync(job.Id, cancellationToken);
        AiBatchJobProgressPolicy.RefreshCounts(job, items, DateTimeOffset.UtcNow);
        await store.SaveChangesAsync(cancellationToken);
    }
}
