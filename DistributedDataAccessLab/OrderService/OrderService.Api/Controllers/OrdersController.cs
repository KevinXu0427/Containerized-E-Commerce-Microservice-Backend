using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Api.Data;
using OrderService.Api.DTOs;
using OrderService.Api.Models;
using OrderService.Api.Services;

namespace OrderService.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly OrdersDbContext _context;
    private readonly ICustomerClient _customerClient;
    private readonly IProductClient _productClient;
    private readonly IInventoryClient _inventoryClient;

    public OrdersController( OrdersDbContext context,
        ICustomerClient customerClient,
        IProductClient productClient,
        IInventoryClient inventoryClient)
    {
        _context = context;
        _customerClient = customerClient;
        _productClient = productClient;
        _inventoryClient = inventoryClient;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _context.Orders.ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest req)
    {
        // 1) validate customer
        var customerExists = await _customerClient.CustomerExistsAsync(req.CustomerId);
        if (!customerExists)
            return BadRequest("Customer does not exist.");

        // 2) get product price (also validates product exists)
        var unitPrice = await _productClient.GetProductPriceAsync(req.ProductId);
        if (unitPrice == null)
            return BadRequest("Product does not exist.");

        // 3) reserve inventory
        var reserved = await _inventoryClient.ReserveAsync(req.ProductId, req.Quantity);
        if (!reserved)
            return Conflict("Insufficient stock.");

        // 4) compute total
        var discountMultiplier = 1m - (req.DiscountPercent / 100m);
        var total = unitPrice.Value * req.Quantity * discountMultiplier;

        // 5) save order
        var order = new Order
        {
            CustomerId = req.CustomerId,
            ProductId = req.ProductId,
            Quantity = req.Quantity,
            UnitPrice = unitPrice.Value,
            DiscountPercent = req.DiscountPercent,
            Total = total,
            Status = "Created",
            CreatedAt = DateTime.UtcNow
        };

        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();

        return Ok(req);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        var order = await _context.Orders.FindAsync(id);

        if (order == null)
            return NotFound();

        order.Status = status;

        await _context.SaveChangesAsync();

        return Ok(order);
    }

}