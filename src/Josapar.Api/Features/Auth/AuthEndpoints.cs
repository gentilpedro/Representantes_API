using Josapar.Api.Infrastructure.Auth;
using Josapar.Api.Infrastructure.Persistence;
using Josapar.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Josapar.Api.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/login", LoginAsync);
        app.MapPost("/first-access/check", CheckFirstAccessAsync);
        app.MapPost("/first-access/activate", ActivateAccountAsync);

        return app;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        AppDbContext db,
        JwtTokenGenerator tokenGenerator)
    {
        var representative = await FindByIdentifierAsync(db, request.Identifier);

        if (representative is null
            || !representative.IsActivated
            || representative.PasswordHash is null
            || !PasswordHasher.Verify(request.Password, representative.PasswordHash))
        {
            return Results.Unauthorized();
        }

        representative.LastSyncAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(BuildAuthResponse(representative, tokenGenerator));
    }

    private static async Task<IResult> CheckFirstAccessAsync(
        FirstAccessCheckRequest request,
        AppDbContext db)
    {
        var representative = await db.Representatives
            .SingleOrDefaultAsync(r => r.MatriculaCode == request.Identifier);

        if (representative is null) return Results.NotFound();
        if (representative.IsActivated) return Results.Conflict();

        return Results.Ok(new FirstAccessCheckResponse(representative.Name));
    }

    private static async Task<IResult> ActivateAccountAsync(
        ActivateAccountRequest request,
        AppDbContext db,
        JwtTokenGenerator tokenGenerator)
    {
        var representative = await db.Representatives
            .SingleOrDefaultAsync(r => r.MatriculaCode == request.Identifier);

        if (representative is null) return Results.NotFound();
        if (representative.IsActivated) return Results.Conflict();

        var unmetRequirements = PasswordPolicy.GetUnmetRequirements(request.Password);
        if (unmetRequirements.Count > 0)
        {
            return Results.BadRequest(new PasswordPolicyErrorResponse(unmetRequirements));
        }

        representative.PasswordHash = PasswordHasher.Hash(request.Password);
        representative.IsActivated = true;
        representative.LastSyncAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(BuildAuthResponse(representative, tokenGenerator));
    }

    private static Task<Representative?> FindByIdentifierAsync(AppDbContext db, string identifier) =>
        db.Representatives.SingleOrDefaultAsync(
            r => r.MatriculaCode == identifier || r.Email == identifier);

    private static AuthResponse BuildAuthResponse(
        Representative representative,
        JwtTokenGenerator tokenGenerator)
    {
        var response = new RepresentativeResponse(
            representative.MatriculaCode,
            representative.Name,
            representative.Role,
            representative.Region,
            representative.AvatarUrl,
            representative.LastSyncAtUtc);

        return new AuthResponse(tokenGenerator.Generate(representative), response);
    }
}
