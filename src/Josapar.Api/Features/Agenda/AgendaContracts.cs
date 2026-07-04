using Josapar.Api.Infrastructure.Persistence.Entities;

namespace Josapar.Api.Features.Agenda;

public record VisitResponse(
    string Id,
    string ClientId,
    string ClientName,
    string ClientAddress,
    DateTime ScheduledAtUtc,
    VisitStatus Status,
    string? Notes,
    bool IsGeoValidated,
    DateTime? CheckInAtUtc,
    DateTime? CheckOutAtUtc);

public record DailyAgendaResponse(DateOnly Date, int VisitsPlanned, IReadOnlyList<VisitResponse> Visits);

public record CreateVisitRequest(Guid ClientId, DateTime ScheduledAtUtc, string? Notes);

public record CheckInRequest(double? Latitude, double? Longitude);

public record CheckOutRequest(string? Notes);
