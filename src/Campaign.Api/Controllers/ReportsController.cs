using Campaign.Application.Reports;
using Microsoft.AspNetCore.Mvc;

namespace Campaign.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _reports;

    public ReportsController(IReportService reports)
    {
        _reports = reports;
    }

    [HttpGet("campaign-results")]
    [ProducesResponseType(typeof(CampaignReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CampaignReportDto>> GetCampaignResults(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
    {
        return Ok(await _reports.GetCampaignResultsAsync(from, to, ct));
    }
}