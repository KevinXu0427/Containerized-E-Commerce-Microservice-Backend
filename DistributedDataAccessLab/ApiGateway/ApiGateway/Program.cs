using ApiGateway.Swagger;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;

var builder = WebApplication.CreateBuilder(args);

// Ocelot: ocelot.json + AddOcelot + UseOcelot; Polly provider for resilience.
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile($"ocelot.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

builder.Services.AddOcelot(builder.Configuration).AddPolly();

var ordersBase = builder.Configuration["Services:OrderService"] ?? "http://orderservice:8080";
var customersBase = builder.Configuration["Services:CustomerService"] ?? "http://customerservice:8080";
var productsBase = builder.Configuration["Services:ProductService"] ?? "http://productservice:8080";

builder.Services.AddHttpClient("orders", c => c.BaseAddress = new Uri(ordersBase.TrimEnd('/') + "/"));
builder.Services.AddHttpClient("customers", c => c.BaseAddress = new Uri(customersBase.TrimEnd('/') + "/"));
builder.Services.AddHttpClient("products", c => c.BaseAddress = new Uri(productsBase.TrimEnd('/') + "/"));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.DocumentFilter<OcelotRoutesDocumentFilter>());

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Ocelot is terminal: if registered on the main pipeline after MapControllers, it still runs for every
// request and returns 404 when no route matches — so /api/gateway/... (GatewayController BFF) never runs.
// Only run Ocelot for paths that are actually proxied (ocelot.json uses /gateway/...).
app.MapWhen(
    static ctx => ctx.Request.Path.StartsWithSegments("/gateway"),
    subApp => { subApp.UseOcelot().GetAwaiter().GetResult(); });

app.MapControllers();

app.Run();
