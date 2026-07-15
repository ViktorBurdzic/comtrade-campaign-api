using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Campaign.Application.Reports;

public interface IReportService
{
    Task<CampaignReportDto> GetCampaignResultsAsync(
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken ct = default);
}
