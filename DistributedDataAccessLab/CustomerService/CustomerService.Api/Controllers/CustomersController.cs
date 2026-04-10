using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CustomerService.Api.Data;
using CustomerService.Api.DTOs;
using CustomerService.Api.Models;

namespace CustomerService.Api.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly CustomerDbContext _context;

    public CustomersController(CustomerDbContext context)
    {
        _context = context;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerResponse>> GetById(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
            return NotFound();

        return Ok(ToResponse(customer));
    }

    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> Create([FromBody] CreateCustomerRequest req)
    {
        var customer = new Customer
        {
            Name = req.Name,
            Email = req.Email
        };
        await _context.Customers.AddAsync(customer);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, ToResponse(customer));
    }

    private static CustomerResponse ToResponse(Customer c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Email = c.Email
    };
}
