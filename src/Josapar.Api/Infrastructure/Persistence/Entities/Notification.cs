namespace Josapar.Api.Infrastructure.Persistence.Entities;

public class Notification
{
    public Guid Id { get; set; }
    public Guid RepresentativeId { get; set; }
    public NotificationCategory Category { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public bool IsUrgent { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
