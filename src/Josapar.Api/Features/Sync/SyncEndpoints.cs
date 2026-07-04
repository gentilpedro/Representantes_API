using System.Security.Claims;
using Josapar.Api.Infrastructure.Auth;
using Josapar.Api.Infrastructure.Persistence;
using Josapar.Api.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Josapar.Api.Features.Sync;

/// <summary>
/// Só o que é derivável de dados reais (pedidos enviados do representante).
/// Sem fila/histórico/conflito fictício — a fila de sync em si é responsabilidade
/// do app (Orders locais pendentes), não do servidor.
/// </summary>
public static class SyncEndpoints
{
    public static RouteGroupBuilder MapSyncEndpoints(this RouteGroupBuilder app)
    {
        app.MapGet("/summary", GetSummaryAsync);
        return app;
    }

    private static async Task<IResult> GetSummaryAsync(ClaimsPrincipal user, AppDbContext db)
    {
        var representativeId = user.GetRepresentativeId();

        var successCount = await db.Orders
            .CountAsync(o => o.RepresentativeId == representativeId && o.Status == OrderStatus.Sent);

        var lastOrderSyncedAtUtc = await db.Orders
            .Where(o => o.RepresentativeId == representativeId && o.SyncedAtUtc != null)
            .Select(o => o.SyncedAtUtc)
            .OrderDescending()
            .FirstOrDefaultAsync();

        var representativeLastSyncAtUtc = await db.Representatives
            .Where(r => r.Id == representativeId)
            .Select(r => r.LastSyncAtUtc)
            .SingleAsync();

        DateTime? lastSyncedAtUtc = (lastOrderSyncedAtUtc, representativeLastSyncAtUtc) switch
        {
            (null, null) => null,
            (var a, null) => a,
            (null, var b) => b,
            (var a, var b) => a > b ? a : b,
        };

        return Results.Ok(new SyncSummaryResponse(lastSyncedAtUtc, successCount));
    }
}
