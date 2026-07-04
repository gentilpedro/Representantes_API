using System.Text.Json;
using Josapar.Api.Infrastructure.Auth;
using Josapar.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Josapar.Api.Infrastructure.Persistence;

/// <summary>
/// Seed de desenvolvimento com as credenciais de teste documentadas no README,
/// mais alguns produtos/clientes sintéticos para exercitar os endpoints.
/// </summary>
public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (!await db.Representatives.AnyAsync())
        {
            db.Representatives.AddRange(
                new Representative
                {
                    Id = Guid.NewGuid(),
                    MatriculaCode = "88294",
                    Email = "ricardo.santos@josapar.com",
                    Name = "Ricardo Santos",
                    Role = "Representante Comercial Sênior",
                    Region = "Região Sul",
                    PasswordHash = PasswordHasher.Hash("Josapar@123"),
                    IsActivated = true,
                    CreatedAtUtc = DateTime.UtcNow,
                },
                new Representative
                {
                    Id = Guid.NewGuid(),
                    MatriculaCode = "00123456",
                    Email = null,
                    Name = "Fernanda Lima",
                    Role = "Representante Comercial",
                    Region = "Região Nordeste",
                    PasswordHash = null,
                    IsActivated = false,
                    CreatedAtUtc = DateTime.UtcNow,
                });
        }

        if (!await db.Products.AnyAsync())
        {
            db.Products.AddRange(
                NewProduct("ARZ-001", "Josapar", "Arroz Branco Tipo 1 5kg", "Arroz", 24.90m, 500, badge: ProductBadge.None),
                NewProduct("ARZ-002", "Josapar", "Arroz Integral 1kg", "Arroz", 8.50m, 40, badge: ProductBadge.Offer, originalPrice: 10.90m),
                NewProduct("FJO-001", "Josapar", "Feijão Carioca 1kg", "Feijões", 7.20m, 0, badge: ProductBadge.OutOfStock),
                NewProduct("FJO-002", "Josapar", "Feijão Preto 1kg", "Feijões", 7.80m, 300, badge: ProductBadge.None),
                NewProduct("OUT-001", "Josapar", "Farinha de Rosca 500g", "Outros", 5.40m, 120, badge: ProductBadge.None));
        }

        if (!await db.Clients.AnyAsync())
        {
            db.Clients.AddRange(
                new Client
                {
                    Id = Guid.NewGuid(),
                    Code = "1001",
                    Name = "Mercado Bom Preço",
                    Cnpj = "12.345.678/0001-90",
                    Tier = ClientTier.Gold,
                    Phone = "(51) 3222-1000",
                    Mobile = "(51) 99900-1000",
                    Email = "compras@bompreco.example.com",
                    CreditLimit = 50_000m,
                    CreditUsedPercent = 0.35m,
                    DeliveryAddress = new DeliveryAddress
                    {
                        Street = "Av. das Indústrias, 500",
                        District = "Distrito Industrial",
                        City = "Porto Alegre",
                        State = "RS",
                    },
                    IsFavorite = true,
                    CreatedAtUtc = DateTime.UtcNow,
                },
                new Client
                {
                    Id = Guid.NewGuid(),
                    Code = "1002",
                    Name = "Atacado Silva & Filhos",
                    Cnpj = "98.765.432/0001-10",
                    Tier = ClientTier.Regular,
                    Phone = "(51) 3222-2000",
                    Mobile = "(51) 99900-2000",
                    Email = "financeiro@silvaefilhos.example.com",
                    CreditLimit = 20_000m,
                    CreditUsedPercent = 0.60m,
                    DeliveryAddress = new DeliveryAddress
                    {
                        Street = "Rua das Palmeiras, 120",
                        District = "Centro",
                        City = "Caxias do Sul",
                        State = "RS",
                    },
                    PendingInvoiceAmount = 3_450.00m,
                    PendingInvoiceDueDateUtc = DateTime.UtcNow.AddDays(5),
                    CreatedAtUtc = DateTime.UtcNow,
                },
                new Client
                {
                    Id = Guid.NewGuid(),
                    Code = "1003",
                    Name = "Distribuidora Boa Vista",
                    Cnpj = "11.222.333/0001-44",
                    Tier = ClientTier.Blocked,
                    Phone = "(51) 3222-3000",
                    Mobile = "(51) 99900-3000",
                    Email = "contato@boavista.example.com",
                    CreditLimit = 10_000m,
                    CreditUsedPercent = 1.0m,
                    DeliveryAddress = new DeliveryAddress
                    {
                        Street = "Rua da Estação, 45",
                        District = "Vila Nova",
                        City = "Novo Hamburgo",
                        State = "RS",
                    },
                    Notes = "Bloqueado por inadimplência — aguardando regularização financeira.",
                    CreatedAtUtc = DateTime.UtcNow,
                });
        }

        await db.SaveChangesAsync();

        var representative = await db.Representatives.SingleAsync(r => r.MatriculaCode == "88294");
        var clients = await db.Clients.OrderBy(c => c.Code).ToListAsync();

        var now = DateTime.UtcNow;

        if (!await db.RepresentativeGoals.AnyAsync(g => g.RepresentativeId == representative.Id
            && g.Year == now.Year && g.Month == now.Month))
        {
            db.RepresentativeGoals.Add(new RepresentativeGoal
            {
                Id = Guid.NewGuid(),
                RepresentativeId = representative.Id,
                Year = now.Year,
                Month = now.Month,
                TargetAmount = 15_000m,
                CreatedAtUtc = now,
            });
        }

        if (!await db.Visits.AnyAsync(v => v.RepresentativeId == representative.Id))
        {
            var today = now.Date;
            db.Visits.AddRange(
                new Visit
                {
                    Id = Guid.NewGuid(),
                    ClientId = clients[0].Id,
                    RepresentativeId = representative.Id,
                    ScheduledAtUtc = today.AddHours(9),
                    Status = VisitStatus.Completed,
                    Notes = "Cliente confirmou reposição mensal.",
                    CheckInAtUtc = today.AddHours(9).AddMinutes(5),
                    CheckInLatitude = -30.0346,
                    CheckInLongitude = -51.2177,
                    IsGeoValidated = true,
                    CheckOutAtUtc = today.AddHours(9).AddMinutes(40),
                    CreatedAtUtc = now,
                },
                new Visit
                {
                    Id = Guid.NewGuid(),
                    ClientId = clients[1].Id,
                    RepresentativeId = representative.Id,
                    ScheduledAtUtc = today.AddHours(11),
                    Status = VisitStatus.Pending,
                    CreatedAtUtc = now,
                },
                new Visit
                {
                    Id = Guid.NewGuid(),
                    ClientId = clients[2].Id,
                    RepresentativeId = representative.Id,
                    ScheduledAtUtc = today.AddHours(15),
                    Status = VisitStatus.Pending,
                    CreatedAtUtc = now,
                });
        }

        if (!await db.Notifications.AnyAsync(n => n.RepresentativeId == representative.Id))
        {
            db.Notifications.AddRange(
                new Notification
                {
                    Id = Guid.NewGuid(),
                    RepresentativeId = representative.Id,
                    Category = NotificationCategory.Order,
                    Title = "Pedido sincronizado",
                    Message = "Seu pedido para Mercado Bom Preço foi enviado com sucesso.",
                    IsUrgent = false,
                    IsRead = false,
                    CreatedAtUtc = now.AddHours(-2),
                },
                new Notification
                {
                    Id = Guid.NewGuid(),
                    RepresentativeId = representative.Id,
                    Category = NotificationCategory.Announcement,
                    Title = "Manutenção programada",
                    Message = "O sistema ficará indisponível das 22h às 23h para manutenção.",
                    IsUrgent = true,
                    IsRead = false,
                    CreatedAtUtc = now.AddHours(-6),
                },
                new Notification
                {
                    Id = Guid.NewGuid(),
                    RepresentativeId = representative.Id,
                    Category = NotificationCategory.Promotion,
                    Title = "Nova promoção de Arroz Integral",
                    Message = "Desconto especial em Arroz Integral 1kg até o fim do mês.",
                    IsUrgent = false,
                    IsRead = true,
                    CreatedAtUtc = now.AddDays(-3),
                });
        }

        await db.SaveChangesAsync();
    }

    private static Product NewProduct(
        string sku,
        string brand,
        string name,
        string category,
        decimal price,
        int availableUnits,
        ProductBadge badge,
        decimal? originalPrice = null) => new()
    {
        Id = Guid.NewGuid(),
        Sku = sku,
        Brand = brand,
        Name = name,
        Category = category,
        ImageUrl = $"https://picsum.photos/seed/{sku}/400",
        Price = price,
        AvailableUnits = availableUnits,
        OriginalPrice = originalPrice,
        Badge = badge,
        CommercialDescription = $"{name} — qualidade Josapar, embalagem padrão de distribuição.",
        TechnicalSpecsJson = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["Categoria"] = category,
            ["Marca"] = brand,
        }),
        ImagesJson = JsonSerializer.Serialize(new List<string> { $"https://picsum.photos/seed/{sku}/400" }),
        AppliedPromotionsJson = JsonSerializer.Serialize(new List<string>()),
        CreatedAtUtc = DateTime.UtcNow,
        StockByWarehouse =
        [
            new WarehouseStock
            {
                Id = Guid.NewGuid(),
                WarehouseName = "CD Porto Alegre",
                State = "RS",
                BundlesAvailable = availableUnits / 10,
                Level = availableUnits switch
                {
                    0 => StockLevel.Critical,
                    < 50 => StockLevel.Medium,
                    _ => StockLevel.High,
                },
            },
        ],
    };
}
