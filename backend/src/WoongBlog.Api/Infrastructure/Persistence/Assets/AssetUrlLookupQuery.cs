using Microsoft.EntityFrameworkCore;

namespace WoongBlog.Api.Infrastructure.Persistence.Assets;

internal static class AssetUrlLookupQuery
{
    public static Task<Dictionary<Guid, string>> LoadPublicUrlLookupAsync(
        this WoongBlogDbContext dbContext,
        IEnumerable<Guid?> assetIds,
        CancellationToken cancellationToken)
    {
        var resolvedIds = assetIds
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();

        if (resolvedIds.Length == 0)
        {
            return Task.FromResult(new Dictionary<Guid, string>());
        }

        return dbContext.Assets
            .AsNoTracking()
            .Where(asset => resolvedIds.Contains(asset.Id))
            .ToDictionaryAsync(asset => asset.Id, asset => asset.PublicUrl, cancellationToken);
    }
}
