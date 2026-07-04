using Josapar.Api.Infrastructure.Persistence.Entities;

namespace Josapar.Api.Features.Dashboard;

public record MonthlySalesPointResponse(string MonthLabel, decimal Amount);

public record RecentOrderResponse(
    string Code,
    string ClientName,
    decimal Amount,
    DateTime CreatedAtUtc,
    OrderStatus Status);

public record DashboardSummaryResponse(
    string GreetingName,
    int ScheduledVisitsToday,
    int VisitsCompleted,
    int VisitsTotal,
    decimal SalesToday,
    decimal SalesTodayGrowthPercent,
    decimal MonthlyGoalAchieved,
    decimal MonthlyGoalTarget,
    decimal MonthlyGoalPercent,
    IReadOnlyList<MonthlySalesPointResponse> MonthlySeries,
    IReadOnlyList<RecentOrderResponse> RecentOrders);
