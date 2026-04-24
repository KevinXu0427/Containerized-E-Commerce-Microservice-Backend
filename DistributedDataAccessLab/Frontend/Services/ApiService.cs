using System.Net;
using System.Net.Http.Json;
using Frontend.Models;

namespace Frontend.Services;

public class ApiService
{
    private readonly HttpClient _http;
    private readonly GatewayPaths _paths;

    public ApiService(HttpClient http, GatewayPaths paths)
    {
        _http = http;
        _paths = paths;
    }

    private static async Task ThrowIfFailedAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();
        if (body.Length > 800)
            body = body[..800] + "…";

        var detail = string.IsNullOrEmpty(body)
            ? (response.ReasonPhrase ?? response.StatusCode.ToString())
            : body;

        throw new HttpRequestException(
            $"{(int)response.StatusCode} {response.StatusCode}: {detail}",
            inner: null,
            statusCode: response.StatusCode);
    }

    public async Task<List<ProductDto>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        var list = await _http.GetFromJsonAsync<List<ProductDto>>(_paths.Products, cancellationToken);
        return list ?? [];
    }

    public async Task<ProductDto?> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync(_paths.Products, request, cancellationToken);
        await ThrowIfFailedAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<ProductDto>(cancellationToken: cancellationToken);
    }

    public async Task<List<CustomerDto>> GetCustomersAsync(CancellationToken cancellationToken = default)
    {
        var list = await _http.GetFromJsonAsync<List<CustomerDto>>(_paths.Customers, cancellationToken);
        return list ?? [];
    }

    public async Task<CustomerDto?> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync(_paths.Customers, request, cancellationToken);
        await ThrowIfFailedAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<CustomerDto>(cancellationToken: cancellationToken);
    }

    public async Task<List<OrderDto>> GetOrdersAsync(CancellationToken cancellationToken = default)
    {
        var list = await _http.GetFromJsonAsync<List<OrderDto>>(_paths.Orders, cancellationToken);
        return list ?? [];
    }

    public async Task<OrderDto?> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync(_paths.Orders, request, cancellationToken);
        await ThrowIfFailedAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<OrderDto>(cancellationToken: cancellationToken);
    }

    public async Task<List<InventoryItemDto>> GetInventoryAsync(CancellationToken cancellationToken = default)
    {
        var list = await _http.GetFromJsonAsync<List<InventoryItemDto>>(_paths.Inventory, cancellationToken);
        return list ?? [];
    }

    public async Task<InventoryItemDto?> GetInventoryByProductIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync($"{_paths.Inventory}/{productId}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        await ThrowIfFailedAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<InventoryItemDto>(cancellationToken: cancellationToken);
    }

    public async Task<InventoryItemDto?> SetInventoryAsync(InventoryProductQuantityRequest body, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync($"{_paths.Inventory}/createOrUpdate", body, cancellationToken);
        await ThrowIfFailedAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<InventoryItemDto>(cancellationToken: cancellationToken);
    }

    public async Task<StockChangeDto?> ReserveInventoryAsync(InventoryProductQuantityRequest body, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync($"{_paths.Inventory}/reserve", body, cancellationToken);
        await ThrowIfFailedAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<StockChangeDto>(cancellationToken: cancellationToken);
    }

    public async Task<StockChangeDto?> ReleaseInventoryAsync(InventoryProductQuantityRequest body, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync($"{_paths.Inventory}/release", body, cancellationToken);
        await ThrowIfFailedAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<StockChangeDto>(cancellationToken: cancellationToken);
    }
}
