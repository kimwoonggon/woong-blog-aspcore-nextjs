using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Application.Modules.Content.Common.Support;

namespace WoongBlog.Infrastructure.Persistence;

internal static class ContentSearchFieldSynchronizer
{
    public static void Apply(ChangeTracker changeTracker)
    {
        foreach (var entry in changeTracker.Entries<Blog>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.SearchTitle = ContentSearchText.Normalize(entry.Entity.Title);
                entry.Entity.SearchText = ContentSearchText.BuildIndex(
                    entry.Entity.Excerpt,
                    AdminContentJson.ExtractExcerptText(entry.Entity.ContentJson));
            }
        }

        foreach (var entry in changeTracker.Entries<Work>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.SearchTitle = ContentSearchText.Normalize(entry.Entity.Title);
                entry.Entity.SearchText = ContentSearchText.BuildIndex(
                    entry.Entity.Excerpt,
                    AdminContentJson.ExtractExcerptText(entry.Entity.ContentJson));
            }
        }
    }
}
