namespace OrderService.Api.Services;

public interface IProductClient
{
    Task<decimal?> GetProductPriceAsync(int productId);
}