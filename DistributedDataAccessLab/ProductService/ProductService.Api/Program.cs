using System.Data;
using Microsoft.EntityFrameworkCore;
using ProductService.Api.Data;

var builder = WebApplication.CreateBuilder(args);

var productDb = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true"
    ? "Data Source=/app/Data/products.db"
    : builder.Environment.IsDevelopment()
        ? "Data Source=products.db"
        : "Data Source=/app/Data/products.db";

builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseSqlite(productDb));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

async Task DropLegacyProductStockColumnIfPresentAsync(ProductDbContext db)
{
    if (db.Database.ProviderName != "Microsoft.EntityFrameworkCore.Sqlite")
        return;

    var conn = db.Database.GetDbConnection();
    var openedHere = conn.State != ConnectionState.Open;
    if (openedHere)
        await conn.OpenAsync();
    try
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Products'";
        if (Convert.ToInt64(await cmd.ExecuteScalarAsync()) == 0)
            return;

        cmd.CommandText = "SELECT COUNT(*) FROM pragma_table_info('Products') WHERE name='Stock'";
        if (Convert.ToInt64(await cmd.ExecuteScalarAsync()) == 0)
            return;

        cmd.CommandText = "ALTER TABLE Products DROP COLUMN Stock;";
        await cmd.ExecuteNonQueryAsync();
    }
    finally
    {
        if (openedHere && conn.State == ConnectionState.Open)
            await conn.CloseAsync();
    }
}

// Ensure DB exists (helpful for docker runs); drop legacy Products.Stock so schema matches the model (stock lives in Inventory).
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    await db.Database.EnsureCreatedAsync();
    await DropLegacyProductStockColumnIfPresentAsync(db);
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.Run();