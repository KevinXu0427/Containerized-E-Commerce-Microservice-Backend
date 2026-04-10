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

    public async Task<InventoryReserveResult> ReserveAsync(int productId, int quantity)
    {
        var body = new { productId, quantity };
        var res = await _httpClient.PostAsJsonAsync("api/inventory/reserve", body);

        if (res.StatusCode == HttpStatusCode.OK)
            return InventoryReserveResult.Success;

        if (res.StatusCode == HttpStatusCode.NotFound)
            return InventoryReserveResult.NoInventoryRecord;

        if (res.StatusCode == HttpStatusCode.Conflict)
            return InventoryReserveResult.InsufficientStock;

        return InventoryReserveResult.InsufficientStock;
    }
}