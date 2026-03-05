namespace InventoryService.Api.Models;

public class InventoryItem
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int Stock { get; set; }

}