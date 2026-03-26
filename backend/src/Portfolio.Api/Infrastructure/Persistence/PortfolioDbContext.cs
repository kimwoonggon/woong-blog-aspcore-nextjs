using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Domain.Entities;

namespace Portfolio.Api.Infrastructure.Persistence;

public class PortfolioDbContext : DbContext
{
    public PortfolioDbContext(DbContextOptions<PortfolioDbContext> options) : base(options)
    {
    }

    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<AuthAuditLog> AuthAuditLogs => Set<AuthAuditLog>();
    public DbSet<AuthSession> AuthSessions => Set<AuthSession>();
    public DbSet<Blog> Blogs => Set<Blog>();
    public DbSet<PageEntity> Pages => Set<PageEntity>();
    public DbSet<PageView> PageViews => Set<PageView>();
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
    public DbSet<Work> Works => Set<Work>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SiteSetting>(entity =>
        {
            entity.HasKey(x => x.Singleton);
            entity.Property(x => x.Singleton).ValueGeneratedNever();
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
            entity.Property(x => x.ContentJson).HasColumnType("jsonb");
            entity.Property(x => x.AllPropertiesJson).HasColumnType("jsonb");
        });

        modelBuilder.Entity<Blog>(entity =>
        {
            entity.HasIndex(x => x.Slug).IsUnique();
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
