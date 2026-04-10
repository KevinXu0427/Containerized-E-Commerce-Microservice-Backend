using OrderService.Api.Data;
using OrderService.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var orderDb = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true"
    ? "Data Source=/app/Data/orders.db"
    : builder.Environment.IsDevelopment()
        ? "Data Source=orders.db"
        : "Data Source=/app/Data/orders.db";

builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseSqlite(orderDb));

var rabbitHost = builder.Configuration["RabbitMQ:HostName"] ?? "localhost";
builder.Services.AddSingleton(_ => new RabbitMqPublisher(rabbitHost, "orders-queue"));
builder.Services.AddHostedService<StockUpdatesConsumerHostedService>();

var customerUrl = "http://customerservice:8080/";
builder.Services.AddHttpClient<ICustomerClient, CustomerClient>(c =>
{
    c.BaseAddress = new Uri(customerUrl);
});

var productUrl = "http://productservice:8080/";
builder.Services.AddHttpClient<IProductClient, ProductClient>(c =>
{
    c.BaseAddress = new Uri(productUrl);
});

var inventoryUrl = "http://inventoryservice:8080/";
builder.Services.AddHttpClient<IInventoryClient, InventoryClient>(c =>
{
    c.BaseAddress = new Uri(inventoryUrl);
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
