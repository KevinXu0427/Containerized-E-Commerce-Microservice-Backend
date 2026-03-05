using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryService.Api.Data;
using InventoryService.Api.Models;

namespace InventoryService.Api.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly InventoryDbContext _context;

    public InventoryController(InventoryDbContext context)
    {
        _context = context;
    }

    // GET /api/inventory/{productId}
    [HttpGet("{productId:int}")]
    public async Task<IActionResult> GetByProductId(int productId)
    {
        var item = await _context.Inventory
            .FirstOrDefaultAsync(x => x.ProductId == productId);

        if (item == null) return NotFound();
        return Ok(item);
    }

    // POST /api/inventory/createOrUpdate
    // body: { "productId": 1, "quantity": 100 }
    [HttpPost("createOrUpdate")]
    public async Task<IActionResult> CreateOrUpdate([FromBody] ReserveRequest req)
    {
        var item = await _context.Inventory
            .FirstOrDefaultAsync(x => x.ProductId == req.ProductId);

        if (item == null)
        {
            item = new InventoryItem
            {
                ProductId = req.ProductId,
                Stock = req.Quantity
            };
            await _context.Inventory.AddAsync(item);
        }
        else
        {
            item.Stock = req.Quantity;
        }

        await _context.SaveChangesAsync();
        return Ok(item);
    }

    // POST /api/inventory/reserve
    // body: { "productId": 1, "quantity": 2 }
    [HttpPost("reserve")]
    public async Task<IActionResult> Reserve([FromBody] ReserveRequest req)
    {
        var item = await _context.Inventory
            .FirstOrDefaultAsync(x => x.ProductId == req.ProductId);

        if (item == null) return NotFound("Inventory record not found.");

        if (item.Stock < req.Quantity)
            return Conflict("Insufficient stock."); // 409

        item.Stock -= req.Quantity;
        await _context.SaveChangesAsync();

        return Ok(new { req.ProductId, Reserved = req.Quantity, Remaining = item.Stock });
    }

    // POST /api/inventory/release
    // body: { "productId": 1, "quantity": 2 }
    [HttpPost("release")]
    public async Task<IActionResult> Release([FromBody] ReserveRequest req)
    {
        var item = await _context.Inventory
            .FirstOrDefaultAsync(x => x.ProductId == req.ProductId);

        if (item == null)
        {
            item = new InventoryItem { ProductId = req.ProductId, Stock = 0 };
            await _context.Inventory.AddAsync(item);
        }

        item.Stock += req.Quantity;
        await _context.SaveChangesAsync();

        return Ok(new { req.ProductId, Released = req.Quantity, Now = item.Stock });
    }
}