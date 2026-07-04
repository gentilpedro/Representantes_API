namespace Josapar.Api.Infrastructure.Persistence.Entities;

public class Product
{
    public Guid Id { get; set; }
    public required string Sku { get; set; }
    public required string Brand { get; set; }
    public required string Name { get; set; }
    public required string Category { get; set; }
    public required string ImageUrl { get; set; }
    public decimal Price { get; set; }
    public int AvailableUnits { get; set; }
    public decimal? OriginalPrice { get; set; }
    public ProductBadge Badge { get; set; }
    public required string CommercialDescription { get; set; }

    /// <summary>Dictionary&lt;string,string&gt; serializado (specs técnicas).</summary>
    public string TechnicalSpecsJson { get; set; } = "{}";

    /// <summary>List&lt;string&gt; serializada (galeria de imagens).</summary>
    public string ImagesJson { get; set; } = "[]";

    /// <summary>List&lt;string&gt; serializada (promoções aplicáveis).</summary>
    public string AppliedPromotionsJson { get; set; } = "[]";

    public DateTime CreatedAtUtc { get; set; }

    public List<WarehouseStock> StockByWarehouse { get; set; } = [];
}
