using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Campaign.Application.Customers;

public sealed record CustomerDto(
    int Id,
    string Name,
    string? Ssn,
    string? DateOfBirth,
    string? HomeCity,
    string? HomeState
    );
