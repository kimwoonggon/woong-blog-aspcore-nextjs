using WoongBlog.Api.Application.Admin.Abstractions;

namespace WoongBlog.Api.Application.Admin.Support;

public sealed class AdminUniqueSlugService : IAdminUniqueSlugService
{
    private readonly IAdminWorkWriteStore _workWriteStore;
    private readonly IAdminBlogWriteStore _blogWriteStore;

    public AdminUniqueSlugService(IAdminWorkWriteStore workWriteStore, IAdminBlogWriteStore blogWriteStore)
    {
        _workWriteStore = workWriteStore;
        _blogWriteStore = blogWriteStore;
    }

    public Task<string> GenerateWorkSlugAsync(string title, Guid? excludingId, CancellationToken cancellationToken)
    {
        return GenerateUniqueSlugAsync(
            AdminContentText.Slugify(title, "work"),
            slug => _workWriteStore.SlugExistsAsync(slug, excludingId, cancellationToken));
    }

    public Task<string> GenerateBlogSlugAsync(string title, Guid? excludingId, CancellationToken cancellationToken)
    {
        return GenerateUniqueSlugAsync(
            AdminContentText.Slugify(title, "post"),
            slug => _blogWriteStore.SlugExistsAsync(slug, excludingId, cancellationToken));
    }

    private static async Task<string> GenerateUniqueSlugAsync(string baseSlug, Func<string, Task<bool>> slugExistsAsync)
    {
        var slug = baseSlug;
        var suffix = 2;

        while (await slugExistsAsync(slug))
        {
            slug = $"{baseSlug}-{suffix}";
            suffix += 1;
        }

        return slug;
    }
}
