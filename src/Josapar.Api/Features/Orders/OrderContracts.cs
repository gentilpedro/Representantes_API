using Josapar.Api.Infrastructure.Persistence.Entities;

namespace Josapar.Api.Features.Orders;

public record CreateOrderItemRequest(Guid ProductId, int Quantity, decimal DiscountPercent);

public record CreateOrderRequest(
    Guid ClientId,
    string? Notes,
    bool IsDraft,
    IReadOnlyList<CreateOrderItemRequest> Items,
    Guid? ClientGeneratedId = null);

public record BatchSyncOrderRequest(
    Guid ClientGeneratedId,
    Guid ClientId,
    string? Notes,
    IReadOnlyList<CreateOrderItemRequest> Items);

public record OrderItemResponse(
    string ProductId,
    string ProductName,
    string ProductSku,
    decimal UnitPrice,
    int Quantity,
    decimal DiscountPercent,
    decimal Subtotal);

public record OrderResponse(
    string Id,
    string Code,
    string ClientId,
    string ClientName,
    string? Notes,
    OrderStatus Status,
    decimal Subtotal,
    decimal DiscountsTotal,
    decimal TaxesTotal,
    decimal Total,
    DateTime CreatedAtUtc,
    DateTime? SyncedAtUtc,
    IReadOnlyList<OrderItemResponse> Items);

public record BatchSyncResultResponse(
    IReadOnlyList<OrderResponse> Created,
    IReadOnlyList<OrderResponse> AlreadySynced);
