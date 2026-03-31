using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Infrastructure.Persistence;
using WoongBlog.Api.Modules.Content.Pages.Application.Abstractions;
using WoongBlog.Api.Modules.Content.Pages.Application.GetPageBySlug;

namespace WoongBlog.Api.Modules.Content.Pages.Persistence;

public sealed class PublicPageService : IPublicPageService
{
    private readonly WoongBlogDbContext _dbContext;

    public PublicPageService(WoongBlogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PageDto?> GetPageBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var page = await _dbContext.Pages
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Slug == slug, cancellationToken);

        return page is null
            ? null
            : new PageDto(page.Id, page.Slug, page.Title, page.ContentJson);
    }
}
