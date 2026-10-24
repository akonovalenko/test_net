using MarketRateInfo.Server.DataAccess.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MarketRateInfo.Server.DataAccess;

/// <summary>
/// Represents the database context for the MarketRateInfo application, which manages
/// currency pairs, market rates, and user authentication and authorization.
/// </summary>
/// <param name="options">The options for configuring the database context.</param>
public sealed class MarketRateDbContext(DbContextOptions<MarketRateDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<CurrencyPair> CurrencyPairs => Set<CurrencyPair>();

    public DbSet<MarketRate> MarketRates => Set<MarketRate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CurrencyPair>(entity =>
        {
            entity.Property(x => x.BaseCurrency)
                .HasMaxLength(3)
                .IsRequired();

            entity.Property(x => x.QuoteCurrency)
                .HasMaxLength(3)
                .IsRequired();

            entity.HasIndex(x => new
            {
                x.BaseCurrency,
                x.QuoteCurrency
            })
            .IsUnique();
        });

        modelBuilder.Entity<MarketRate>(entity =>
        {
            entity.Property(x => x.Rate)
                .HasPrecision(18, 8);

            entity.HasOne(x => x.CurrencyPair)
                .WithMany(x => x.MarketRates)
                .HasForeignKey(x => x.CurrencyPairId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => new
            {
                x.CurrencyPairId,
                x.UpdatedAtUtc
            });
        });
    }
}