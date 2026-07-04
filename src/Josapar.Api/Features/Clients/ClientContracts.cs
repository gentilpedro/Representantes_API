using Josapar.Api.Infrastructure.Persistence.Entities;

namespace Josapar.Api.Features.Clients;

public record ClientListItemResponse(
    string Id,
    string Code,
    string Name,
    string Cnpj,
    string City,
    string State,
    ClientTier Tier,
    DateTime? LastOrderAtUtc,
    decimal CreditLimit,
    bool IsFavorite);

public record DeliveryAddressResponse(string Street, string District, string City, string State);

public record PendingInvoiceResponse(DateTime DueDateUtc, decimal Amount);

public record ClientOrderHistoryItemResponse(
    string Code,
    DateTime CreatedAtUtc,
    decimal Total,
    OrderStatus Status);

public record ClientDetailResponse(
    string Id,
    string Code,
    string Name,
    string Cnpj,
    ClientTier Tier,
    string Phone,
    string Mobile,
    string Email,
    decimal CreditLimit,
    decimal CreditUsedPercent,
    decimal CreditAvailable,
    DeliveryAddressResponse DeliveryAddress,
    PendingInvoiceResponse? PendingInvoice,
    string? Notes,
    bool IsFavorite,
    IReadOnlyList<ClientOrderHistoryItemResponse> OrderHistory);

public record FavoriteToggleRequest(bool IsFavorite);
