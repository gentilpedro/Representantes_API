using System.Security.Claims;
using Josapar.Api.Infrastructure.Auth;
using Josapar.Api.Infrastructure.Persistence;
using Josapar.Api.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Josapar.Api.Features.Leads;

public static class LeadEndpoints
{
    public static RouteGroupBuilder MapLeadEndpoints(this RouteGroupBuilder app)
    {
        app.MapGet("/", ListLeadsAsync);
        app.MapGet("/{id:guid}", GetLeadAsync);
        app.MapPost("/", CreateLeadAsync);
        app.MapPatch("/{id:guid}", UpdateLeadAsync);

        return app;
    }

    private static async Task<IResult> ListLeadsAsync(ClaimsPrincipal user, AppDbContext db)
    {
        var representativeId = user.GetRepresentativeId();

        var leads = await db.Leads
            .AsNoTracking()
            .Where(l => l.RepresentativeId == representativeId)
            .OrderByDescending(l => l.CreatedAtUtc)
            .ToListAsync();

        return Results.Ok(leads.Select(ToResponse));
    }

    private static async Task<IResult> GetLeadAsync(Guid id, ClaimsPrincipal user, AppDbContext db)
    {
        var lead = await FindOwnedLeadAsync(db.Leads.AsNoTracking(), id, user);
        return lead is null ? Results.NotFound() : Results.Ok(ToResponse(lead));
    }

    private static async Task<IResult> CreateLeadAsync(
        CreateLeadRequest request,
        ClaimsPrincipal user,
        AppDbContext db)
    {
        var lead = new Lead
        {
            Id = Guid.NewGuid(),
            RepresentativeId = user.GetRepresentativeId(),
            ContactName = request.ContactName,
            CompanyName = request.CompanyName,
            Cnpj = request.Cnpj,
            Phone = request.Phone,
            Email = request.Email,
            Status = LeadStatus.New,
            Source = request.Source,
            Notes = request.Notes,
            CreatedAtUtc = DateTime.UtcNow,
        };

        db.Leads.Add(lead);
        await db.SaveChangesAsync();

        return Results.Created($"/api/leads/{lead.Id}", ToResponse(lead));
    }

    private static async Task<IResult> UpdateLeadAsync(
        Guid id,
        UpdateLeadRequest request,
        ClaimsPrincipal user,
        AppDbContext db)
    {
        var lead = await FindOwnedLeadAsync(db.Leads, id, user);
        if (lead is null) return Results.NotFound();

        lead.ContactName = request.ContactName;
        lead.CompanyName = request.CompanyName;
        lead.Cnpj = request.Cnpj;
        lead.Phone = request.Phone;
        lead.Email = request.Email;
        lead.Status = request.Status;
        lead.Source = request.Source;
        lead.Notes = request.Notes;
        lead.LastContactAtUtc = request.LastContactAtUtc;

        await db.SaveChangesAsync();

        return Results.Ok(ToResponse(lead));
    }

    private static Task<Lead?> FindOwnedLeadAsync(IQueryable<Lead> leads, Guid id, ClaimsPrincipal user) =>
        leads.SingleOrDefaultAsync(l => l.Id == id && l.RepresentativeId == user.GetRepresentativeId());

    private static LeadResponse ToResponse(Lead lead) => new(
        lead.Id.ToString(),
        lead.ContactName,
        lead.CompanyName,
        lead.Cnpj,
        lead.Phone,
        lead.Email,
        lead.Status,
        lead.Source,
        lead.Notes,
        lead.CreatedAtUtc,
        lead.LastContactAtUtc);
}
