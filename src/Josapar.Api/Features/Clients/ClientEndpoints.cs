using Josapar.Api.Infrastructure.Persistence;
using Josapar.Api.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Josapar.Api.Features.Clients;

public static class ClientEndpoints
{
    public static RouteGroupBuilder MapClientEndpoints(this RouteGroupBuilder app)
    {
        app.MapGet("/", ListClientsAsync);
        app.MapGet("/{id:guid}", GetClientDetailAsync);
        app.MapPatch("/{id:guid}/favorite", ToggleFavoriteAsync);

        return app;
    }

    private static async Task<IResult> ListClientsAsync(AppDbContext db)
    {
        var clients = await db.Clients.AsNoTracking().ToListAsync();

        var lastOrderByClient = await db.Orders
            .AsNoTracking()
            .GroupBy(o => o.ClientId)
            .Select(g => new { ClientId = g.Key, LastOrderAtUtc = g.Max(o => o.CreatedAtUtc) })
            .ToDictionaryAsync(x => x.ClientId, x => (DateTime?)x.LastOrderAtUtc);

        var response = clients.Select(c => new ClientListItemResponse(
            c.Id.ToString(),
            c.Code,
            c.Name,
            c.Cnpj,
            c.DeliveryAddress.City,
            c.DeliveryAddress.State,
            c.Tier,
            lastOrderByClient.GetValueOrDefault(c.Id),
            c.CreditLimit,
            c.IsFavorite));

        return Results.Ok(response);
    }

    private static async Task<IResult> GetClientDetailAsync(Guid id, AppDbContext db)
    {
        var client = await db.Clients.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id);
        if (client is null) return Results.NotFound();

        var orderHistory = await db.Orders
            .AsNoTracking()
            .Where(o => o.ClientId == id)
            .OrderByDescending(o => o.CreatedAtUtc)
            .Select(o => new ClientOrderHistoryItemResponse(o.Code, o.CreatedAtUtc, o.Total, o.Status))
            .ToListAsync();

        return Results.Ok(ToDetailResponse(client, orderHistory));
    }

    private static async Task<IResult> ToggleFavoriteAsync(Guid id, FavoriteToggleRequest request, AppDbContext db)
    {
        var client = await db.Clients.SingleOrDefaultAsync(c => c.Id == id);
        if (client is null) return Results.NotFound();

        client.IsFavorite = request.IsFavorite;
        await db.SaveChangesAsync();

        var lastOrderAtUtc = await db.Orders
            .AsNoTracking()
            .Where(o => o.ClientId == id)
            .Select(o => (DateTime?)o.CreatedAtUtc)
            .OrderDescending()
            .FirstOrDefaultAsync();

        return Results.Ok(new ClientListItemResponse(
            client.Id.ToString(),
            client.Code,
            client.Name,
            client.Cnpj,
            client.DeliveryAddress.City,
            client.DeliveryAddress.State,
            client.Tier,
            lastOrderAtUtc,
            client.CreditLimit,
            client.IsFavorite));
    }

    private static ClientDetailResponse ToDetailResponse(
        Client client,
        IReadOnlyList<ClientOrderHistoryItemResponse> orderHistory) => new(
        client.Id.ToString(),
        client.Code,
        client.Name,
        client.Cnpj,
        client.Tier,
        client.Phone,
        client.Mobile,
        client.Email,
        client.CreditLimit,
        client.CreditUsedPercent,
        client.CreditLimit * (1 - client.CreditUsedPercent),
        new DeliveryAddressResponse(
            client.DeliveryAddress.Street,
            client.DeliveryAddress.District,
            client.DeliveryAddress.City,
            client.DeliveryAddress.State),
        client.PendingInvoiceAmount is null
            ? null
            : new PendingInvoiceResponse(client.PendingInvoiceDueDateUtc!.Value, client.PendingInvoiceAmount.Value),
        client.Notes,
        client.IsFavorite,
        orderHistory);
}
