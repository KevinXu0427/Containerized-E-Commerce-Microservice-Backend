using Microsoft.EntityFrameworkCore;
using ProductService.Api.Data;

var builder = WebApplication.CreateBuilder(args);

var productDb = builder.Environment.IsDevelopment()
    ? "Data Source=products.db"
    : "Data Source=/app/Data/products.db";

builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseSqlite(productDb));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Ensure DB exists (helpful for docker runs)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.Run();