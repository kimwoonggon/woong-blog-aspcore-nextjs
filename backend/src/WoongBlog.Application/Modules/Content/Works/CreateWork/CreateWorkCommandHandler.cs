using MediatR;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Application.Modules.Content.Common.Support;
using WoongBlog.Application.Modules.Content.Works.Abstractions;
using WoongBlog.Application.Modules.Content.Works.Support;

namespace WoongBlog.Application.Modules.Content.Works.CreateWork;

public sealed class CreateWorkCommandHandler : IRequestHandler<CreateWorkCommand, AdminMutationResult>
{
    private readonly IWorkCommandStore _workCommandStore;

    public CreateWorkCommandHandler(IWorkCommandStore workCommandStore)
    {
        _workCommandStore = workCommandStore;
    }

    public async Task<AdminMutationResult> Handle(CreateWorkCommand request, CancellationToken cancellationToken)
    {
        var slug = await GenerateUniqueSlugAsync(request.Title, null, cancellationToken);
        var excerpt = AdminContentText.GenerateExcerpt(AdminContentJson.ExtractHtml(request.ContentJson));
        var now = DateTimeOffset.UtcNow;
        var assetPublicUrls = await _workCommandStore.GetAssetPublicUrlsAsync(
            GetPublicMediaAssetIds(request.ThumbnailAssetId, request.IconAssetId),
            cancellationToken);

        var work = new Work
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Slug = slug,
            Excerpt = excerpt,
            ThumbnailAssetId = request.ThumbnailAssetId,
            IconAssetId = request.IconAssetId,
            Category = request.Category,
            Period = request.Period,
            Tags = request.Tags,
            Published = request.Published,
            PublishedAt = request.Published ? now : null,
            ContentJson = request.ContentJson,
            AllPropertiesJson = request.AllPropertiesJson,
            PublicIconUrl = ResolvePublicMediaUrl(request.IconAssetId, assetPublicUrls),
            SearchTitle = ContentSearchText.Normalize(request.Title),
            SearchText = ContentSearchText.BuildIndex(excerpt, AdminContentJson.ExtractExcerptText(request.ContentJson)),
            CreatedAt = now,
            UpdatedAt = now
        };

        WorkPublicThumbnailReadModel.Refresh(work, Array.Empty<WorkVideo>(), assetPublicUrls);
        _workCommandStore.Add(work);
        await _workCommandStore.SaveChangesAsync(cancellationToken);
        return new AdminMutationResult(work.Id, work.Slug);
    }

    private static Guid[] GetPublicMediaAssetIds(Guid? thumbnailAssetId, Guid? iconAssetId)
    {
        return new[] { thumbnailAssetId, iconAssetId }
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();
    }

    private static string ResolvePublicMediaUrl(Guid? assetId, IReadOnlyDictionary<Guid, string> assetPublicUrls)
    {
        return assetId is not null && assetPublicUrls.TryGetValue(assetId.Value, out var publicUrl)
            ? publicUrl
            : string.Empty;
    }

    private async Task<string> GenerateUniqueSlugAsync(string title, Guid? currentWorkId, CancellationToken cancellationToken)
    {
        var baseSlug = AdminContentText.Slugify(title, "work");
        var slug = baseSlug;
        var suffix = 2;

        while (await _workCommandStore.SlugExistsAsync(slug, currentWorkId, cancellationToken))
        {
            slug = $"{baseSlug}-{suffix}";
            suffix += 1;
        }

        return slug;
    }
}
