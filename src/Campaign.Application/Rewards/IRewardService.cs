using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Campaign.Application.Rewards;

public interface IRewardService
{
    Task<RewardDto> CreateAsync(CreateRewardRequest request, CancellationToken ct = default);
    Task<RewardDto> UpdateAsync(Guid id, UpdateRewardRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<RewardDto> GetAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<RewardDto>> ListAsync(
        string? agentUsername = null,
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken ct = default);
}
