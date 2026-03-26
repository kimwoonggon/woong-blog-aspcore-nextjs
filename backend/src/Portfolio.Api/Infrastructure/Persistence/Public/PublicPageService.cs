using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Application.Public.Abstractions;
using Portfolio.Api.Application.Public.GetPageBySlug;

namespace Portfolio.Api.Infrastructure.Persistence.Public;

public sealed class PublicPageService : IPublicPageService
{
    private readonly PortfolioDbContext _dbContext;

    public PublicPageService(PortfolioDbContext dbContext)
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
