using System.Security.Claims;
using Josapar.Api.Infrastructure.Auth;
using Josapar.Api.Infrastructure.Persistence;
using Josapar.Api.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Josapar.Api.Features.Agenda;

public static class AgendaEndpoints
{
    public static RouteGroupBuilder MapAgendaEndpoints(this RouteGroupBuilder app)
    {
        app.MapGet("/", GetDailyAgendaAsync);
        return app;
    }

    public static RouteGroupBuilder MapVisitEndpoints(this RouteGroupBuilder app)
    {
        app.MapPost("/", CreateVisitAsync);
        app.MapPost("/{id:guid}/check-in", CheckInAsync);
        app.MapPost("/{id:guid}/check-out", CheckOutAsync);
        return app;
    }

    private static async Task<IResult> GetDailyAgendaAsync(DateOnly? date, ClaimsPrincipal user, AppDbContext db)
    {
        var representativeId = user.GetRepresentativeId();
        var day = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var startOfDay = day.ToDateTime(TimeOnly.MinValue);
        var endOfDay = startOfDay.AddDays(1);

        var visits = await db.Visits
            .AsNoTracking()
            .Include(v => v.Client)
            .Where(v => v.RepresentativeId == representativeId
                && v.ScheduledAtUtc >= startOfDay
                && v.ScheduledAtUtc < endOfDay)
            .OrderBy(v => v.ScheduledAtUtc)
            .ToListAsync();

        var response = new DailyAgendaResponse(day, visits.Count, visits.Select(ToResponse).ToList());
        return Results.Ok(response);
    }

    private static async Task<IResult> CreateVisitAsync(CreateVisitRequest request, ClaimsPrincipal user, AppDbContext db)
    {
        var client = await db.Clients.SingleOrDefaultAsync(c => c.Id == request.ClientId);
        if (client is null) return Results.NotFound("Cliente não encontrado.");

        var visit = new Visit
        {
            Id = Guid.NewGuid(),
            ClientId = client.Id,
            RepresentativeId = user.GetRepresentativeId(),
            ScheduledAtUtc = request.ScheduledAtUtc,
            Status = VisitStatus.Pending,
            Notes = request.Notes,
            IsGeoValidated = false,
            CreatedAtUtc = DateTime.UtcNow,
        };

        db.Visits.Add(visit);
        await db.SaveChangesAsync();

        visit.Client = client;
        return Results.Created($"/api/visits/{visit.Id}", ToResponse(visit));
    }

    private static async Task<IResult> CheckInAsync(Guid id, CheckInRequest request, ClaimsPrincipal user, AppDbContext db)
    {
        var visit = await FindOwnedVisitAsync(db, id, user);
        if (visit is null) return Results.NotFound();
        if (visit.Status != VisitStatus.Pending)
        {
            return Results.Conflict("Visita já teve check-in ou foi concluída.");
        }

        visit.Status = VisitStatus.InProgress;
        visit.CheckInAtUtc = DateTime.UtcNow;
        visit.CheckInLatitude = request.Latitude;
        visit.CheckInLongitude = request.Longitude;
        visit.IsGeoValidated = request.Latitude is not null && request.Longitude is not null;

        await db.SaveChangesAsync();
        return Results.Ok(ToResponse(visit));
    }

    private static async Task<IResult> CheckOutAsync(Guid id, CheckOutRequest request, ClaimsPrincipal user, AppDbContext db)
    {
        var visit = await FindOwnedVisitAsync(db, id, user);
        if (visit is null) return Results.NotFound();
        if (visit.Status != VisitStatus.InProgress)
        {
            return Results.Conflict("Visita precisa estar em andamento para dar check-out.");
        }

        visit.Status = VisitStatus.Completed;
        visit.CheckOutAtUtc = DateTime.UtcNow;
        if (request.Notes is not null) visit.Notes = request.Notes;

        await db.SaveChangesAsync();
        return Results.Ok(ToResponse(visit));
    }

    private static Task<Visit?> FindOwnedVisitAsync(AppDbContext db, Guid id, ClaimsPrincipal user) =>
        db.Visits
            .Include(v => v.Client)
            .SingleOrDefaultAsync(v => v.Id == id && v.RepresentativeId == user.GetRepresentativeId());

    private static VisitResponse ToResponse(Visit visit) => new(
        visit.Id.ToString(),
        visit.ClientId.ToString(),
        visit.Client?.Name ?? string.Empty,
        visit.Client is null
            ? string.Empty
            : $"{visit.Client.DeliveryAddress.Street}, {visit.Client.DeliveryAddress.District} - {visit.Client.DeliveryAddress.City}/{visit.Client.DeliveryAddress.State}",
        visit.ScheduledAtUtc,
        visit.Status,
        visit.Notes,
        visit.IsGeoValidated,
        visit.CheckInAtUtc,
        visit.CheckOutAtUtc);
}
