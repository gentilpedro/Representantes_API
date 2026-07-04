namespace Josapar.Api.Infrastructure.Persistence.Entities;

public class Order
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public Guid ClientId { get; set; }
    public Guid RepresentativeId { get; set; }

    /// <summary>Guid gerado pelo app ao criar o pedido offline — chave de idempotência do sync.</summary>
    public Guid? ClientGeneratedId { get; set; }

    public string? Notes { get; set; }
    public OrderStatus Status { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountsTotal { get; set; }
    public decimal TaxesTotal { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? SyncedAtUtc { get; set; }

    public Client? Client { get; set; }
    public List<OrderItem> Items { get; set; } = [];
}
