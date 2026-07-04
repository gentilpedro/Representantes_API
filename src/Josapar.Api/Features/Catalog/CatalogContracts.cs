using Josapar.Api.Infrastructure.Persistence.Entities;

namespace Josapar.Api.Features.Catalog;

public record ProductResponse(
    string Id,
    string Sku,
    string Brand,
    string Name,
    string Category,
    string ImageUrl,
    decimal Price,
    int AvailableUnits,
    decimal? OriginalPrice,
    ProductBadge Badge);

public record WarehouseStockResponse(
    string WarehouseName,
    string State,
    int BundlesAvailable,
    StockLevel Level);

public record ProductDetailResponse(
    ProductResponse Product,
    IReadOnlyList<string> Images,
    IReadOnlyList<string> AppliedPromotions,
    IReadOnlyList<WarehouseStockResponse> StockByWarehouse,
    string CommercialDescription,
    IReadOnlyDictionary<string, string> TechnicalSpecs);
