using System.ComponentModel.DataAnnotations;

namespace  InventoryService.Api.Models;

public class ReserveRequest
{
    [Required]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity {get; set; }
}