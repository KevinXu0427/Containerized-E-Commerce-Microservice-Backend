using InventoryService.Api.Data;
using InventoryService.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var inventoryDb = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true"
    ? "Data Source=/app/Data/inventory.db"
    : builder.Environment.IsDevelopment()
        ? "Data Source=inventory.db"
        : "Data Source=/app/Data/inventory.db";

builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseSqlite(inventoryDb));

var rabbitHost = builder.Configuration["RabbitMQ:HostName"] ?? "localhost";
builder.Services.AddSingleton(_ => new RabbitMqPublisher(rabbitHost, "stock-updates-queue"));
builder.Services.AddHostedService<OrderQueueConsumerHostedService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
