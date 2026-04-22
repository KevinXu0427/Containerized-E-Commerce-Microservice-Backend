using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/gateway")]
[Tags("Gateway")]
public class GatewayController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GatewayController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("order/{orderId:int}")]
    public async Task<IActionResult> GetOrderWithCustomerAndProduct(int orderId)
    {
        var orders = _httpClientFactory.CreateClient("orders");
        var customers = _httpClientFactory.CreateClient("customers");
        var products = _httpClientFactory.CreateClient("products");

        var orderRes = await orders.GetAsync($"api/orders/{orderId}");
        if (orderRes.StatusCode == HttpStatusCode.NotFound)
            return NotFound();
        if (!orderRes.IsSuccessStatusCode)
            return StatusCode((int)orderRes.StatusCode, await orderRes.Content.ReadAsStringAsync());

        var order = await orderRes.Content.ReadFromJsonAsync<OrderDto>(JsonOptions);
        if (order == null)
            return BadRequest("Invalid order payload.");

        var customerTask = customers.GetAsync($"api/customers/{order.CustomerId}");
        var productTask = products.GetAsync($"api/products/{order.ProductId}");
        await Task.WhenAll(customerTask, productTask);

        object? customer = null;
        var customerRes = await customerTask;
        if (customerRes.IsSuccessStatusCode)
            customer = await customerRes.Content.ReadFromJsonAsync<JsonElement>();

        object? product = null;
        var productRes = await productTask;
        if (productRes.IsSuccessStatusCode)
            product = await productRes.Content.ReadFromJsonAsync<JsonElement>();

        return Ok(new
        {
            order,
            customer,
            product
        });
    }

    private sealed class OrderDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
