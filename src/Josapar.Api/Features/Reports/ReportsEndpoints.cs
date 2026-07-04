using System.Security.Claims;
using Josapar.Api.Infrastructure.Auth;
using Josapar.Api.Infrastructure.Persistence;
using Josapar.Api.Infrastructure.Persistence.Entities;
using Josapar.Api.Infrastructure.Reporting;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Josapar.Api.Features.Reports;

public static class ReportsEndpoints
{
    public static RouteGroupBuilder MapReportsEndpoints(this RouteGroupBuilder app)
    {
        app.MapGet("/summary", GetSummaryAsync);
        return app;
    }

    private static async Task<IResult> GetSummaryAsync(string? period, ClaimsPrincipal user, AppDbContext db)
    {
        var representativeId = user.GetRepresentativeId();
        Enum.TryParse<ReportPeriod>(period, ignoreCase: true, out var reportPeriod);

        var now = DateTime.UtcNow;
        var (start, end) = GetPeriodRange(now, reportPeriod);
        var length = end - start;
        var prevStart = start - length;
        var prevEnd = start;

        var totalSales = await SalesAggregationHelper.SumOrdersAsync(db, representativeId, start, end);
        var totalSalesPrev = await SalesAggregationHelper.SumOrdersAsync(db, representativeId, prevStart, prevEnd);

        var ordersCount = await db.Orders.CountAsync(o => o.RepresentativeId == representativeId
            && o.Status != OrderStatus.Draft && o.CreatedAtUtc >= start && o.CreatedAtUtc < end);
        var ordersCountPrev = await db.Orders.CountAsync(o => o.RepresentativeId == representativeId
            && o.Status != OrderStatus.Draft && o.CreatedAtUtc >= prevStart && o.CreatedAtUtc < prevEnd);

        var activeClients = await db.Orders
            .Where(o => o.RepresentativeId == representativeId && o.Status != OrderStatus.Draft
                && o.CreatedAtUtc >= start && o.CreatedAtUtc < end)
            .Select(o => o.ClientId).Distinct().CountAsync();
        var activeClientsPrev = await db.Orders
            .Where(o => o.RepresentativeId == representativeId && o.Status != OrderStatus.Draft
                && o.CreatedAtUtc >= prevStart && o.CreatedAtUtc < prevEnd)
            .Select(o => o.ClientId).Distinct().CountAsync();

        var averageTicket = ordersCount == 0 ? 0 : totalSales / ordersCount;
        var averageTicketPrev = ordersCountPrev == 0 ? 0 : totalSalesPrev / ordersCountPrev;

        var monthStart = new DateTime(now.Year, now.Month, 1);
        var lastMonthStart = monthStart.AddMonths(-1);
        var goalAchievementPercent = await GetGoalAchievementPercentAsync(db, representativeId, monthStart);
        var goalAchievementPercentPrev = await GetGoalAchievementPercentAsync(db, representativeId, lastMonthStart);

        var monthlySales = await SalesAggregationHelper.GetTrailingMonthlySalesAsync(db, representativeId, monthStart, 6);
        var goals = await db.RepresentativeGoals
            .AsNoTracking()
            .Where(g => g.RepresentativeId == representativeId)
            .ToListAsync();
        var salesTrend = monthlySales.Select(m => new SalesTrendPointResponse(
            SalesAggregationHelper.MonthLabel(m.Month),
            m.Sales,
            goals.SingleOrDefault(g => g.Year == m.Month.Year && g.Month == m.Month.Month)?.TargetAmount ?? 0)).ToList();

        var topProducts = (await db.Orders
            .Where(o => o.RepresentativeId == representativeId && o.Status != OrderStatus.Draft
                && o.CreatedAtUtc >= start && o.CreatedAtUtc < end)
            .SelectMany(o => o.Items)
            .GroupBy(i => i.ProductNameSnapshot)
            .Select(g => new { Name = g.Key, Amount = g.Sum(i => i.Subtotal) })
            .OrderByDescending(x => x.Amount)
            .Take(5)
            .ToListAsync())
            .Select(x => new TopProductResponse(x.Name, x.Amount))
            .ToList();

        var regionMix = await GetRegionMixAsync(db, representativeId, start, end, totalSales);
        var topClients = await GetTopClientsAsync(db, representativeId, start, end, prevStart, prevEnd);

        var response = new ReportsSummaryResponse(
            new ReportKpiResponse("Vendas Totais", totalSales, GrowthPercent(totalSales, totalSalesPrev)),
            new ReportKpiResponse("Clientes Ativos", activeClients, GrowthPercent(activeClients, activeClientsPrev)),
            new ReportKpiResponse("Ticket Médio", averageTicket, GrowthPercent(averageTicket, averageTicketPrev)),
            new ReportKpiResponse("Meta Atingida", goalAchievementPercent, goalAchievementPercent - goalAchievementPercentPrev),
            salesTrend,
            topProducts,
            regionMix,
            topClients,
            null);

        return Results.Ok(response);
    }

