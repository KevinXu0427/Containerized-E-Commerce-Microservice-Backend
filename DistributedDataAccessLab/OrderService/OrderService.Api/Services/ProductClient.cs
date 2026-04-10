using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace OrderService.Api.Services;

public class ProductClient : IProductClient
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ProductClient(HttpClient httpClient) => _httpClient = httpClient;

    private sealed class ProductPriceDto
    {
        public decimal Price { get; set; }
    }

    public async Task<decimal?> GetProductPriceAsync(int productId)
    {
        var res = await _httpClient.GetAsync($"api/products/{productId}");
        if (res.StatusCode != HttpStatusCode.OK)
            return null;

        var product = await res.Content.ReadFromJsonAsync<ProductPriceDto>(JsonOptions);
        return product?.Price;
    }
}
