using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Infrastructure.Persistence;
using WoongBlog.Api.Modules.Composition.Application.Abstractions;
using WoongBlog.Api.Modules.Composition.Application.GetDashboardSummary;

namespace WoongBlog.Api.Modules.Composition.Persistence;

public sealed class AdminDashboardService : IAdminDashboardService
{
    private readonly WoongBlogDbContext _dbContext;

    public AdminDashboardService(WoongBlogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AdminDashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var worksCount = await _dbContext.Works.CountAsync(cancellationToken);
        var blogsCount = await _dbContext.Blogs.CountAsync(cancellationToken);
        var viewsCount = await _dbContext.PageViews.CountAsync(cancellationToken);

        return new AdminDashboardSummaryDto(worksCount, blogsCount, viewsCount);
    }
}
