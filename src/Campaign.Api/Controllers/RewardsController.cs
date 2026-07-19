using Campaign.Application.Rewards;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;



namespace Campaign.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/rewards")]
public sealed class RewardsController : ControllerBase
{
    private readonly IRewardService _rewards;

    public RewardsController(IRewardService rewards)
    {
        _rewards = rewards;
    }

    [HttpPost]
    [ProducesResponseType(typeof(RewardDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RewardDto>> Create([FromBody] CreateRewardRequest request, CancellationToken ct)
    {
        var created = await _rewards.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RewardDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RewardDto>>> List(
        [FromQuery] string? agentUsername,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
    {
        return Ok(await _rewards.ListAsync(agentUsername, from, to, ct));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RewardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RewardDto>> GetById(Guid id, CancellationToken ct)
    {
        return Ok(await _rewards.GetAsync(id, ct));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(RewardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RewardDto>> Update(Guid id, [FromBody] UpdateRewardRequest request, CancellationToken ct)
    {
        return Ok(await _rewards.UpdateAsync(id, request, ct));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _rewards.DeleteAsync(id, ct);
        return NoContent();
    }
}
