using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Campaign.Domain.Entities;

// one row from the monthly purchase report (.csv)
public class PurchaseRecord
{
    public Guid Id { get; set; }
    public int CustomerId { get; set; }
    public DateOnly PurchaseDate { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public string SourceFileName { get; set; } = string.Empty;
    public DateTime ImportedAtUtc { get; set; }
}
