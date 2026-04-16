using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Modules.Content.Common.Application.Support;
using WoongBlog.Api.Infrastructure.Persistence;
using WoongBlog.Api.Modules.Content.Pages.Application.Abstractions;
using WoongBlog.Api.Modules.Content.Pages.Application.GetAdminPages;

namespace WoongBlog.Api.Modules.Content.Pages.Persistence;

public sealed class AdminPageService : IAdminPageService
{
    private readonly WoongBlogDbContext _dbContext;

    public AdminPageService(WoongBlogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AdminPageListItemDto>> GetPagesAsync(string[]? slugs, CancellationToken cancellationToken)
    {
        var query = _dbContext.Pages.AsNoTracking();

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
                    : new AdminPageHtmlDto(AdminContentJson.ExtractHtml(x.ContentJson))
            ))
            .ToList();
    }

    public async Task<AdminActionResult> UpdatePageAsync(Guid id, string title, string contentJson, CancellationToken cancellationToken)
    {
        var page = await _dbContext.Pages.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (page is null)
        {
            return new AdminActionResult(false);
        }

        page.Title = title;
        page.ContentJson = contentJson;
        page.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new AdminActionResult(true);
    }
}
