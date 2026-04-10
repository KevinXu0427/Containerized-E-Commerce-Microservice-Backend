using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Api.Data;
using OrderService.Api.DTOs;
using OrderService.Api.Models;
using OrderService.Api.Services;
using System.Text.Json;

namespace OrderService.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly OrdersDbContext _context;
    private readonly ICustomerClient _customerClient;
    private readonly IProductClient _productClient;
    private readonly IInventoryClient _inventoryClient;
    private readonly RabbitMqPublisher _orderEventsPublisher;

    public OrdersController(
        OrdersDbContext context,
        ICustomerClient customerClient,
        IProductClient productClient,
        IInventoryClient inventoryClient,
        RabbitMqPublisher orderEventsPublisher)
    {
        _context = context;
        _customerClient = customerClient;
        _productClient = productClient;
        _inventoryClient = inventoryClient;
        _orderEventsPublisher = orderEventsPublisher;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> GetAll()
    {
        var list = await _context.Orders.Select(o => ToResponse(o)).ToListAsync();
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderResponse>> GetById(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound();
        return Ok(ToResponse(order));
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Create([FromBody] CreateOrderRequest req)
    {
        var customerExists = await _customerClient.CustomerExistsAsync(req.CustomerId);
        if (!customerExists)
            return BadRequest("Customer does not exist.");

        var unitPrice = await _productClient.GetProductPriceAsync(req.ProductId);
        if (unitPrice == null)
            return BadRequest("Product does not exist.");

        var reserve = await _inventoryClient.ReserveAsync(req.ProductId, req.Quantity);
        if (reserve == InventoryReserveResult.NoInventoryRecord)
            return Conflict("No inventory for this product. Add stock first: POST /gateway/inventory/createOrUpdate.");
        if (reserve != InventoryReserveResult.Success)
            return Conflict("Insufficient stock.");

        var discountMultiplier = 1m - (req.DiscountPercent / 100m);
        var total = unitPrice.Value * req.Quantity * discountMultiplier;

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

        var created = JsonSerializer.Serialize(new
        {
            eventType = "OrderCreated",
            orderId = order.Id,
            order.CustomerId,
            order.ProductId,
            order.Quantity,
            order.Total
        });
        _orderEventsPublisher.Publish(created);

        return Ok(ToResponse(order));
    }

    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<OrderResponse>> UpdateStatus(int id, [FromBody] UpdateOrderStatusRequest body)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null)
            return NotFound();

        var previous = order.Status;
        order.Status = body.Status;
        await _context.SaveChangesAsync();

        if (string.Equals(body.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(previous, "Cancelled", StringComparison.OrdinalIgnoreCase))
        {
            var payload = JsonSerializer.Serialize(new { eventType = "OrderCancelled", orderId = order.Id });
            _orderEventsPublisher.Publish(payload);
        }

        return Ok(ToResponse(order));
    }

    private static OrderResponse ToResponse(Order o) => new()
    {
        Id = o.Id,
        CustomerId = o.CustomerId,
        ProductId = o.ProductId,
        Quantity = o.Quantity,
        UnitPrice = o.UnitPrice,
        DiscountPercent = o.DiscountPercent,
        Total = o.Total,
        Status = o.Status,
        CreatedAt = o.CreatedAt
    };
}
