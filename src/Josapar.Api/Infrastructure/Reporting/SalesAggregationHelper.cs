using Josapar.Api.Infrastructure.Persistence;
using Josapar.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Josapar.Api.Infrastructure.Reporting;

public static class SalesAggregationHelper
{
    private static readonly string[] PtMonthAbbreviations =
        ["Jan", "Fev", "Mar", "Abr", "Mai", "Jun", "Jul", "Ago", "Set", "Out", "Nov", "Dez"];

    public static string MonthLabel(DateTime month) => PtMonthAbbreviations[month.Month - 1];

    public static async Task<decimal> SumOrdersAsync(
        AppDbContext db, Guid representativeId, DateTime startInclusive, DateTime endExclusive)
    {
        // SumAsync sobre uma coluna decimal não-anulável quebra quando não há
        // nenhuma linha (o SQL SUM(...) retorna NULL, que o EF/Npgsql não
        // consegue materializar em `decimal`) — somamos como `decimal?` e
        // convertemos o "sem pedidos no período" pra 0 explicitamente.
        return await db.Orders
            .Where(o => o.RepresentativeId == representativeId
                && o.Status != OrderStatus.Draft
                && o.CreatedAtUtc >= startInclusive
                && o.CreatedAtUtc < endExclusive)
            .SumAsync(o => (decimal?)o.Total) ?? 0m;
    }

    /// <summary>Últimos <paramref name="monthCount"/> meses (incluindo o mês de <paramref name="referenceMonthStart"/>), mais antigo primeiro.</summary>
    public static async Task<List<(DateTime Month, decimal Sales)>> GetTrailingMonthlySalesAsync(
        AppDbContext db, Guid representativeId, DateTime referenceMonthStart, int monthCount)
    {
        var result = new List<(DateTime Month, decimal Sales)>();
        for (var i = monthCount - 1; i >= 0; i--)
        {
            var month = referenceMonthStart.AddMonths(-i);
            var sales = await SumOrdersAsync(db, representativeId, month, month.AddMonths(1));
            result.Add((month, sales));
        }

        return result;
    }
}
