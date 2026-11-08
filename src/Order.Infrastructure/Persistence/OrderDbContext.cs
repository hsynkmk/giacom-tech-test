using Microsoft.EntityFrameworkCore;
using Order.Domain.Entities;
using OrderEntity = Order.Domain.Entities.Order;

namespace Order.Infrastructure.Persistence;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options)
        : base(options)
    {
    }

    public DbSet<OrderEntity> Orders => Set<OrderEntity>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Service> Services => Set<Service>();

    public DbSet<OrderStatus> OrderStatuses => Set<OrderStatus>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // All ids are stored as binary(16). EF's built-in Guid <-> byte[] conversion uses Guid.ToByteArray(),
        // which is the same byte order as the binToGuid/guidToBin functions in mysql-init.sql,
        // so ids from the seeded data come out exactly as before.
        configurationBuilder.Properties<Guid>()
            .HaveConversion<byte[]>()
            .HaveColumnType("binary(16)");

        // SQLite (used by the tests) can't aggregate decimals, so store them as double there.
        // MySQL keeps proper decimal columns.
        if (IsSqlite)
        {
            configurationBuilder.Properties<decimal>().HaveConversion<double>();
        }
    }

    // Compared by name so Infrastructure doesn't need a reference to the SQLite provider package.
    private bool IsSqlite => Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderDbContext).Assembly);
    }
}
