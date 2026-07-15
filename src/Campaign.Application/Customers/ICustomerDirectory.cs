using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Campaign.Application.Customers;

public interface ICustomerDirectory
{
    Task<CustomerDto?> FindPersonAsync(int id, CancellationToken ct = default);
}
