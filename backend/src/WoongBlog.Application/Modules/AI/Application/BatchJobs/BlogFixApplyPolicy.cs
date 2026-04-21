using System.Text.Json;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Modules.Content.Common.Application.Support;

namespace WoongBlog.Api.Modules.AI.Application.BatchJobs;

public sealed class BlogFixApplyPolicy : IBlogFixApplyPolicy
{
    public void Apply(Blog blog, AiBatchJobItem item, string fixedHtml, DateTimeOffset timestamp)
    {
        blog.ContentJson = $$"""{"html":{{JsonSerializer.Serialize(fixedHtml)}}}""";
        blog.Excerpt = AdminContentText.GenerateExcerpt(fixedHtml);
        blog.UpdatedAt = timestamp;
        item.AppliedAt = timestamp;
        item.Status = AiBatchJobItemStates.Applied;
    }
}
