using Microsoft.EntityFrameworkCore;
using WoongBlog.Infrastructure.Persistence;
using WoongBlog.Application.Modules.Content.Common.Support;
using WoongBlog.Application.Modules.Content.Pages.Abstractions;
using WoongBlog.Application.Modules.Content.Pages.GetAdminPages;
using WoongBlog.Application.Modules.Content.Pages.GetPageBySlug;

namespace WoongBlog.Infrastructure.Modules.Content.Pages.Persistence;

public sealed class PageQueryStore(WoongBlogDbContext dbContext) : IPageQueryStore
{
    public async Task<IReadOnlyList<AdminPageListItemDto>> GetAdminPagesAsync(string[]? slugs, CancellationToken cancellationToken)
    {
        var query = dbContext.Pages.AsNoTracking();

        if (slugs is { Length: > 0 })
        {
            query = query.Where(x => slugs.Contains(x.Slug));
        }

        var pages = await query
            .OrderBy(x => x.Slug)
            .ToListAsync(cancellationToken);

        return pages
            .Select(x => new AdminPageListItemDto(
                x.Id,
                x.Slug,
                x.Title,
                string.Equals(x.Slug, "home", StringComparison.OrdinalIgnoreCase)
                    ? AdminContentJson.ParseObject(x.ContentJson)
                    : new AdminPageHtmlDto(AdminContentJson.ExtractHtml(x.ContentJson))))
            .ToList();
    }

    public async Task<PageDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var page = await dbContext.Pages
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Slug == slug, cancellationToken);

        return page is null
            ? null
            : new PageDto(page.Id, page.Slug, page.Title, page.ContentJson);
    }
}
