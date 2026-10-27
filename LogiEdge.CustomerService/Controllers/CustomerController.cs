using LogiEdge.CustomerService.Dto;
using LogiEdge.CustomerService.Services;
using LogiEdge.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogiEdge.CustomerService.Controllers;

[ApiController]
[Route("/api/customers")]
[Authorize(AuthenticationSchemes = AppAuthSchemes.CookieAndToken)]
public class CustomerController(CustomerManagementService service)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCustomers() => Ok(await service.GetCustomersAsync());

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCustomer(Guid id)
    {
        CustomerDto? customer = await service.GetCustomerByIdAsync(id);

        if (customer == null)
            return NotFound();

        return Ok(customer);
    }
}
