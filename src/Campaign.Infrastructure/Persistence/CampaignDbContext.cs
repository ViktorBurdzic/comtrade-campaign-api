using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Campaign.Application.Common;
using Campaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Campaign.Infrastructure.Persistence;

public class CampaignDbContext : DbContext, IApplicationDbContext
{
    public CampaignDbContext(DbContextOptions<CampaignDbContext> options) : base(options)
    {
    }

    public DbSet<RewardEntry> Rewards => Set<RewardEntry>();
    public DbSet<PurchaseRecord> Purchases => Set<PurchaseRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RewardEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.AgentUsername).HasMaxLength(100).IsRequired();
            e.Property(x => x.CustomerName).HasMaxLength(200).IsRequired();
            e.Property(x => x.DiscountPercent).HasPrecision(5, 2);

            e.HasIndex(x => new { x.AgentUsername, x.RewardDate });

            e.HasIndex(x => new { x.AgentUsername, x.CustomerId, x.RewardDate })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            e.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<PurchaseRecord>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            e.Property(x => x.SourceFileName).HasMaxLength(260);
            e.HasIndex(x => x.CustomerId);
        });
    }
}