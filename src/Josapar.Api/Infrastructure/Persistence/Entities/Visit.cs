namespace Josapar.Api.Infrastructure.Persistence.Entities;

public class Visit
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public Guid RepresentativeId { get; set; }
    public DateTime ScheduledAtUtc { get; set; }
    public VisitStatus Status { get; set; }
    public string? Notes { get; set; }

    public DateTime? CheckInAtUtc { get; set; }
    public double? CheckInLatitude { get; set; }
    public double? CheckInLongitude { get; set; }
    public bool IsGeoValidated { get; set; }

    public DateTime? CheckOutAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public Client? Client { get; set; }
}
