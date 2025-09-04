using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.Data;

public class InventoryContext : DbContext 
{
    public InventoryContext(DbContextOptions<InventoryContext> opt) : base(opt) {}
    public DbSet<User> Users => Set<User>();
}

public class User 
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
}