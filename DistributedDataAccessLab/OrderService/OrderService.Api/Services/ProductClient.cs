using System.Net;

namespace OrderService.Api.Services;

public class ProductClient : IProductClient
{
    private readonly HttpClient _httpClient;
    public ProductClient(HttpClient httpClient) { _httpClient = httpClient; }

    private sealed class ProductDto
    {
        public int Id { get; set; }
        public decimal Price { get; set; }
    }

    public async Task<decimal?> GetProductPriceAsync(int productId)
    {
        var res = await _httpClient.GetAsync($"api/products/{productId}");
        if (res.StatusCode != HttpStatusCode.OK)
            return null;

        var product = await res.Content.ReadFromJsonAsync<ProductDto>();
        return product?.Price;
    }
}