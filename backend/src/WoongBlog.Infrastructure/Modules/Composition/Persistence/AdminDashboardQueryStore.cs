using Microsoft.EntityFrameworkCore;
using WoongBlog.Infrastructure.Persistence;
using WoongBlog.Application.Modules.Composition.Abstractions;
using WoongBlog.Application.Modules.Composition.GetDashboardSummary;

namespace WoongBlog.Infrastructure.Modules.Composition.Persistence;

public sealed class AdminDashboardQueryStore(WoongBlogDbContext dbContext) : IAdminDashboardQueryStore
{
    public async Task<AdminDashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var worksCount = await dbContext.Works.CountAsync(cancellationToken);
        var blogsCount = await dbContext.Blogs.CountAsync(cancellationToken);
        var viewsCount = await dbContext.PageViews.CountAsync(cancellationToken);

        return new AdminDashboardSummaryDto(worksCount, blogsCount, viewsCount);
    }
}
