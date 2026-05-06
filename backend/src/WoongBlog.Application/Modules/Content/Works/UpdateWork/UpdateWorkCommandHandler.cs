using MediatR;
using WoongBlog.Application.Modules.Content.Common.Support;
using WoongBlog.Application.Modules.Content.Works.Abstractions;

namespace WoongBlog.Application.Modules.Content.Works.UpdateWork;

public sealed class UpdateWorkCommandHandler : IRequestHandler<UpdateWorkCommand, AdminMutationResult?>
{
    private readonly IWorkCommandStore _workCommandStore;

    public UpdateWorkCommandHandler(IWorkCommandStore workCommandStore)
    {
        _workCommandStore = workCommandStore;
    }

    public async Task<AdminMutationResult?> Handle(UpdateWorkCommand request, CancellationToken cancellationToken)
    {
        var work = await _workCommandStore.GetByIdForUpdateAsync(request.Id, cancellationToken);
        if (work is null)
        {
            return null;
        }

        var excerpt = AdminContentText.GenerateExcerpt(AdminContentJson.ExtractHtml(request.ContentJson));
        var now = DateTimeOffset.UtcNow;
        var assetPublicUrls = await _workCommandStore.GetAssetPublicUrlsAsync(
            GetPublicMediaAssetIds(request.ThumbnailAssetId, request.IconAssetId),
            cancellationToken);

        work.Title = request.Title;
        work.Slug = await GenerateUniqueSlugAsync(request.Title, work.Id, cancellationToken);
        work.Excerpt = excerpt;
        work.ThumbnailAssetId = request.ThumbnailAssetId;
        work.IconAssetId = request.IconAssetId;
        work.Category = request.Category;
        work.Period = request.Period;
        work.Tags = request.Tags;
        work.ContentJson = request.ContentJson;
        work.AllPropertiesJson = request.AllPropertiesJson;
        work.PublicThumbnailUrl = ResolvePublicMediaUrl(request.ThumbnailAssetId, assetPublicUrls);
        work.PublicIconUrl = ResolvePublicMediaUrl(request.IconAssetId, assetPublicUrls);
        work.SearchTitle = ContentSearchText.Normalize(request.Title);
        work.SearchText = ContentSearchText.BuildIndex(excerpt, AdminContentJson.ExtractExcerptText(request.ContentJson));
        work.UpdatedAt = now;
        work.Published = request.Published;
        if (request.Published && work.PublishedAt is null)
        {
            work.PublishedAt = now;
        }

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
