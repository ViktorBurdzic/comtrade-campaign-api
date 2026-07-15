using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Campaign.Application.Common;
using Campaign.Application.Customers;
using Campaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Campaign.Application.Rewards;

public sealed class RewardService : IRewardService
{
    public const int DailyLimitPerAgent = 5;

    private readonly IApplicationDbContext _db;
    private readonly ICustomerDirectory _customers;

    public RewardService(IApplicationDbContext db, ICustomerDirectory customers)
    {
        _db = db;
        _customers = customers;
    }

    public async Task<RewardDto> CreateAsync(CreateRewardRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.AgentUsername))
            throw new BadRequestException("AgentUsername is required.");

        ValidateDiscount(request.DiscountPercent);

        var agent = request.AgentUsername.Trim();
        var rewardDate = request.RewardDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        // customer must exist in the external directory
        var customer = await _customers.FindPersonAsync(request.CustomerId, ct)
            ?? throw new NotFoundException(
                $"Customer with id {request.CustomerId} was not found in the customer directory.");

        // daily limit; soft-deleted rows are excluded by the global query filter
        var activeToday = await _db.Rewards
            .CountAsync(r => r.AgentUsername == agent && r.RewardDate == rewardDate, ct);

        if (activeToday >= DailyLimitPerAgent)
            throw new DailyLimitExceededException(agent, rewardDate, DailyLimitPerAgent);

        // no duplicate customer per agent per day (also guarded by a unique index)
        var alreadyRewarded = await _db.Rewards.AnyAsync(
            r => r.AgentUsername == agent &&
                 r.CustomerId == request.CustomerId &&
                 r.RewardDate == rewardDate,
            ct);

        if (alreadyRewarded)
            throw new ConflictException(
                $"Customer {request.CustomerId} was already rewarded by agent '{agent}' on {rewardDate:yyyy-MM-dd}.");

        var entity = new RewardEntry
        {
            Id = Guid.NewGuid(),
            AgentUsername = agent,
            CustomerId = request.CustomerId,
            CustomerName = customer.Name,
            RewardDate = rewardDate,
            DiscountPercent = request.DiscountPercent,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Rewards.Add(entity);
        await _db.SaveChangesAsync(ct);

        return ToDto(entity);
    }

    public async Task<RewardDto> UpdateAsync(Guid id, UpdateRewardRequest request, CancellationToken ct = default)
    {
        var entity = await _db.Rewards.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException($"Reward entry {id} was not found.");

        if (request.DiscountPercent is { } discount)
        {
            ValidateDiscount(discount);
            entity.DiscountPercent = discount;
        }

        if (request.CustomerId is { } newCustomerId && newCustomerId != entity.CustomerId)
        {
            var customer = await _customers.FindPersonAsync(newCustomerId, ct)
                ?? throw new NotFoundException(
                    $"Customer with id {newCustomerId} was not found in the customer directory.");

            var duplicate = await _db.Rewards.AnyAsync(
                r => r.Id != id &&
                     r.AgentUsername == entity.AgentUsername &&
                     r.CustomerId == newCustomerId &&
                     r.RewardDate == entity.RewardDate,
                ct);

            if (duplicate)
                throw new ConflictException(
                    $"Customer {newCustomerId} was already rewarded by agent '{entity.AgentUsername}' " +
                    $"on {entity.RewardDate:yyyy-MM-dd}.");

            entity.CustomerId = newCustomerId;
            entity.CustomerName = customer.Name;
        }

        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return ToDto(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.Rewards.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException($"Reward entry {id} was not found.");

        // soft delete keeps the audit trail and frees the agent's daily slot
        entity.IsDeleted = true;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    public async Task<RewardDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.Rewards.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException($"Reward entry {id} was not found.");

        return ToDto(entity);
    }

    public async Task<IReadOnlyList<RewardDto>> ListAsync(
        string? agentUsername = null,
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken ct = default)
    {
        var query = _db.Rewards.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(agentUsername))
            query = query.Where(r => r.AgentUsername == agentUsername.Trim());

        if (from is { } f)
            query = query.Where(r => r.RewardDate >= f);

        if (to is { } t)
            query = query.Where(r => r.RewardDate <= t);

        var items = await query
            .OrderByDescending(r => r.RewardDate)
            .ThenBy(r => r.AgentUsername)
            .ToListAsync(ct);

        return items.Select(ToDto).ToList();
    }

    private static void ValidateDiscount(decimal discountPercent)
    {
        if (discountPercent is <= 0 or > 100)
            throw new BadRequestException("DiscountPercent must be greater than 0 and at most 100.");
    }

    private static RewardDto ToDto(RewardEntry e) => new(
        e.Id, e.AgentUsername, e.CustomerId, e.CustomerName,
        e.RewardDate, e.DiscountPercent, e.CreatedAtUtc, e.UpdatedAtUtc);
}
