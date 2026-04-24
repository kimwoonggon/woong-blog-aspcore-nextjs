using WoongBlog.Api.Domain.Entities;
using WoongBlog.Application.Modules.AI;
using WoongBlog.Application.Modules.AI.Abstractions;
using WoongBlog.Application.Modules.Content.Common.Support;

namespace WoongBlog.Application.Modules.AI.BatchJobs;

public sealed class AiBatchJobItemProcessor(
    IAiBatchJobQueryStore jobQueryStore,
    IAiBatchTargetQueryStore targetStore,
    IAiBatchJobCommandStore commandStore,
    IBlogAiFixService aiFixService,
    IBlogFixApplyPolicy applyPolicy) : IAiBatchJobItemProcessor
{
    public async Task ProcessAsync(Guid jobId, Guid itemId, CancellationToken cancellationToken)
    {
        var item = await jobQueryStore.GetJobItemAsync(itemId, cancellationToken);
        if (item is null)
        {
            return;
        }

        var job = await jobQueryStore.GetBlogJobAsync(jobId, cancellationToken);
        if (job is null)
        {
            AiBatchJobProgressPolicy.MarkFailed(item, "Batch job no longer exists.", DateTimeOffset.UtcNow);
            await commandStore.SaveChangesAsync(cancellationToken);
            return;
        }

        var blogLookup = await targetStore.GetBlogsForUpdateAsync([item.EntityId], cancellationToken);
        if (!blogLookup.TryGetValue(item.EntityId, out var blog))
        {
            AiBatchJobProgressPolicy.MarkFailed(item, "Target blog no longer exists.", DateTimeOffset.UtcNow);
            await commandStore.SaveChangesAsync(cancellationToken);
            return;
        }

        AiBatchJobProgressPolicy.MarkRunning(item, DateTimeOffset.UtcNow);
        await commandStore.SaveChangesAsync(cancellationToken);

        try
        {
            var html = AdminContentJson.ExtractHtml(blog.ContentJson);
            var result = await aiFixService.FixHtmlAsync(html, cancellationToken, new AiFixRequestOptions(
                Mode: AiFixMode.BlogFix,
                Provider: job.Provider,
                CodexModel: job.Model,
                CodexReasoningEffort: job.ReasoningEffort,
                CustomPrompt: job.CustomPrompt));

            item.FixedHtml = result.FixedHtml;
            item.Provider = result.Provider;
            item.Model = result.Model;
            item.ReasoningEffort = result.ReasoningEffort;
            item.Error = null;

            if (job.AutoApply)
            {
                applyPolicy.Apply(blog, item, result.FixedHtml, DateTimeOffset.UtcNow);
            }
            else
            {
                item.Status = AiBatchJobItemStates.Succeeded;
            }
        }
        catch (Exception exception)
        {
            AiBatchJobProgressPolicy.MarkFailed(item, exception.Message, DateTimeOffset.UtcNow);
        }

        item.FinishedAt = DateTimeOffset.UtcNow;
        await commandStore.SaveChangesAsync(cancellationToken);
    }
}
