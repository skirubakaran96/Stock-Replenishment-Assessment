using Microsoft.EntityFrameworkCore;
using StockReplenishment.Api.Domain;

namespace StockReplenishment.Api.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ReplenishmentRequest> ReplenishmentRequests => Set<ReplenishmentRequest>();
    public DbSet<ReplenishmentItem> ReplenishmentItems => Set<ReplenishmentItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReplenishmentRequest>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Location).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Priority).HasConversion<string>().IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().IsRequired();
            entity.Property(x => x.StockValidationStatus).HasConversion<string>().IsRequired();
            entity.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.ReplenishmentRequestId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ReplenishmentItem>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ArticleNumber).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(250).IsRequired();
            entity.Property(x => x.RequestedQuantity).HasPrecision(18, 2);
            entity.Property(x => x.FulfilledQuantity).HasPrecision(18, 2);
        });
    }
}
