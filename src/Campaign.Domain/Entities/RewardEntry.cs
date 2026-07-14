using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Campaign.Domain.Entities;

public class RewardEntry
{
    public Guid Id { get; set; }
    public string AgentUsername { get; set; } = string.Empty;
    public int CustomerId { get; set; }

    // snapshot at reward time so reports don't depend on the external service
    public string CustomerName { get; set; } = string.Empty;

    public DateOnly RewardDate { get; set; }
    public decimal DiscountPercent { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    // soft delete: keeps history and frees the agent's daily slot
    public bool IsDeleted { get; set; }
}
