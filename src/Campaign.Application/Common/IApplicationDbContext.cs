using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Campaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Campaign.Application.Common;

public interface IApplicationDbContext
{
    DbSet<RewardEntry> Rewards { get; }
    DbSet<PurchaseRecord> Purchases { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
