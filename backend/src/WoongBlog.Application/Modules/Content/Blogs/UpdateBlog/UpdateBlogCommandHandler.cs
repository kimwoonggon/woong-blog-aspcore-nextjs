using MediatR;
using WoongBlog.Application.Modules.Content.Common.Support;
using WoongBlog.Application.Modules.Content.Blogs.Abstractions;

namespace WoongBlog.Application.Modules.Content.Blogs.UpdateBlog;

public sealed class UpdateBlogCommandHandler : IRequestHandler<UpdateBlogCommand, AdminMutationResult?>
{
    private readonly IBlogCommandStore _blogCommandStore;

    public UpdateBlogCommandHandler(IBlogCommandStore blogCommandStore)
    {
        _blogCommandStore = blogCommandStore;
    }

    public async Task<AdminMutationResult?> Handle(UpdateBlogCommand request, CancellationToken cancellationToken)
    {
        var blog = await _blogCommandStore.GetByIdForUpdateAsync(request.Id, cancellationToken);
        if (blog is null)
        {
            return null;
        }

        var contentText = AdminContentJson.ExtractExcerptText(request.ContentJson);
        var excerpt = ResolveExcerpt(request.Excerpt, contentText);
        var now = DateTimeOffset.UtcNow;

        blog.Title = request.Title;
        blog.Slug = await GenerateUniqueSlugAsync(request.Title, blog.Id, cancellationToken);
        blog.Excerpt = excerpt;
        blog.Tags = request.Tags;
        blog.ContentJson = request.ContentJson;
        blog.SearchTitle = ContentSearchText.Normalize(request.Title);
        blog.SearchText = ContentSearchText.BuildIndex(excerpt, contentText);
        blog.UpdatedAt = now;
        blog.Published = request.Published;
        if (request.Published && blog.PublishedAt is null)
        {
            blog.PublishedAt = now;
        }

        await _blogCommandStore.SaveChangesAsync(cancellationToken);
        return new AdminMutationResult(blog.Id, blog.Slug);
    }

    private static string ResolveExcerpt(string? manualExcerpt, string contentText)
    {
        var normalizedManualExcerpt = manualExcerpt?.Trim();
        return string.IsNullOrWhiteSpace(normalizedManualExcerpt)
            ? AdminContentText.GenerateExcerpt(contentText)
            : normalizedManualExcerpt;
    }

    private async Task<string> GenerateUniqueSlugAsync(string title, Guid? currentBlogId, CancellationToken cancellationToken)
    {
        var baseSlug = AdminContentText.Slugify(title, "post");
        var slug = baseSlug;
        var suffix = 2;

        while (await _blogCommandStore.SlugExistsAsync(slug, currentBlogId, cancellationToken))
        {
            slug = $"{baseSlug}-{suffix}";
            suffix += 1;
        }

        return slug;
    }
}
