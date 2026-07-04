namespace Josapar.Api.Infrastructure.Persistence.Entities;

public class RepresentativeGoal
{
    public Guid Id { get; set; }
    public Guid RepresentativeId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TargetAmount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
