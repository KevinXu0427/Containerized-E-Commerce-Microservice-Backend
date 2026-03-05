using System.ComponentModel.DataAnnotations;

namespace OrderService.Api.DTOs;

public class CreateOrderRequest
{
    [Required]
    public int CustomerId { get; set; }

    [Required]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    // 10 = 10% off
    [Range(0, 100)]
    public decimal DiscountPercent { get; set; } = 0;
}