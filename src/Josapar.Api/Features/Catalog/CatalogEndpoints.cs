using System.Text.Json;
using Josapar.Api.Infrastructure.Persistence;
using Josapar.Api.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Josapar.Api.Features.Catalog;

public static class CatalogEndpoints
{
    public static RouteGroupBuilder MapCatalogEndpoints(this RouteGroupBuilder app)
    {
        app.MapGet("/", ListProductsAsync)
            .WithSummary("Listar produtos")
            .WithDescription("Retorna o catálogo completo de produtos disponíveis.");
        app.MapGet("/{id:guid}", GetProductDetailAsync)
            .WithSummary("Detalhe do produto")
            .WithDescription("Retorna imagens, promoções, especificações técnicas e estoque por depósito de um produto.");

        return app;
    }

    private static async Task<IResult> ListProductsAsync(AppDbContext db)
    {
        var products = await db.Products.AsNoTracking().ToListAsync();
        return Results.Ok(products.Select(ToResponse));
    }

    private static async Task<IResult> GetProductDetailAsync(Guid id, AppDbContext db)
    {
        var product = await db.Products
            .AsNoTracking()
            .Include(p => p.StockByWarehouse)
            .SingleOrDefaultAsync(p => p.Id == id);

        if (product is null) return Results.NotFound();

        var response = new ProductDetailResponse(
            ToResponse(product),
            JsonSerializer.Deserialize<List<string>>(product.ImagesJson) ?? [],
            JsonSerializer.Deserialize<List<string>>(product.AppliedPromotionsJson) ?? [],
            product.StockByWarehouse
                .Select(s => new WarehouseStockResponse(s.WarehouseName, s.State, s.BundlesAvailable, s.Level))
                .ToList(),
            product.CommercialDescription,
            JsonSerializer.Deserialize<Dictionary<string, string>>(product.TechnicalSpecsJson) ?? []);

        return Results.Ok(response);
    }

    private static ProductResponse ToResponse(Product product) => new(
        product.Id.ToString(),
        product.Sku,
        product.Brand,
        product.Name,
        product.Category,
        product.ImageUrl,
        product.Price,
        product.AvailableUnits,
        product.OriginalPrice,
        product.Badge);
}
