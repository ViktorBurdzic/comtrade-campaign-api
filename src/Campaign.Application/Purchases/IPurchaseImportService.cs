using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Campaign.Application.Purchases;

public interface IPurchaseImportService
{
    Task<ImportResultDto> ImportAsync(Stream csvStream, string fileName, CancellationToken ct = default);
}
