using Campaign.Application.Common;
using Campaign.Application.Customers;
using Microsoft.AspNetCore.Mvc;

namespace Campaign.Api.Controllers;

[ApiController]
[Route("api/v1/customers")]
public sealed class CustomersController : ControllerBase
{
    private readonly ICustomerDirectory _customers;

    public CustomersController(ICustomerDirectory customers)
    {
        _customers = customers;
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDto>> GetById(int id, CancellationToken ct)
    {
        var customer = await _customers.FindPersonAsync(id, ct)
            ?? throw new NotFoundException($"Customer with id {id} was not found in the customer directory.");

        return Ok(customer);
    }
}