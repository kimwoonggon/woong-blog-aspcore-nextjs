using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Infrastructure.Persistence;

public class WoongBlogDbContext : DbContext
{
    public WoongBlogDbContext(DbContextOptions<WoongBlogDbContext> options) : base(options)
    {
    }

    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<AiBatchJob> AiBatchJobs => Set<AiBatchJob>();
    public DbSet<AiBatchJobItem> AiBatchJobItems => Set<AiBatchJobItem>();
    public DbSet<AuthAuditLog> AuthAuditLogs => Set<AuthAuditLog>();
    public DbSet<AuthSession> AuthSessions => Set<AuthSession>();
    public DbSet<Blog> Blogs => Set<Blog>();
    public DbSet<PageEntity> Pages => Set<PageEntity>();
    public DbSet<PageView> PageViews => Set<PageView>();
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<SchemaPatch> SchemaPatches => Set<SchemaPatch>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
    public DbSet<VideoStorageCleanupJob> VideoStorageCleanupJobs => Set<VideoStorageCleanupJob>();
    public DbSet<Work> Works => Set<Work>();
    public DbSet<WorkVideo> WorkVideos => Set<WorkVideo>();
    public DbSet<WorkVideoUploadSession> WorkVideoUploadSessions => Set<WorkVideoUploadSession>();

    public override int SaveChanges()
    {
        ContentSearchFieldSynchronizer.Apply(ChangeTracker);
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ContentSearchFieldSynchronizer.Apply(ChangeTracker);
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SiteSetting>(entity =>
        {
            entity.HasKey(x => x.Singleton);
            entity.Property(x => x.Singleton).ValueGeneratedNever();
        });

        modelBuilder.Entity<SchemaPatch>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<AiBatchJob>(entity =>
        {
            entity.HasIndex(x => new { x.TargetType, x.Status, x.CreatedAt });
            entity.HasIndex(x => new { x.TargetType, x.SelectionKey, x.Status });
        });

        modelBuilder.Entity<AiBatchJobItem>(entity =>
        {
            entity.HasIndex(x => new { x.JobId, x.EntityId }).IsUnique();
            entity.HasIndex(x => new { x.JobId, x.Status });
        });

        modelBuilder.Entity<PageEntity>(entity =>
        {
            entity.ToTable("Pages");
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.Property(x => x.ContentJson).HasColumnType("jsonb");
        });

        modelBuilder.Entity<Work>(entity =>
        {
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.HasIndex(x => new { x.Published, x.PublishedAt });
            entity.HasIndex(x => x.SearchTitle);
            entity.Property(x => x.ContentJson).HasColumnType("jsonb");
            entity.Property(x => x.AllPropertiesJson).HasColumnType("jsonb");
        });

        modelBuilder.Entity<WorkVideo>(entity =>
        {
            entity.HasIndex(x => new { x.WorkId, x.SortOrder }).IsUnique();
            entity.HasOne<Work>()
                .WithMany()
                .HasForeignKey(x => x.WorkId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkVideoUploadSession>(entity =>
        {
            entity.HasIndex(x => x.WorkId);
            entity.HasIndex(x => x.ExpiresAt);
            entity.HasOne<Work>()
                .WithMany()
                .HasForeignKey(x => x.WorkId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VideoStorageCleanupJob>(entity =>
        {
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.CreatedAt);
        });

        modelBuilder.Entity<Blog>(entity =>
        {
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.HasIndex(x => new { x.Published, x.PublishedAt });
            entity.HasIndex(x => x.SearchTitle);
            entity.Property(x => x.ContentJson).HasColumnType("jsonb");
        });

        modelBuilder.Entity<PageView>(entity =>
        {
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<AuthAuditLog>(entity =>
        {
            entity.HasIndex(x => x.CreatedAt);
            entity.HasIndex(x => x.EventType);
        });

        modelBuilder.Entity<AuthSession>(entity =>
        {
            entity.HasIndex(x => x.ProfileId);
            entity.HasIndex(x => x.SessionKey).IsUnique();
        });

        modelBuilder.Entity<Profile>(entity =>
        {
            entity.HasIndex(x => x.Email);
            entity.HasIndex(x => new { x.Provider, x.ProviderSubject }).IsUnique();
        });
    }

}