    private static async Task<decimal> GetGoalAchievementPercentAsync(AppDbContext db, Guid representativeId, DateTime monthStart)
    {
        var achieved = await SalesAggregationHelper.SumOrdersAsync(db, representativeId, monthStart, monthStart.AddMonths(1));
        var goal = await db.RepresentativeGoals.AsNoTracking()
            .SingleOrDefaultAsync(g => g.RepresentativeId == representativeId && g.Year == monthStart.Year && g.Month == monthStart.Month);
        var target = goal?.TargetAmount ?? 0;
        return target == 0 ? 0 : achieved / target * 100;
    }

    private static async Task<List<RegionMixResponse>> GetRegionMixAsync(
        AppDbContext db, Guid representativeId, DateTime start, DateTime end, decimal totalSales)
    {
        var byRegion = await db.Orders
            .Where(o => o.RepresentativeId == representativeId && o.Status != OrderStatus.Draft
                && o.CreatedAtUtc >= start && o.CreatedAtUtc < end)
            .GroupBy(o => o.Client!.DeliveryAddress.State)
            .Select(g => new { State = g.Key, Amount = g.Sum(o => o.Total) })
            .ToListAsync();

        if (totalSales == 0) return [];

        return byRegion
            .Select(r => new RegionMixResponse(r.State, r.Amount / totalSales * 100))
            .OrderByDescending(r => r.Percent)
            .ToList();
    }

    private static async Task<List<TopClientResponse>> GetTopClientsAsync(
        AppDbContext db, Guid representativeId, DateTime start, DateTime end, DateTime prevStart, DateTime prevEnd)
    {
        var current = await db.Orders
            .Where(o => o.RepresentativeId == representativeId && o.Status != OrderStatus.Draft
                && o.CreatedAtUtc >= start && o.CreatedAtUtc < end)
            .GroupBy(o => o.ClientId)
            .Select(g => new { ClientId = g.Key, Volume = g.Sum(o => o.Total) })
            .OrderByDescending(x => x.Volume)
            .Take(5)
            .ToListAsync();

        var clientIds = current.Select(c => c.ClientId).ToList();

        var previous = await db.Orders
            .Where(o => o.RepresentativeId == representativeId && o.Status != OrderStatus.Draft
                && clientIds.Contains(o.ClientId) && o.CreatedAtUtc >= prevStart && o.CreatedAtUtc < prevEnd)
            .GroupBy(o => o.ClientId)
            .Select(g => new { ClientId = g.Key, Volume = g.Sum(o => o.Total) })
            .ToDictionaryAsync(x => x.ClientId, x => x.Volume);

        var names = await db.Clients
            .Where(c => clientIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name);

        return current.Select(c => new TopClientResponse(
            names.GetValueOrDefault(c.ClientId, string.Empty),
            c.Volume,
            c.Volume >= previous.GetValueOrDefault(c.ClientId, 0) ? TrendDirection.Up : TrendDirection.Down)).ToList();
    }

    private static decimal GrowthPercent(decimal current, decimal previous) =>
        previous == 0 ? 0 : (current - previous) / previous * 100;

    private static (DateTime Start, DateTime End) GetPeriodRange(DateTime nowUtc, ReportPeriod period)
    {
        var todayStart = nowUtc.Date;
        var monthStart = new DateTime(nowUtc.Year, nowUtc.Month, 1);

        return period switch
        {
            ReportPeriod.Today => (todayStart, todayStart.AddDays(1)),
            ReportPeriod.Week => GetWeekRange(todayStart),
            ReportPeriod.Quarter => (monthStart.AddMonths(-2), monthStart.AddMonths(1)),
            _ => (monthStart, monthStart.AddMonths(1)),
        };
    }

    private static (DateTime Start, DateTime End) GetWeekRange(DateTime todayStart)
    {
        var daysSinceMonday = ((int)todayStart.DayOfWeek + 6) % 7;
        var monday = todayStart.AddDays(-daysSinceMonday);
        return (monday, monday.AddDays(7));
    }
}
