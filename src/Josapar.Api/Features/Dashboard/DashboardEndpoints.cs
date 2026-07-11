using System.Security.Claims;
using Josapar.Api.Infrastructure.Auth;
using Josapar.Api.Infrastructure.Persistence;
using Josapar.Api.Infrastructure.Persistence.Entities;
using Josapar.Api.Infrastructure.Reporting;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Josapar.Api.Features.Dashboard;

public static class DashboardEndpoints
{
    public static RouteGroupBuilder MapDashboardEndpoints(this RouteGroupBuilder app)
    {
        app.MapGet("/summary", GetSummaryAsync)
            .WithSummary("Resumo do dashboard")
            .WithDescription("Retorna os indicadores de desempenho do representante autenticado (vendas, metas, etc.).");
        return app;
    }

    private static async Task<IResult> GetSummaryAsync(ClaimsPrincipal user, AppDbContext db)
    {
        var representativeId = user.GetRepresentativeId();
        var representative = await db.Representatives.AsNoTracking().SingleAsync(r => r.Id == representativeId);

        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var todayEnd = todayStart.AddDays(1);
        var yesterdayStart = todayStart.AddDays(-1);

        var salesToday = await SalesAggregationHelper.SumOrdersAsync(db, representativeId, todayStart, todayEnd);
        var salesYesterday = await SalesAggregationHelper.SumOrdersAsync(db, representativeId, yesterdayStart, todayStart);
        var salesGrowthPercent = salesYesterday == 0 ? 0 : (salesToday - salesYesterday) / salesYesterday * 100;

        var visitsToday = await db.Visits
            .AsNoTracking()
            .Where(v => v.RepresentativeId == representativeId
                && v.ScheduledAtUtc >= todayStart
                && v.ScheduledAtUtc < todayEnd)
            .ToListAsync();
        var visitsCompleted = visitsToday.Count(v => v.Status == VisitStatus.Completed);

        // `new DateTime(y, m, 1)` vem com Kind=Unspecified — o Npgsql rejeita
        // comparar isso com a coluna timestamptz `CreatedAtUtc`.
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthlyGoalAchieved = await SalesAggregationHelper.SumOrdersAsync(db, representativeId, monthStart, monthStart.AddMonths(1));
        var goal = await db.RepresentativeGoals
            .AsNoTracking()
            .SingleOrDefaultAsync(g => g.RepresentativeId == representativeId && g.Year == now.Year && g.Month == now.Month);
        var monthlyGoalTarget = goal?.TargetAmount ?? 0;
        // Fração 0-1 (não *100) — o client formata como percentual na exibição.
        var monthlyGoalPercent = monthlyGoalTarget == 0 ? 0 : monthlyGoalAchieved / monthlyGoalTarget;

        var monthlySeries = await SalesAggregationHelper.GetTrailingMonthlySalesAsync(db, representativeId, monthStart, 6);

        var recentOrders = await db.Orders
            .AsNoTracking()
            .Include(o => o.Client)
            .Where(o => o.RepresentativeId == representativeId)
            .OrderByDescending(o => o.CreatedAtUtc)
            .Take(5)
            .ToListAsync();

        var response = new DashboardSummaryResponse(
            representative.Name,
            visitsToday.Count,
            visitsCompleted,
            visitsToday.Count,
            salesToday,
            salesGrowthPercent,
            monthlyGoalAchieved,
            monthlyGoalTarget,
            monthlyGoalPercent,
            monthlySeries.Select(m => new MonthlySalesPointResponse(SalesAggregationHelper.MonthLabel(m.Month), m.Sales)).ToList(),
            recentOrders.Select(o => new RecentOrderResponse(
                o.Code,
                o.Client?.Name ?? string.Empty,
                o.Total,
                o.CreatedAtUtc,
                o.Status)).ToList());

        return Results.Ok(response);
    }
}
