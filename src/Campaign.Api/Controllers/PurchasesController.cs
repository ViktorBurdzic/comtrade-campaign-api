using Campaign.Application.Common;
using Campaign.Application.Purchases;
using Microsoft.AspNetCore.Mvc;

namespace Campaign.Api.Controllers;

[ApiController]
[Route("api/v1/purchases")]
public sealed class PurchasesController : ControllerBase
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    private readonly IPurchaseImportService _import;

    public PurchasesController(IPurchaseImportService import)
    {
        _import = import;
    }

    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxFileSizeBytes)]
    [ProducesResponseType(typeof(ImportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ImportResultDto>> Import(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            throw new BadRequestException("Upload a non-empty .csv file in the 'file' form field.");

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            throw new BadRequestException("Only .csv files are supported.");

        await using var stream = file.OpenReadStream();
        var result = await _import.ImportAsync(stream, file.FileName, ct);

        return Ok(result);
    }
}