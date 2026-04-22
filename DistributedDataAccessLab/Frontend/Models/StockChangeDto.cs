namespace Frontend.Models;

public class StockChangeDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public int RemainingStock { get; set; }
}
