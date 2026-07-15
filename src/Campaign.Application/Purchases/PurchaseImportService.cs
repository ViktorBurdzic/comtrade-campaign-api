using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Campaign.Application.Common;
using Campaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Campaign.Application.Purchases;

public sealed class PurchaseImportService : IPurchaseImportService
{
    private readonly IApplicationDbContext _db;
    private readonly IPurchaseCsvParser _parser;

    public PurchaseImportService(IApplicationDbContext db, IPurchaseCsvParser parser)
    {
        _db = db;
        _parser = parser;
    }

    public async Task<ImportResultDto> ImportAsync(Stream csvStream, string fileName, CancellationToken ct = default)
    {
        var parsed = _parser.Parse(csvStream);

        var customerIdsInFile = parsed.Rows.Select(r => r.CustomerId).Distinct().ToList();

        var existing = await _db.Purchases
            .Where(p => customerIdsInFile.Contains(p.CustomerId))
            .Select(p => new { p.CustomerId, p.PurchaseDate, p.Amount })
            .ToListAsync(ct);

        var seenKeys = existing
            .Select(e => (e.CustomerId, e.PurchaseDate, e.Amount))
            .ToHashSet();

        var toInsert = new List<PurchaseRecord>();
        var skippedDuplicates = 0;

        foreach (var row in parsed.Rows)
        {
            if (!seenKeys.Add((row.CustomerId, row.PurchaseDate, row.Amount)))
            {
                skippedDuplicates++;
                continue;
            }

            toInsert.Add(new PurchaseRecord
            {
                Id = Guid.NewGuid(),
                CustomerId = row.CustomerId,
                PurchaseDate = row.PurchaseDate,
                Amount = row.Amount,
                Currency = row.Currency,
                SourceFileName = fileName,
                ImportedAtUtc = DateTime.UtcNow
            });
        }

        if (toInsert.Count > 0)
        {
            _db.Purchases.AddRange(toInsert);
            await _db.SaveChangesAsync(ct);
        }

        return new ImportResultDto(
            TotalDataRows: parsed.Rows.Count + parsed.Errors.Count,
            Imported: toInsert.Count,
            SkippedDuplicates: skippedDuplicates,
            Failed: parsed.Errors.Count,
            Errors: parsed.Errors);
    }
}
