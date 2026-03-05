using System.Net;
using System.Net.Http.Json;

namespace OrderService.Api.Services;

public class InventoryClient : IInventoryClient
{
    private readonly HttpClient _httpClient;

    public InventoryClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> ReserveAsync(int productId, int quantity)
    {
        var body = new { productId, quantity };
        var res = await _httpClient.PostAsJsonAsync("api/inventory/reserve", body);

        // 200 OKz: reserved
        if (res.StatusCode == HttpStatusCode.OK) return true;

        // 409 Conflict: insufficient stock
        if (res.StatusCode == HttpStatusCode.Conflict) return false;

        // 404 NotFound or others
        return false;
    }
}