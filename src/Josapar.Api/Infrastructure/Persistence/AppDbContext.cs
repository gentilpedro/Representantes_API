using Josapar.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Josapar.Api.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Representative> Representatives => Set<Representative>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Lead> Leads => Set<Lead>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Representative>(entity =>
        {
            entity.HasIndex(r => r.MatriculaCode).IsUnique();
            entity.HasIndex(r => r.Email).IsUnique().HasFilter("`Email` IS NOT NULL");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(p => p.Sku).IsUnique();
            entity.HasMany(p => p.StockByWarehouse)
                .WithOne()
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasIndex(c => c.Code).IsUnique();
            entity.HasIndex(c => c.Cnpj).IsUnique();
            entity.OwnsOne(c => c.DeliveryAddress);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasIndex(o => o.Code).IsUnique();
            entity.HasOne(o => o.Client)
                .WithMany()
                .HasForeignKey(o => o.ClientId);
            entity.HasMany(o => o.Items)
                .WithOne()
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
