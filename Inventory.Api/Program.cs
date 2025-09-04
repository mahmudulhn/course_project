using Inventory.Api.Data;
using Microsoft.EntityFrameworkCore;

var b = WebApplication.CreateBuilder(args);

// Get connection string from environment variable or appsettings
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") 
                       ?? b.Configuration.GetConnectionString("DefaultConnection");

b.Services.AddDbContext<InventoryContext>(o => 
    o.UseNpgsql(connectionString));

b.Services.AddControllers();

var app = b.Build();

// Auto-migrate on startup (for production)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<InventoryContext>();
    context.Database.Migrate();
}

app.MapGet("/api/health", () => new { ok = true });
app.MapControllers();
app.Run();