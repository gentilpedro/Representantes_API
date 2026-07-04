namespace Josapar.Api.Infrastructure.Persistence.Entities;

public class DeliveryAddress
{
    public required string Street { get; set; }
    public required string District { get; set; }
    public required string City { get; set; }
    public required string State { get; set; }
}

public class Client
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string Cnpj { get; set; }
    public ClientTier Tier { get; set; }
    public required string Phone { get; set; }
    public required string Mobile { get; set; }
    public required string Email { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal CreditUsedPercent { get; set; }
    public required DeliveryAddress DeliveryAddress { get; set; }
    public decimal? PendingInvoiceAmount { get; set; }
    public DateTime? PendingInvoiceDueDateUtc { get; set; }
    public string? Notes { get; set; }
    public bool IsFavorite { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
