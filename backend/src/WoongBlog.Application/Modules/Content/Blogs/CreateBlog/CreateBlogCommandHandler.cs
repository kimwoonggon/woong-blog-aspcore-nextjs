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
        var excerpt = ResolveExcerpt(request.Excerpt);
        var contentText = AdminContentJson.ExtractExcerptText(request.ContentJson);
        var now = DateTimeOffset.UtcNow;
        var assetPublicUrls = await _blogCommandStore.GetAssetPublicUrlsAsync(
            GetPublicMediaAssetIds(request.CoverAssetId),
            cancellationToken);

        var blog = new Blog
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Slug = slug,
            Excerpt = excerpt,
            CoverAssetId = request.CoverAssetId,
            PublicCoverUrl = ResolvePublicMediaUrl(request.CoverAssetId, assetPublicUrls),
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

    private static string ResolveExcerpt(string? manualExcerpt)
    {
        var normalizedManualExcerpt = manualExcerpt?.Trim();
        return string.IsNullOrWhiteSpace(normalizedManualExcerpt) ? string.Empty : normalizedManualExcerpt;
    }

    private static Guid[] GetPublicMediaAssetIds(Guid? coverAssetId)
    {
        return coverAssetId.HasValue ? [coverAssetId.Value] : [];
    }

    private static string ResolvePublicMediaUrl(Guid? assetId, IReadOnlyDictionary<Guid, string> assetPublicUrls)
    {
        return assetId is not null && assetPublicUrls.TryGetValue(assetId.Value, out var publicUrl)
            ? publicUrl
            : string.Empty;
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
