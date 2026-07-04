namespace Josapar.Api.Features.Reports;

public enum ReportPeriod
{
    Today,
    Week,
    Month,
    Quarter,
}

public enum TrendDirection
{
    Up,
    Down,
}

public record ReportKpiResponse(string Label, decimal Value, decimal GrowthPercent);

public record SalesTrendPointResponse(string MonthLabel, decimal Sales, decimal Target);

public record TopProductResponse(string Name, decimal Amount);

public record RegionMixResponse(string Label, decimal Percent);

public record TopClientResponse(string Name, decimal Volume, TrendDirection Trend);

public record ReportsSummaryResponse(
    ReportKpiResponse TotalSales,
    ReportKpiResponse ActiveClients,
    ReportKpiResponse AverageTicket,
    ReportKpiResponse GoalAchievement,
    IReadOnlyList<SalesTrendPointResponse> SalesTrend,
    IReadOnlyList<TopProductResponse> TopProducts,
    IReadOnlyList<RegionMixResponse> RegionMix,
    IReadOnlyList<TopClientResponse> TopClients,
    string? Insight);
