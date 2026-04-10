namespace InventoryService.Api.DTOs;

public class StockChangeResponse
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public int RemainingStock { get; set; }
}
