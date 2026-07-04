namespace Josapar.Api.Infrastructure.Persistence.Entities;

public class Lead
{
    public Guid Id { get; set; }
    public Guid RepresentativeId { get; set; }
    public required string ContactName { get; set; }
    public required string CompanyName { get; set; }
    public string? Cnpj { get; set; }
    public required string Phone { get; set; }
    public string? Email { get; set; }
    public LeadStatus Status { get; set; }
    public string? Source { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastContactAtUtc { get; set; }
}
