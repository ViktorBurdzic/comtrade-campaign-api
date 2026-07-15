using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Campaign.Application.Purchases;

public sealed record PurchaseCsvRow(
    int CustomerId,
    DateOnly PurchaseDate,
    decimal Amount,
    string Currency);

public sealed record CsvParseResult(
    IReadOnlyList<PurchaseCsvRow> Rows,
    IReadOnlyList<string> Errors);

public sealed record ImportResultDto(
    int TotalDataRows,
    int Imported,
    int SkippedDuplicates,
    int Failed,
    IReadOnlyList<string> Errors);
