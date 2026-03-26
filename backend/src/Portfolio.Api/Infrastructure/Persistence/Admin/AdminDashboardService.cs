using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Application.Admin.Abstractions;
using Portfolio.Api.Application.Admin.GetDashboardSummary;

namespace Portfolio.Api.Infrastructure.Persistence.Admin;

public sealed class AdminDashboardService : IAdminDashboardService
{
    private readonly PortfolioDbContext _dbContext;

    public AdminDashboardService(PortfolioDbContext dbContext)
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
