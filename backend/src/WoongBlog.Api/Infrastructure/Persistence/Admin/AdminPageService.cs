using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Application.Admin.Abstractions;
using WoongBlog.Api.Application.Admin.GetAdminPages;
using WoongBlog.Api.Application.Admin.Support;

namespace WoongBlog.Api.Infrastructure.Persistence.Admin;

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

        return await query
            .OrderBy(x => x.Slug)
            .Select(x => new AdminPageListItemDto(
                x.Id,
                x.Slug,
                x.Title,
                new AdminPageHtmlDto(AdminContentJson.ExtractHtml(x.ContentJson))
            ))
            .ToListAsync(cancellationToken);
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
