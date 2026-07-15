using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Campaign.Application.Reports;

public sealed record CampaignResultItemDto(
    int CustomerId,
    string CustomerName,
    string AgentUsername,
    DateOnly RewardDate,
    decimal DiscountPercent,
    bool HasPurchased,
    DateOnly? FirstPurchaseDate,
    decimal? TotalPurchaseAmount);

public sealed record CampaignReportDto(
    int TotalRewards,
    int DistinctRewardedCustomers,
    int CustomersWhoPurchased,
    decimal ConversionRatePercent,
    IReadOnlyList<CampaignResultItemDto> Items
    );
