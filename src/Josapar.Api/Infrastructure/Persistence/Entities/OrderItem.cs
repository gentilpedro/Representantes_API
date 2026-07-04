namespace Josapar.Api.Infrastructure.Persistence.Entities;

public class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }

    /// <summary>Snapshot no momento da compra — histórico não muda se o produto mudar depois.</summary>
    public required string ProductNameSnapshot { get; set; }
    public required string ProductSkuSnapshot { get; set; }
    public decimal UnitPriceSnapshot { get; set; }

    public int Quantity { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal Subtotal { get; set; }
}
