using Microsoft.EntityFrameworkCore;
using InventoryService.Api.Data;

var builder = WebApplication.CreateBuilder(args);

var inventoryDb = builder.Environment.IsDevelopment()
    ? "Data Source=inventory.db"
    : "Data Source=/app/Data/inventory.db";

builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseSqlite(inventoryDb));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// helpful for docker runs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.Run();