using System.ComponentModel.DataAnnotations;

namespace Frontend.Models;

public class CreateOrderRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int CustomerId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0, 100)]
    public decimal DiscountPercent { get; set; }
}
