using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Application.Public.Abstractions;
using WoongBlog.Api.Application.Public.GetPageBySlug;

namespace WoongBlog.Api.Infrastructure.Persistence.Public;

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
