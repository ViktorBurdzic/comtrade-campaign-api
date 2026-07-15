using Campaign.Application.Common;
using Campaign.Application.Customers;
using Campaign.Application.Rewards;
using Campaign.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Campaign.Tests;

public sealed class RewardServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly CampaignDbContext _db;
    private readonly RewardService _service;

    private static readonly DateOnly Day = new(2026, 8, 3);

    public RewardServiceTests()
    {
        // in-memory sqlite lives as long as the connection stays open
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<CampaignDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new CampaignDbContext(options);
        _db.Database.EnsureCreated();

        _service = new RewardService(_db, new FakeCustomerDirectory());
    }

    [Fact]
    public async Task Create_persists_reward_and_returns_dto()
    {
        var dto = await _service.CreateAsync(
            new CreateRewardRequest("viktor", CustomerId: 1, DiscountPercent: 15m, RewardDate: Day));

        Assert.Equal("viktor", dto.AgentUsername);
        Assert.Equal(1, dto.CustomerId);
        Assert.Equal("Customer 1", dto.CustomerName);
        Assert.Equal(15m, dto.DiscountPercent);
        Assert.Equal(1, await _db.Rewards.CountAsync());
    }

    [Fact]
    public async Task Create_rejects_sixth_reward_on_same_day_for_same_agent()
    {
        for (var i = 1; i <= RewardService.DailyLimitPerAgent; i++)
        {
            await _service.CreateAsync(new CreateRewardRequest("viktor", i, 10m, Day));
        }

        await Assert.ThrowsAsync<DailyLimitExceededException>(() =>
            _service.CreateAsync(new CreateRewardRequest("viktor", 99, 10m, Day)));
    }

    [Fact]
    public async Task Create_allows_new_reward_after_a_mistake_is_soft_deleted()
    {
        RewardDto last = null!;
        for (var i = 1; i <= RewardService.DailyLimitPerAgent; i++)
        {
            last = await _service.CreateAsync(new CreateRewardRequest("viktor", i, 10m, Day));
        }

        await _service.DeleteAsync(last.Id);

        var replacement = await _service.CreateAsync(new CreateRewardRequest("viktor", 42, 20m, Day));
        Assert.Equal(42, replacement.CustomerId);
    }

    [Fact]
    public async Task Create_rejects_customer_unknown_to_the_directory()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.CreateAsync(new CreateRewardRequest("viktor", 5000, 10m, Day)));
    }

    [Fact]
    public async Task Create_rejects_duplicate_customer_for_same_agent_and_day()
    {
        await _service.CreateAsync(new CreateRewardRequest("viktor", 7, 10m, Day));

        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.CreateAsync(new CreateRewardRequest("viktor", 7, 25m, Day)));
    }

    [Fact]
    public async Task Different_agents_have_independent_daily_limits()
    {
        for (var i = 1; i <= RewardService.DailyLimitPerAgent; i++)
        {
            await _service.CreateAsync(new CreateRewardRequest("viktor", i, 10m, Day));
        }

        var dto = await _service.CreateAsync(new CreateRewardRequest("jovica", 1, 10m, Day));
        Assert.Equal("jovica", dto.AgentUsername);
    }

    [Fact]
    public async Task Update_can_fix_wrong_customer_and_discount()
    {
        var created = await _service.CreateAsync(new CreateRewardRequest("viktor", 1, 10m, Day));

        var updated = await _service.UpdateAsync(created.Id,
            new UpdateRewardRequest(CustomerId: 2, DiscountPercent: 30m));

        Assert.Equal(2, updated.CustomerId);
        Assert.Equal("Customer 2", updated.CustomerName);
        Assert.Equal(30m, updated.DiscountPercent);
        Assert.NotNull(updated.UpdatedAtUtc);
    }

    [Fact]
    public async Task Create_rejects_invalid_discount()
    {
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.CreateAsync(new CreateRewardRequest("viktor", 1, 0m, Day)));

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.CreateAsync(new CreateRewardRequest("viktor", 1, 101m, Day)));
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    // ids below 1000 exist
    private sealed class FakeCustomerDirectory : ICustomerDirectory
    {
        public Task<CustomerDto?> FindPersonAsync(int id, CancellationToken ct = default) =>
            Task.FromResult<CustomerDto?>(
                id < 1000
                    ? new CustomerDto(id, $"Customer {id}", null, null, null, null)
                    : null);
    }
}
