using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Globalization;
using Campaign.Application.Purchases;
using CsvHelper;
using CsvHelper.Configuration;
using System.Formats.Asn1;

namespace Campaign.Infrastructure.Csv;

// expected format: CustomerId,PurchaseDate,Amount,Currency
public sealed class PurchaseCsvParser : IPurchaseCsvParser
{
    private const string ExpectedHeader = "CustomerId,PurchaseDate,Amount,Currency";

    public CsvParseResult Parse(Stream csvStream)
    {
        var rows = new List<PurchaseCsvRow>();
        var errors = new List<string>();

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            BadDataFound = null,
            HeaderValidated = null
        };

        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, config);

        if (!csv.Read() || !csv.ReadHeader())
        {
            errors.Add($"The file is empty or has no header row. Expected header: {ExpectedHeader}");
            return new CsvParseResult(rows, errors);
        }

        var line = 1;

        while (csv.Read())
        {
            line++;
            try
            {
                var customerIdRaw = csv.GetField("CustomerId");
                var dateRaw = csv.GetField("PurchaseDate");
                var amountRaw = csv.GetField("Amount");
                var currency = csv.GetField("Currency");

                if (!int.TryParse(customerIdRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var customerId)
                    || customerId <= 0)
                {
                    throw new FormatException($"invalid CustomerId '{customerIdRaw}' (expected a positive integer)");
                }

                if (!DateOnly.TryParseExact(dateRaw, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var purchaseDate))
                {
                    throw new FormatException($"invalid PurchaseDate '{dateRaw}' (expected yyyy-MM-dd)");
                }

                if (!decimal.TryParse(amountRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
                    || amount <= 0)
                {
                    throw new FormatException($"invalid Amount '{amountRaw}' (expected a positive number)");
                }

                if (string.IsNullOrWhiteSpace(currency))
                    currency = "EUR";

                rows.Add(new PurchaseCsvRow(customerId, purchaseDate, amount, currency.ToUpperInvariant()));
            }
            catch (Exception ex)
            {
                errors.Add($"Row {line}: {ex.Message}");
            }
        }

        return new CsvParseResult(rows, errors);
    }
}
