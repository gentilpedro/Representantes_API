using Josapar.Api.Infrastructure.Persistence.Entities;

namespace Josapar.Api.Features.Notifications;

public record NotificationResponse(
    string Id,
    NotificationCategory Category,
    string Title,
    string Message,
    bool IsUrgent,
    bool IsRead,
    bool IsRecent,
    DateTime CreatedAtUtc);

public record MarkAllReadResponse(int UpdatedCount);
