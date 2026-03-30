using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Application.Admin.Abstractions;
using WoongBlog.Api.Application.Admin.GetDashboardSummary;

namespace WoongBlog.Api.Infrastructure.Persistence.Admin;

public sealed class AdminDashboardQueries : IAdminDashboardQueries
{
    private readonly WoongBlogDbContext _dbContext;

    public AdminDashboardQueries(WoongBlogDbContext dbContext)
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
