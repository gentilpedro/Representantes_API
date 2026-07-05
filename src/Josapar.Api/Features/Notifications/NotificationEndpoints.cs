using System.Security.Claims;
using Josapar.Api.Infrastructure.Auth;
using Josapar.Api.Infrastructure.Persistence;
using Josapar.Api.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Josapar.Api.Features.Notifications;

public static class NotificationEndpoints
{
    public static RouteGroupBuilder MapNotificationEndpoints(this RouteGroupBuilder app)
    {
        app.MapGet("/", ListNotificationsAsync)
            .WithSummary("Listar notificações")
            .WithDescription("Lista as notificações do representante autenticado.");
        app.MapPost("/{id:guid}/read", MarkAsReadAsync)
            .WithSummary("Marcar como lida")
            .WithDescription("Marca uma notificação específica como lida.");
        app.MapPost("/read-all", MarkAllAsReadAsync)
            .WithSummary("Marcar todas como lidas")
            .WithDescription("Marca todas as notificações do representante autenticado como lidas.");
        return app;
    }

    private static async Task<IResult> ListNotificationsAsync(ClaimsPrincipal user, AppDbContext db)
    {
        var representativeId = user.GetRepresentativeId();

        var notifications = await db.Notifications
            .AsNoTracking()
            .Where(n => n.RepresentativeId == representativeId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .ToListAsync();

        return Results.Ok(notifications.Select(ToResponse));
    }

    private static async Task<IResult> MarkAsReadAsync(Guid id, ClaimsPrincipal user, AppDbContext db)
    {
        var notification = await db.Notifications
            .SingleOrDefaultAsync(n => n.Id == id && n.RepresentativeId == user.GetRepresentativeId());
        if (notification is null) return Results.NotFound();

        notification.IsRead = true;
        await db.SaveChangesAsync();

        return Results.Ok(ToResponse(notification));
    }

    private static async Task<IResult> MarkAllAsReadAsync(ClaimsPrincipal user, AppDbContext db)
    {
        var representativeId = user.GetRepresentativeId();

        var unread = await db.Notifications
            .Where(n => n.RepresentativeId == representativeId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in unread) notification.IsRead = true;
        await db.SaveChangesAsync();

        return Results.Ok(new MarkAllReadResponse(unread.Count));
    }

    private static NotificationResponse ToResponse(Notification notification) => new(
        notification.Id.ToString(),
        notification.Category,
        notification.Title,
        notification.Message,
        notification.IsUrgent,
        notification.IsRead,
        notification.CreatedAtUtc >= DateTime.UtcNow.AddHours(-24),
        notification.CreatedAtUtc);
}
