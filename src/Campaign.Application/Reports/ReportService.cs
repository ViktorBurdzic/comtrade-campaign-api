using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Campaign.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Campaign.Application.Reports;

public sealed class ReportService : IReportService
{
    private readonly IApplicationDbContext _db;

    public ReportService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<CampaignReportDto> GetCampaignResultsAsync(
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken ct = default)
    {
        var rewardsQuery = _db.Rewards.AsNoTracking();

        if (from is { } f) rewardsQuery = rewardsQuery.Where(r => r.RewardDate >= f);
        if (to is { } t) rewardsQuery = rewardsQuery.Where(r => r.RewardDate <= t);

        var rewards = await rewardsQuery
            .OrderBy(r => r.RewardDate)
            .ThenBy(r => r.AgentUsername)
            .ToListAsync(ct);

        // only pull purchases for customers that were actually rewarded
        var rewardedCustomerIds = rewards.Select(r => r.CustomerId).Distinct().ToList();

        var purchases = await _db.Purchases.AsNoTracking()
            .Where(p => rewardedCustomerIds.Contains(p.CustomerId))
            .ToListAsync(ct);

        var purchasesByCustomer = purchases
            .GroupBy(p => p.CustomerId)
            .ToDictionary(
                g => g.Key,
                g => (FirstDate: g.Min(p => p.PurchaseDate), Total: g.Sum(p => p.Amount)));

        var items = rewards.Select(r =>
        {
            var purchased = purchasesByCustomer.TryGetValue(r.CustomerId, out var p);
            return new CampaignResultItemDto(
                r.CustomerId,
                r.CustomerName,
                r.AgentUsername,
                r.RewardDate,
                r.DiscountPercent,
                HasPurchased: purchased,
                FirstPurchaseDate: purchased ? p.FirstDate : null,
                TotalPurchaseAmount: purchased ? p.Total : null);
        }).ToList();

        var distinctRewarded = rewardedCustomerIds.Count;
        var converted = rewardedCustomerIds.Count(purchasesByCustomer.ContainsKey);
        var conversion = distinctRewarded == 0
            ? 0m
            : Math.Round(100m * converted / distinctRewarded, 2);

        return new CampaignReportDto(
            TotalRewards: rewards.Count,
            DistinctRewardedCustomers: distinctRewarded,
            CustomersWhoPurchased: converted,
            ConversionRatePercent: conversion,
            Items: items);
    }
}
