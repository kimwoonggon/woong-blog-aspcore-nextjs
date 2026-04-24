using System.Text.Json;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Application.Modules.Content.Common.Support;

namespace WoongBlog.Application.Modules.AI.BatchJobs;

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
