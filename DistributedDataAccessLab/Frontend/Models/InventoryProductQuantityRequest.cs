using System.ComponentModel.DataAnnotations;

namespace Frontend.Models;

public class InventoryProductQuantityRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}
