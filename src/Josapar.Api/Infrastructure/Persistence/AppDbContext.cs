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
    public DbSet<Visit> Visits => Set<Visit>();
    public DbSet<RepresentativeGoal> RepresentativeGoals => Set<RepresentativeGoal>();
    public DbSet<Notification> Notifications => Set<Notification>();

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

        modelBuilder.Entity<Visit>(entity =>
        {
            entity.HasOne(v => v.Client)
                .WithMany()
                .HasForeignKey(v => v.ClientId);
        });

        modelBuilder.Entity<RepresentativeGoal>(entity =>
        {
            entity.HasIndex(g => new { g.RepresentativeId, g.Year, g.Month }).IsUnique();
        });
    }
}
