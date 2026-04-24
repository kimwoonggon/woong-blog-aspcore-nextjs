using MediatR;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Application.Modules.Content.Common.Support;
using WoongBlog.Application.Modules.Content.Blogs.Abstractions;

namespace WoongBlog.Application.Modules.Content.Blogs.CreateBlog;

public sealed class CreateBlogCommandHandler : IRequestHandler<CreateBlogCommand, AdminMutationResult>
{
    private readonly IBlogCommandStore _blogCommandStore;

    public CreateBlogCommandHandler(IBlogCommandStore blogCommandStore)
    {
        _blogCommandStore = blogCommandStore;
    }

    public async Task<AdminMutationResult> Handle(CreateBlogCommand request, CancellationToken cancellationToken)
    {
        var slug = await GenerateUniqueSlugAsync(request.Title, null, cancellationToken);
        var contentText = AdminContentJson.ExtractExcerptText(request.ContentJson);
        var excerpt = ResolveExcerpt(request.Excerpt, contentText);
        var now = DateTimeOffset.UtcNow;

        var blog = new Blog
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Slug = slug,
            Excerpt = excerpt,
            Tags = request.Tags,
            Published = request.Published,
            PublishedAt = request.Published ? now : null,
            ContentJson = request.ContentJson,
            SearchTitle = ContentSearchText.Normalize(request.Title),
            SearchText = ContentSearchText.BuildIndex(excerpt, contentText),
            CreatedAt = now,
            UpdatedAt = now
        };

        _blogCommandStore.Add(blog);
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
