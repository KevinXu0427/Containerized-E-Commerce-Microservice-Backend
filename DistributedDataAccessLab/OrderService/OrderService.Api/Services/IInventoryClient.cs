namespace OrderService.Api.Services;

public interface IInventoryClient
{
    Task<bool> ReserveAsync(int productId, int quantity);
}

