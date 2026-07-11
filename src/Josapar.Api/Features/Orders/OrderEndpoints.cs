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
        app.MapGet("/", ListOrdersAsync)
            .WithSummary("Listar pedidos")
            .WithDescription("Lista os pedidos do representante autenticado, do mais recente para o mais antigo.");
        app.MapPost("/", CreateOrderAsync)
            .WithSummary("Criar pedido")
            .WithDescription("Cria um pedido (rascunho ou enviado). Idempotente via clientGeneratedId para reenvios sem duplicar.");
        app.MapPost("/batch-sync", BatchSyncOrdersAsync)
            .WithSummary("Sincronizar pedidos offline")
            .WithDescription("Recebe em lote os pedidos criados offline pelo app e persiste os que ainda não existem no servidor.");

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
        var representativeId = user.GetRepresentativeId();

        if (request.ClientGeneratedId is Guid clientGeneratedId)
        {
            var existing = await FindByClientGeneratedIdAsync(db, representativeId, clientGeneratedId);
            if (existing is not null) return Results.Ok(ToResponse(existing));
        }

        var (client, orderItems, error) = await PrepareOrderAsync(db, request.ClientId, request.Items);
        if (error is not null) return error.NotFound ? Results.NotFound(error.Message) : Results.BadRequest(error.Message);

        var code = await GenerateUniqueCodeAsync(db, []);
        var now = DateTime.UtcNow;
        var order = BuildOrder(
            client!, orderItems!, configuration, representativeId, request.Notes,
            request.IsDraft ? OrderStatus.Draft : OrderStatus.Sent,
            request.ClientGeneratedId, syncedAtUtc: request.IsDraft ? null : now, code);

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        return Results.Created($"/api/orders/{order.Id}", ToResponse(order));
    }

    /// <summary>
    /// Recebe a fila de pedidos criados offline e persiste os que ainda não existem no
    /// servidor (idempotente via ClientGeneratedId — reenvios por falha de rede não duplicam).
    /// Um pedido inválido (cliente/produto que deixou de existir) é rejeitado individualmente
    /// e reportado em <see cref="BatchSyncResultResponse.Rejected"/> — não pode travar a
    /// sincronização dos demais pedidos válidos do lote.
    /// </summary>
    private static async Task<IResult> BatchSyncOrdersAsync(
        IReadOnlyList<BatchSyncOrderRequest> requests,
        ClaimsPrincipal user,
        AppDbContext db,
        IConfiguration configuration)
    {
        var representativeId = user.GetRepresentativeId();
        var created = new List<OrderResponse>();
        var alreadySynced = new List<OrderResponse>();
        var rejected = new List<BatchSyncRejectedResponse>();
        var usedCodes = new HashSet<string>();

        foreach (var request in requests)
        {
            var existing = await FindByClientGeneratedIdAsync(db, representativeId, request.ClientGeneratedId);
            if (existing is not null)
            {
                alreadySynced.Add(ToResponse(existing));
                continue;
            }

            var (client, orderItems, error) = await PrepareOrderAsync(db, request.ClientId, request.Items);
            if (error is not null)
            {
                rejected.Add(new BatchSyncRejectedResponse(request.ClientGeneratedId, error.Message));
                continue;
            }

            var code = await GenerateUniqueCodeAsync(db, usedCodes);
            var order = BuildOrder(
                client!, orderItems!, configuration, representativeId, request.Notes,
                OrderStatus.Sent, request.ClientGeneratedId, syncedAtUtc: DateTime.UtcNow, code);

            db.Orders.Add(order);
            created.Add(ToResponse(order));
        }

        await db.SaveChangesAsync();

        return Results.Ok(new BatchSyncResultResponse(created, alreadySynced, rejected));
    }

    private static Task<Order?> FindByClientGeneratedIdAsync(AppDbContext db, Guid representativeId, Guid clientGeneratedId) =>
        db.Orders
            .AsNoTracking()
            .Include(o => o.Client)
            .Include(o => o.Items)
            .SingleOrDefaultAsync(o => o.RepresentativeId == representativeId && o.ClientGeneratedId == clientGeneratedId);

    private static async Task<(Client? Client, List<OrderItem>? Items, PrepareOrderError? Error)> PrepareOrderAsync(
        AppDbContext db, Guid clientId, IReadOnlyList<CreateOrderItemRequest> items)
    {
        if (items.Count == 0)
        {
            return (null, null, new PrepareOrderError("O pedido precisa de ao menos um item."));
        }

        var invalidItems = items
            .Where(i => i.Quantity <= 0 || i.DiscountPercent < 0 || i.DiscountPercent > 100)
            .ToList();
        if (invalidItems.Count > 0)
        {
            return (null, null, new PrepareOrderError(
                "Quantidade deve ser maior que zero e desconto deve estar entre 0% e 100%."));
        }

        var client = await db.Clients.SingleOrDefaultAsync(c => c.Id == clientId);
        if (client is null) return (null, null, new PrepareOrderError("Cliente não encontrado.", NotFound: true));

        var productIds = items.Select(i => i.ProductId).ToList();
        var products = await db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var missingProductIds = productIds.Where(id => !products.ContainsKey(id)).ToList();
        if (missingProductIds.Count > 0)
        {
            return (null, null, new PrepareOrderError($"Produto(s) não encontrado(s): {string.Join(", ", missingProductIds)}."));
        }

        var orderItems = items.Select(item =>
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

        return (client, orderItems, null);
    }

    private static Order BuildOrder(
        Client client,
        List<OrderItem> orderItems,
        IConfiguration configuration,
        Guid representativeId,
        string? notes,
        OrderStatus status,
        Guid? clientGeneratedId,
        DateTime? syncedAtUtc,
        string code)
    {
        var subtotal = Math.Round(orderItems.Sum(i => i.UnitPriceSnapshot * i.Quantity), 2);
        var discountsTotal = Math.Round(subtotal - orderItems.Sum(i => i.Subtotal), 2);
        var netBeforeTax = subtotal - discountsTotal;
        var taxRate = configuration.GetValue("Orders:FlatTaxRate", 0.08m);
        var taxesTotal = Math.Round(netBeforeTax * taxRate, 2);
        var total = netBeforeTax + taxesTotal;

        var order = new Order
        {
            Id = Guid.NewGuid(),
            Code = code,
            ClientId = client.Id,
            RepresentativeId = representativeId,
            ClientGeneratedId = clientGeneratedId,
            Notes = notes,
            Status = status,
            Subtotal = subtotal,
            DiscountsTotal = discountsTotal,
            TaxesTotal = taxesTotal,
            Total = total,
            CreatedAtUtc = DateTime.UtcNow,
            SyncedAtUtc = syncedAtUtc,
            Items = orderItems,
        };
        order.Client = client;

        return order;
    }

    private static async Task<string> GenerateUniqueCodeAsync(AppDbContext db, HashSet<string> usedCodes)
    {
        string code;
        do
        {
            code = $"PED-{Random.Shared.Next(1000, 9999)}";
        }
        while (usedCodes.Contains(code) || await db.Orders.AnyAsync(o => o.Code == code));

        usedCodes.Add(code);
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

    private sealed record PrepareOrderError(string Message, bool NotFound = false);
}
