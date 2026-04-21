using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Infrastructure.Persistence;
using WoongBlog.Api.Modules.Media.Application.Abstractions;

namespace WoongBlog.Api.Modules.Media.Persistence;

public sealed class MediaAssetCommandStore(WoongBlogDbContext dbContext) : IMediaAssetCommandStore
{
    private readonly WoongBlogDbContext _dbContext = dbContext;

    public Task<Asset?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Assets.SingleOrDefaultAsync(asset => asset.Id == id, cancellationToken);
    }

    public void Add(Asset asset)
    {
        _dbContext.Assets.Add(asset);
    }

    public void Remove(Asset asset)
    {
        _dbContext.Assets.Remove(asset);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
