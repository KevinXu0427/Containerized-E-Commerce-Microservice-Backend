using System.ComponentModel.DataAnnotations;

namespace OrderService.Api.DTOs;

public class UpdateOrderStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;
}
