using Inventory.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure connection string with proper URL parsing for Render
var connectionString = GetConnectionString(builder.Configuration);

// Add services
builder.Services.AddDbContext<InventoryContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllers();

// Add CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure middleware
app.UseCors();

// Configure port for Render (and other cloud providers)
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");

// Auto-migrate on startup with proper error handling
await MigrateDatabase(app.Services);

// Configure endpoints
app.MapGet("/api/health", () => new { ok = true });
app.MapControllers();

app.Run();

// Helper method to get connection string with URL parsing
static string GetConnectionString(IConfiguration configuration)
{
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    
    // Debug logging
    Console.WriteLine($"DATABASE_URL exists: {!string.IsNullOrEmpty(databaseUrl)}");
    
    // If DATABASE_URL is provided (like on Render), it's usually in URL format
    if (!string.IsNullOrEmpty(databaseUrl))
    {
        Console.WriteLine("Using DATABASE_URL");
        
        // Handle postgres:// URL format (common on Render, Heroku, etc.)
        if (databaseUrl.StartsWith("postgres://") || databaseUrl.StartsWith("postgresql://"))
        {
            try
            {
                var uri = new Uri(databaseUrl);
                var userInfo = uri.UserInfo.Split(':');
                
                var connectionString = $"Host={uri.Host};" +
                                     $"Port={uri.Port};" +
                                     $"Database={uri.AbsolutePath.TrimStart('/')};" +
                                     $"Username={userInfo[0]};" +
                                     $"Password={userInfo[1]};" +
                                     $"SSL Mode=Require;" +
                                     $"Trust Server Certificate=true";
                
                Console.WriteLine("Converted URL to connection string format");
                return connectionString;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing DATABASE_URL: {ex.Message}");
                throw new InvalidOperationException("Invalid DATABASE_URL format", ex);
            }
        }
        
        // If it's already in connection string format, use as-is
        return databaseUrl;
    }
    
    // Fall back to appsettings for local development
    Console.WriteLine("Using DefaultConnection from appsettings");
    var fallbackConnection = configuration.GetConnectionString("DefaultConnection");
    
    if (string.IsNullOrEmpty(fallbackConnection))
    {
        throw new InvalidOperationException("No database connection string found. Set DATABASE_URL environment variable or DefaultConnection in appsettings.json");
    }
    
    return fallbackConnection;
}

// Helper method for database migration with error handling
static async Task MigrateDatabase(IServiceProvider services)
{
    try
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InventoryContext>();
        
        Console.WriteLine("Testing database connection...");
        
        // Test connection first
        var canConnect = await context.Database.CanConnectAsync();
        Console.WriteLine($"Database connection test: {(canConnect ? "SUCCESS" : "FAILED")}");
        
        if (canConnect)
        {
            Console.WriteLine("Running database migrations...");
            await context.Database.MigrateAsync();
            Console.WriteLine("Database migrations completed successfully");
        }
        else
        {
            Console.WriteLine("Cannot connect to database. Skipping migrations.");
            throw new InvalidOperationException("Database connection failed");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database migration failed: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        throw; // Re-throw to stop the application if DB is critical
    }
}