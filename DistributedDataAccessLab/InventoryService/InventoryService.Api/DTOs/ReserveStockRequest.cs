using System.ComponentModel.DataAnnotations;

namespace InventoryService.Api.DTOs;

public class ReserveStockRequest
{
    [Required]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}
