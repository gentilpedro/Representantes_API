namespace Josapar.Api.Infrastructure.Persistence.Entities;

public class WarehouseStock
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public required string WarehouseName { get; set; }
    public required string State { get; set; }
    public int BundlesAvailable { get; set; }
    public StockLevel Level { get; set; }
}
