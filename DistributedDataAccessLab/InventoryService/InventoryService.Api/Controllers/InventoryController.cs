using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryService.Api.Data;
using InventoryService.Api.DTOs;
using InventoryService.Api.Models;
using InventoryService.Api.Services;
using System.Text.Json;

namespace InventoryService.Api.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly InventoryDbContext _context;
    private readonly RabbitMqPublisher _stockPublisher;

    public InventoryController(InventoryDbContext context, RabbitMqPublisher stockPublisher)
    {
        _context = context;
        _stockPublisher = stockPublisher;
    }

    [HttpGet("{productId:int}")]
    public async Task<ActionResult<InventoryItemResponse>> GetByProductId(int productId)
    {
        var item = await _context.Inventory.FirstOrDefaultAsync(x => x.ProductId == productId);
        if (item == null) return NotFound();
        return Ok(ToResponse(item));
    }

    [HttpPost("createOrUpdate")]
    public async Task<ActionResult<InventoryItemResponse>> CreateOrUpdate([FromBody] ReserveStockRequest req)
    {
        var item = await _context.Inventory.FirstOrDefaultAsync(x => x.ProductId == req.ProductId);

        if (item == null)
        {
            item = new InventoryItem { ProductId = req.ProductId, Stock = req.Quantity };
            await _context.Inventory.AddAsync(item);
        }
        else
            item.Stock = req.Quantity;

        await _context.SaveChangesAsync();
        PublishStockUpdated(req.ProductId, item.Stock, "createOrUpdate");
        return Ok(ToResponse(item));
    }

    [HttpPost("reserve")]
    public async Task<ActionResult<StockChangeResponse>> Reserve([FromBody] ReserveStockRequest req)
    {
        var item = await _context.Inventory.FirstOrDefaultAsync(x => x.ProductId == req.ProductId);
        if (item == null) return NotFound("Inventory record not found.");

        if (item.Stock < req.Quantity)
            return Conflict("Insufficient stock.");

        item.Stock -= req.Quantity;
        await _context.SaveChangesAsync();

        PublishStockUpdated(req.ProductId, item.Stock, "reserve");
        return Ok(new StockChangeResponse
        {
            ProductId = req.ProductId,
            Quantity = req.Quantity,
            RemainingStock = item.Stock
        });
    }

    [HttpPost("release")]
    public async Task<ActionResult<StockChangeResponse>> Release([FromBody] ReserveStockRequest req)
    {
        var item = await _context.Inventory.FirstOrDefaultAsync(x => x.ProductId == req.ProductId);

        if (item == null)
        {
            item = new InventoryItem { ProductId = req.ProductId, Stock = 0 };
            await _context.Inventory.AddAsync(item);
        }

        item.Stock += req.Quantity;
        await _context.SaveChangesAsync();

        PublishStockUpdated(req.ProductId, item.Stock, "release");
        return Ok(new StockChangeResponse
        {
            ProductId = req.ProductId,
            Quantity = req.Quantity,
            RemainingStock = item.Stock
        });
    }

    private void PublishStockUpdated(int productId, int newStock, string reason)
    {
        var payload = JsonSerializer.Serialize(new
        {
            eventType = "StockUpdated",
            productId,
            newStock,
            reason
        });
        _stockPublisher.Publish(payload);
    }

    private static InventoryItemResponse ToResponse(InventoryItem item) => new()
    {
        Id = item.Id,
        ProductId = item.ProductId,
        Stock = item.Stock
    };
}
