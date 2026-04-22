using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Frontend;
using Frontend.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var baseUrl = (builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5080").TrimEnd('/');
var paths = new GatewayPaths(
    builder.Configuration["Api:ProductsPath"] ?? "/gateway/products",
    builder.Configuration["Api:CustomersPath"] ?? "/gateway/customers",
    builder.Configuration["Api:OrdersPath"] ?? "/gateway/orders",
    builder.Configuration["Api:InventoryBasePath"] ?? "/gateway/inventory");

builder.Services.AddSingleton(paths);
builder.Services.AddScoped(_ => new ApiService(
    new HttpClient { BaseAddress = new Uri(baseUrl) },
    paths));

await builder.Build().RunAsync();
