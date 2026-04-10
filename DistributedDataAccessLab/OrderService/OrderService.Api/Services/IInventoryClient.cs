namespace OrderService.Api.Services;

public interface IInventoryClient
{
    Task<InventoryReserveResult> ReserveAsync(int productId, int quantity);
}

