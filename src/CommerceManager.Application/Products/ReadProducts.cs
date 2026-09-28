using MediatR;

namespace CommerceManager.Application.Products;

public sealed record ProductRow(
    Guid Id,
    string Barcode,
    string? StockCode,
    string? ExternalProductId,
    string? ProductMainId,
    string Name,
    string? ImageUrl,
    int? Quantity,
    DateTimeOffset UpdatedAt);

public sealed record ProductPage(
    int Page,
    int Size,
    int Total,
    int WithStockCount,
    int WithoutStockCount,
    IReadOnlyList<ProductRow> Items);

public interface IProductReader
{
    Task<ProductPage> ListTrendyolAsync(
        int page,
        int size,
        string? search,
        CancellationToken cancellationToken);

    Task<ProductDetails?> GetTrendyolAsync(
        Guid id,
        CancellationToken cancellationToken);
}

public sealed record ListTrendyolProducts(
    int Page = 0,
    int Size = 25,
    string? Search = null)
    : IRequest<ProductPage>;

public sealed class ListTrendyolProductsHandler(
    IProductReader reader)
    : IRequestHandler<ListTrendyolProducts, ProductPage>
{
    public Task<ProductPage> Handle(
        ListTrendyolProducts request,
        CancellationToken cancellationToken)
        => reader.ListTrendyolAsync(
            request.Page,
            request.Size,
            request.Search,
            cancellationToken);
}

public sealed record ProductDetails(
    Guid Id,
    string Barcode,
    string? StockCode,
    string? ExternalProductId,
    string? ProductMainId,
    string Name,
    string? ImageUrl,
    int? Quantity,
    DateTimeOffset ProductUpdatedAt,
    DateTimeOffset? StockUpdatedAt);

public sealed record GetTrendyolProduct(Guid Id)
    : IRequest<ProductDetails?>;

public sealed class GetTrendyolProductHandler(
    IProductReader reader)
    : IRequestHandler<GetTrendyolProduct, ProductDetails?>
{
    public Task<ProductDetails?> Handle(
        GetTrendyolProduct request,
        CancellationToken cancellationToken)
        => reader.GetTrendyolAsync(
            request.Id,
            cancellationToken);
}
