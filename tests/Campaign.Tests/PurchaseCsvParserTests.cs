using System.Text;
using Campaign.Infrastructure.Csv;
using Xunit;

namespace Campaign.Tests;

public sealed class PurchaseCsvParserTests
{
    private static Stream AsStream(string content) =>
        new MemoryStream(Encoding.UTF8.GetBytes(content));

    [Fact]
    public void Parses_valid_rows()
    {
        const string csv =
            "CustomerId,PurchaseDate,Amount,Currency\n" +
            "1,2026-08-10,199.99,EUR\n" +
            "5,2026-08-11,49.50,RSD\n";

        var result = new PurchaseCsvParser().Parse(AsStream(csv));

        Assert.Equal(2, result.Rows.Count);
        Assert.Empty(result.Errors);

        Assert.Equal(1, result.Rows[0].CustomerId);
        Assert.Equal(new DateOnly(2026, 8, 10), result.Rows[0].PurchaseDate);
        Assert.Equal(199.99m, result.Rows[0].Amount);
        Assert.Equal("EUR", result.Rows[0].Currency);
    }

    [Fact]
    public void Collects_errors_for_bad_rows_and_keeps_good_ones()
    {
        const string csv =
            "CustomerId,PurchaseDate,Amount,Currency\n" +
            "1,2026-08-10,199.99,EUR\n" +
            "abc,2026-08-10,10.00,EUR\n" +
            "2,10/08/2026,15.00,EUR\n" +
            "3,2026-08-12,-5.00,EUR\n" +
            "4,2026-08-13,20.00,\n";

        var result = new PurchaseCsvParser().Parse(AsStream(csv));

        Assert.Equal(2, result.Rows.Count);
        Assert.Equal(3, result.Errors.Count);

        Assert.Contains(result.Errors, e => e.Contains("CustomerId"));
        Assert.Contains(result.Errors, e => e.Contains("PurchaseDate"));
        Assert.Contains(result.Errors, e => e.Contains("Amount"));

        Assert.Equal("EUR", result.Rows[1].Currency);
    }

    [Fact]
    public void Reports_missing_header()
    {
        var result = new PurchaseCsvParser().Parse(AsStream(string.Empty));

        Assert.Empty(result.Rows);
        Assert.Single(result.Errors);
        Assert.Contains("header", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Currency_is_normalized_to_upper_case()
    {
        const string csv =
            "CustomerId,PurchaseDate,Amount,Currency\n" +
            "1,2026-08-10,10.00,eur\n";

        var result = new PurchaseCsvParser().Parse(AsStream(csv));

        Assert.Equal("EUR", result.Rows[0].Currency);
    }
}