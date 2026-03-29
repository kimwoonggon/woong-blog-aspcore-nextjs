using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Application.Admin.Abstractions;
using WoongBlog.Api.Application.Admin.GetAdminPages;
using WoongBlog.Api.Application.Admin.Support;
using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Api.Infrastructure.Persistence.Admin;

public sealed class AdminPageService : IAdminPageQueries, IAdminPageWriteStore
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

    public Task<PageEntity?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Pages.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
