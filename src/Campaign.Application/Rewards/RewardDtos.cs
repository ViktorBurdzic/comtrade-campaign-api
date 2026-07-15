using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Campaign.Application.Rewards;

public sealed record CreateRewardRequest(
    string AgentUsername,
    int CustomerId,
    decimal DiscountPercent,
    DateOnly? RewardDate = null);

// both fields optional - only what's supplied gets changed
public sealed record UpdateRewardRequest(
    int? CustomerId = null,
    decimal? DiscountPercent = null
    );

public sealed record RewardDto(
    Guid Id,
    string AgentUsername,
    int CustomerId,
    string CustomerName,
    DateOnly RewardDate,
    decimal DiscountPercent,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
    );
