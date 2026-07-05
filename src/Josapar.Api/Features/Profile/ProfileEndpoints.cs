using System.Security.Claims;
using Josapar.Api.Infrastructure.Auth;
using Josapar.Api.Infrastructure.Persistence;
using Josapar.Api.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Josapar.Api.Features.Profile;

public static class ProfileEndpoints
{
    public static RouteGroupBuilder MapProfileEndpoints(this RouteGroupBuilder app)
    {
        app.MapGet("/permissions", GetPermissionsAsync)
            .WithSummary("Permissões do representante")
            .WithDescription("Retorna as permissões do representante autenticado com base no seu cargo.");
        return app;
    }

    /// <summary>
    /// Regra provisória: reps "Sênior" têm acesso a tabelas especiais e aprovação de
    /// crédito. Não é uma regra de negócio formal — é o ponto de partida até existir
    /// uma especificação real de permissões por representante.
    /// </summary>
    private static async Task<IResult> GetPermissionsAsync(ClaimsPrincipal user, AppDbContext db)
    {
        var representative = await db.Representatives
            .AsNoTracking()
            .SingleAsync(r => r.Id == user.GetRepresentativeId());

        var isSenior = representative.Role.Contains("Sênior", StringComparison.OrdinalIgnoreCase);

        var permissions = new[]
        {
            new PermissionResponse("Vendas Offline", "Registrar pedidos sem conexão com a internet.", PermissionStatus.Granted),
            new PermissionResponse(
                "Acesso a Tabelas Especiais",
                "Ver preços e condições comerciais diferenciadas.",
                isSenior ? PermissionStatus.Granted : PermissionStatus.Restricted),
            new PermissionResponse(
                "Aprovação de Crédito",
                "Aprovar pedidos acima do limite de crédito padrão do cliente.",
                isSenior ? PermissionStatus.Granted : PermissionStatus.Restricted),
        };

        return Results.Ok(permissions);
    }
}
