using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Infrastructure.Persistence;
using WoongBlog.Api.Modules.Composition.Application.Abstractions;
using WoongBlog.Api.Modules.Composition.Application.GetDashboardSummary;

namespace WoongBlog.Api.Modules.Composition.Persistence;

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
