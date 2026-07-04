using System.Security.Claims;
using Josapar.Api.Infrastructure.Auth;
using Josapar.Api.Infrastructure.Persistence;
using Josapar.Api.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Josapar.Api.Features.Orders;

public static class OrderEndpoints
{
    public static RouteGroupBuilder MapOrderEndpoints(this RouteGroupBuilder app)
    {
        app.MapGet("/", ListOrdersAsync);
        app.MapPost("/", CreateOrderAsync);
        app.MapPost("/sync", SyncPendingOrdersAsync);

        return app;
    }

    private static async Task<IResult> ListOrdersAsync(ClaimsPrincipal user, AppDbContext db)
    {
        var representativeId = user.GetRepresentativeId();

        var orders = await db.Orders
            .AsNoTracking()
            .Include(o => o.Client)
            .Include(o => o.Items)
            .Where(o => o.RepresentativeId == representativeId)
            .OrderByDescending(o => o.CreatedAtUtc)
            .ToListAsync();

        return Results.Ok(orders.Select(ToResponse));
    }

    private static async Task<IResult> CreateOrderAsync(
        CreateOrderRequest request,
        ClaimsPrincipal user,
        AppDbContext db,
        IConfiguration configuration)
    {
        if (request.Items.Count == 0)
        {
            return Results.BadRequest("O pedido precisa de ao menos um item.");
        }

        var client = await db.Clients.SingleOrDefaultAsync(c => c.Id == request.ClientId);
        if (client is null) return Results.NotFound("Cliente não encontrado.");

        var productIds = request.Items.Select(i => i.ProductId).ToList();
        var products = await db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var missingProductIds = productIds.Where(id => !products.ContainsKey(id)).ToList();
        if (missingProductIds.Count > 0)
        {
            return Results.BadRequest($"Produto(s) não encontrado(s): {string.Join(", ", missingProductIds)}.");
        }

        var orderItems = request.Items.Select(item =>
        {
            var product = products[item.ProductId];
            var subtotal = Math.Round(product.Price * item.Quantity * (1 - item.DiscountPercent / 100), 2);

            return new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                ProductNameSnapshot = product.Name,
                ProductSkuSnapshot = product.Sku,
                UnitPriceSnapshot = product.Price,
                Quantity = item.Quantity,
                DiscountPercent = item.DiscountPercent,
                Subtotal = subtotal,
            };
        }).ToList();

        var subtotal = Math.Round(orderItems.Sum(i => i.UnitPriceSnapshot * i.Quantity), 2);
        var discountsTotal = Math.Round(subtotal - orderItems.Sum(i => i.Subtotal), 2);
        var netBeforeTax = subtotal - discountsTotal;
        var taxRate = configuration.GetValue("Orders:FlatTaxRate", 0.08m);
        var taxesTotal = Math.Round(netBeforeTax * taxRate, 2);
        var total = netBeforeTax + taxesTotal;

        var order = new Order
        {
            Id = Guid.NewGuid(),
            Code = await GenerateUniqueCodeAsync(db),
            ClientId = client.Id,
            RepresentativeId = user.GetRepresentativeId(),
            Notes = request.Notes,
            Status = request.IsDraft ? OrderStatus.Draft : OrderStatus.Pending,
            Subtotal = subtotal,
            DiscountsTotal = discountsTotal,
            TaxesTotal = taxesTotal,
            Total = total,
            CreatedAtUtc = DateTime.UtcNow,
            Items = orderItems,
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        order.Client = client;
        return Results.Created($"/api/orders/{order.Id}", ToResponse(order));
    }

    private static async Task<IResult> SyncPendingOrdersAsync(ClaimsPrincipal user, AppDbContext db)
    {
        var representativeId = user.GetRepresentativeId();

        var pendingOrders = await db.Orders
            .Where(o => o.RepresentativeId == representativeId && o.Status == OrderStatus.Pending)
            .ToListAsync();

        var syncedAt = DateTime.UtcNow;
        foreach (var order in pendingOrders)
        {
            order.Status = OrderStatus.Sent;
            order.SyncedAtUtc = syncedAt;
        }

        await db.SaveChangesAsync();

        return Results.Ok(new SyncResultResponse(pendingOrders.Count));
    }

    private static async Task<string> GenerateUniqueCodeAsync(AppDbContext db)
    {
        string code;
        do
        {
            code = $"PED-{Random.Shared.Next(1000, 9999)}";
        }
        while (await db.Orders.AnyAsync(o => o.Code == code));

        return code;
    }

    private static OrderResponse ToResponse(Order order) => new(
        order.Id.ToString(),
        order.Code,
        order.ClientId.ToString(),
        order.Client?.Name ?? string.Empty,
        order.Notes,
        order.Status,
        order.Subtotal,
        order.DiscountsTotal,
        order.TaxesTotal,
        order.Total,
        order.CreatedAtUtc,
        order.SyncedAtUtc,
        order.Items.Select(i => new OrderItemResponse(
            i.ProductId.ToString(),
            i.ProductNameSnapshot,
            i.ProductSkuSnapshot,
            i.UnitPriceSnapshot,
            i.Quantity,
            i.DiscountPercent,
            i.Subtotal)).ToList());
}
