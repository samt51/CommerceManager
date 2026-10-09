using MediatR;

namespace CommerceManager.Application.Catalog;

public sealed record CatalogAllocationRow(
    int Channel,
    int Quantity);

public sealed record TrendyolListingRow(
    string Barcode,
    int Units,
    int? CardQuantity,
    decimal? ListPrice);

public sealed record CatalogBarcodeLink(
    int Channel,
    string Barcode,
    int Units);

public sealed record CatalogProductRow(
    Guid Id,
    string Name,
    string? Description,
    decimal SalePrice,
    string? ImageUrl,
    int Quantity,
    string? TrendyolBarcode,
    string? WebsiteBarcode,
    int RetailQuantity,
    IReadOnlyList<CatalogAllocationRow> Allocations,
    IReadOnlyList<CatalogBarcodeLink> Barcodes,
    DateTimeOffset CreatedAt);

public sealed record CatalogProductPage(
    int Page,
    int Size,
    int Total,
    int TotalQuantity,
    IReadOnlyList<CatalogProductRow> Items);

public interface ICatalogProductReader
{
    Task<CatalogProductPage> ListAsync(
        int page,
        int size,
        string? search,
        CancellationToken cancellationToken);
}

public sealed record ListCatalogProducts(
    int Page = 0,
    int Size = 25,
    string? Search = null) : IRequest<CatalogProductPage>;

public sealed class ListCatalogProductsHandler(ICatalogProductReader reader)
    : IRequestHandler<ListCatalogProducts, CatalogProductPage>
{
    public Task<CatalogProductPage> Handle(
        ListCatalogProducts request,
        CancellationToken cancellationToken) =>
        reader.ListAsync(
            request.Page,
            request.Size,
            request.Search,
            cancellationToken);
}

public sealed record CreateCatalogProductResult(
    bool Success,
    string? Error,
    Guid? Id);

public sealed record SetTrendyolStockResult(
    bool Success,
    string? Error,
    string? Barcode);

public sealed record UpdateCatalogProductResult(
    bool Success,
    string? Error,
    string? TrendyolBarcode,
    IReadOnlyList<CatalogBarcodeLink>? TrendyolBarcodes = null,
    decimal? PreviousTrendyolListPrice = null);

public interface ICatalogProductStore
{
    Task<UpdateCatalogProductResult> UpdateAsync(
        Guid productId,
        string name,
        string? description,
        decimal salePrice,
        string? imageUrl,
        int quantity,
        int trendyolQuantity,
        int websiteQuantity,
        IReadOnlyList<CatalogBarcodeLink> barcodes,
        decimal? trendyolListPrice,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TrendyolListingRow>> TrendyolListingsAsync(
        Guid productId,
        CancellationToken cancellationToken);

    Task<SetTrendyolStockResult> SetTrendyolStockAsync(
        Guid productId,
        int quantity,
        CancellationToken cancellationToken);

    Task<CreateCatalogProductResult> AddAsync(
        string name,
        int quantity,
        decimal salePrice,
        string? description,
        string? imageUrl,
        string? trendyolBarcode,
        string? websiteBarcode,
        int trendyolUnits,
        int websiteUnits,
        IReadOnlyList<CatalogAllocationRow> allocations,
        IReadOnlyList<CatalogBarcodeLink>? extraBarcodes,
        CancellationToken cancellationToken);
}

public sealed record CreateCatalogProduct(
    string Name,
    int Quantity,
    decimal SalePrice,
    string? Description,
    string? ImageUrl,
    string? TrendyolBarcode,
    string? WebsiteBarcode,
    int TrendyolUnits,
    int WebsiteUnits,
    IReadOnlyList<CatalogAllocationRow> Allocations,
    IReadOnlyList<CatalogBarcodeLink>? ExtraBarcodes) : IRequest<CreateCatalogProductResult>;

public sealed class CreateCatalogProductHandler(ICatalogProductStore store)
    : IRequestHandler<CreateCatalogProduct, CreateCatalogProductResult>
{
    public Task<CreateCatalogProductResult> Handle(
        CreateCatalogProduct request,
        CancellationToken cancellationToken) =>
        store.AddAsync(
            request.Name,
            request.Quantity,
            request.SalePrice,
            request.Description,
            request.ImageUrl,
            request.TrendyolBarcode,
            request.WebsiteBarcode,
            request.TrendyolUnits,
            request.WebsiteUnits,
            request.Allocations,
            request.ExtraBarcodes,
            cancellationToken);
}
