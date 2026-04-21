using System.Text.Json;
using MediatR;
using WoongBlog.Api.Modules.AI.Application;
using WoongBlog.Api.Modules.AI.Application.Abstractions;
using WoongBlog.Api.Modules.Content.Common.Application.Support;

namespace WoongBlog.Api.Modules.AI.Application.BlogFix;

public sealed class FixBlogBatchCommandHandler(
    IAiBlogFixBatchStore store,
    IBlogAiFixService aiFixService) : IRequestHandler<FixBlogBatchCommand, AiActionResult<BlogFixBatchResponse>>
{
    public async Task<AiActionResult<BlogFixBatchResponse>> Handle(FixBlogBatchCommand request, CancellationToken cancellationToken)
    {
        if (!request.All && (request.BlogIds is null || request.BlogIds.Count == 0))
        {
            return AiActionResult<BlogFixBatchResponse>.BadRequest("Either blogIds or all=true is required.");
        }

        var blogs = await store.GetBlogsForFixAsync(request.All, request.BlogIds, cancellationToken);
        var results = new List<BlogFixBatchItemResponse>(blogs.Count);
        foreach (var blog in blogs)
        {
            var html = AdminContentJson.ExtractHtml(blog.ContentJson);

            try
            {
                var aiResult = await aiFixService.FixHtmlAsync(html, cancellationToken, new AiFixRequestOptions(
                    Mode: AiFixMode.BlogFix,
                    Provider: request.Provider,
                    CodexModel: request.CodexModel,
                    CodexReasoningEffort: request.CodexReasoningEffort,
                    CustomPrompt: request.CustomPrompt));

                if (request.Apply)
                {
                    blog.ContentJson = $$"""{"html":{{JsonSerializer.Serialize(aiResult.FixedHtml)}}}""";
                    blog.Excerpt = AdminContentText.GenerateExcerpt(aiResult.FixedHtml);
                    blog.UpdatedAt = DateTimeOffset.UtcNow;
                }

                results.Add(new BlogFixBatchItemResponse(
                    blog.Id,
                    blog.Title,
                    "fixed",
                    aiResult.FixedHtml,
                    null,
                    aiResult.Provider,
                    aiResult.Model,
                    aiResult.ReasoningEffort));
            }
            catch (Exception exception)
            {
                results.Add(new BlogFixBatchItemResponse(
                    blog.Id,
                    blog.Title,
                    "failed",
                    null,
                    exception.Message,
                    null,
                    null,
                    null));
            }
        }

        if (request.Apply)
        {
            await store.SaveChangesAsync(cancellationToken);
        }

        return AiActionResult<BlogFixBatchResponse>.Ok(new BlogFixBatchResponse(results, request.Apply));
    }
}
