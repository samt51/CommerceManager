using System.Globalization;
using System.Text.Json;
using CommerceManager.Domain.Catalog;
using CommerceManager.Domain.Orders;
using CommerceManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommerceManager.Infrastructure.Integrations.TSoft;

public sealed record TSoftSyncResult(
    int Received,
    int Linked,
    int Created,
    string? OrderNote);

public sealed class TSoftCatalogSync(AppDbContext db, TSoftClient client)
{
    public async Task<TSoftSyncResult> SyncAsync(CancellationToken cancellationToken)
    {
        var token = await client.LoginAsync(cancellationToken);
        var received = 0;
        var linked = 0;
        var created = 0;
        var seen = new HashSet<string>(StringComparer.Ordinal);

        for (var start = 0; start < 5000; start += 100)
        {
            using var page = await client.PostAsync(
                "product/getProducts",
                token,
                new Dictionary<string, string>
                {
                    ["start"] = start.ToString(CultureInfo.InvariantCulture),
                    ["limit"] = "100",
                    ["columns"] = "ProductName,Barcode,Stock,SellingPriceVatIncluded,ImageUrl,IsActive"
                },
                cancellationToken);

            if (!page.RootElement.TryGetProperty("data", out var data) ||
                data.ValueKind != JsonValueKind.Array ||
                data.GetArrayLength() == 0)
                break;

            foreach (var item in data.EnumerateArray())
            {
                received++;
                if (!IsActive(item))
                    continue;

                var barcode = Text(item, "Barcode");
                if (string.IsNullOrWhiteSpace(barcode) || !seen.Add(barcode))
                    continue;

                var name = Text(item, "ProductName") ?? barcode;
                var stock = Math.Max(0, Int(item, "Stock"));
                var price = Money(item, "SellingPriceVatIncluded");
                var image = Absolute(Text(item, "ImageUrl"));
                if (await LinkAsync(barcode, name, stock, price, image, cancellationToken))
                    linked++;
                else
                {
                    await CreateAsync(barcode, name, stock, price, image, cancellationToken);
                    created++;
                }
            }

            await db.SaveChangesAsync(cancellationToken);
            if (data.GetArrayLength() < 100)
                break;
        }
        var orderNote = await TryOrdersAsync(token, cancellationToken);
        return new TSoftSyncResult(received, linked, created, orderNote);
    }

    private async Task<bool> LinkAsync(
        string barcode,
        string name,
        int stock,
        decimal price,
        string? image,
        CancellationToken cancellationToken)
    {
        var existing = await db.CatalogBarcodes
            .FirstOrDefaultAsync(x => x.Barcode == barcode, cancellationToken);
        if (existing is null)
            return false;

        var product = await db.CatalogProducts
            .FirstAsync(x => x.Id == existing.CatalogProductId, cancellationToken);

        var website = db.CatalogBarcodes.Local.FirstOrDefault(
                x => x.Channel == SalesChannel.Website && x.Barcode == barcode)
            ?? await db.CatalogBarcodes.FirstOrDefaultAsync(
                x => x.Channel == SalesChannel.Website && x.Barcode == barcode,
                cancellationToken);
        if (website is null)
        {
            db.CatalogBarcodes.Add(CatalogBarcode.Create(
                product.Id, SalesChannel.Website, barcode, existing.Units));
        }

        var allocationQuantity = Math.Min(stock, Math.Max(product.Quantity, 0));
        var allocation = await db.CatalogChannelAllocations.FirstOrDefaultAsync(
            x => x.CatalogProductId == product.Id && x.Channel == SalesChannel.Website,
            cancellationToken);
        if (allocation is null)
        {
            if (allocationQuantity > 0)
            {
                db.CatalogChannelAllocations.Add(CatalogChannelAllocation.Create(
                    product.Id, SalesChannel.Website, allocationQuantity));
            }
        }
        else
        {
            allocation.SetQuantity(allocationQuantity);
        }

        if (product.SalePrice == 0 && price > 0)
            product.SetDetails(product.Name, product.Description, price, image ?? product.ImageUrl);
        else if (string.IsNullOrWhiteSpace(product.ImageUrl) && image is not null)
            product.SetDetails(product.Name, product.Description, product.SalePrice, image);

        if (string.IsNullOrWhiteSpace(product.Name) && !string.IsNullOrWhiteSpace(name))
            product.Rename(name.Length > 300 ? name[..300] : name);

        return true;
    }

    private async Task CreateAsync(
        string barcode,
        string name,
        int stock,
        decimal price,
        string? image,
        CancellationToken cancellationToken)
    {
        var title = string.IsNullOrWhiteSpace(name) ? barcode : name.Trim();
        if (title.Length > 300)
            title = title[..300];

        var product = CatalogProduct.Create(title, stock, price, null, image);
        db.CatalogProducts.Add(product);
        db.CatalogBarcodes.Add(CatalogBarcode.Create(
            product.Id, SalesChannel.Website, barcode));
        if (stock > 0)
        {
            db.CatalogChannelAllocations.Add(CatalogChannelAllocation.Create(
                product.Id, SalesChannel.Website, stock));
        }

        await Task.CompletedTask;
    }

    private async Task<string?> TryOrdersAsync(string token, CancellationToken cancellationToken)
    {
        var saved = 0;
        string? fields = null;

        for (var start = 0; start < 200; start += 50)
        {
            using var page = await client.PostAsync(
                "order2/getOrders",
                token,
                new Dictionary<string, string>
                {
                    ["start"] = start.ToString(CultureInfo.InvariantCulture),
                    ["limit"] = "50"
                },
                cancellationToken);

            if (!page.RootElement.TryGetProperty("data", out var data) ||
                data.ValueKind != JsonValueKind.Array ||
                data.GetArrayLength() == 0)
                break;

            if (fields is null && data[0].ValueKind == JsonValueKind.Object)
                fields = string.Join(",", data[0].EnumerateObject().Take(12).Select(x => x.Name));

            foreach (var item in data.EnumerateArray())
            {
                var code = Text(item, "OrderCode") ?? Text(item, "OrderId");
                if (string.IsNullOrWhiteSpace(code))
                    continue;

                var total = Money(item, "OrderTotalPrice");
                if (total == 0)
                    total = Money(item, "OrderTotal");
                var when = When(item, "OrderDate") ?? DateTimeOffset.UtcNow;
                var status = Text(item, "OrderStatus") ?? Text(item, "OrderStatusId") ?? "Unknown";

                var order = await db.Orders.FirstOrDefaultAsync(
                    x => x.Channel == SalesChannel.Website && x.ExternalOrderId == code,
                    cancellationToken);

                if (order is null)
                {
                    order = Order.Create(SalesChannel.Website, code, when, total);
                    db.Orders.Add(order);
                }

                order.UpdateWebsiteOrder(code, status, when, total);
                saved++;
            }

            await db.SaveChangesAsync(cancellationToken);
            if (data.GetArrayLength() < 50)
                break;
        }

        return saved == 0
            ? "Sipariş servisi açıldı, yazılacak kayıt yok. Alanlar: " + (fields ?? "yok")
            : $"{saved} site siparişi kaydedildi.";
    }

    private string? Absolute(string? image)
    {
        if (string.IsNullOrWhiteSpace(image))
            return null;
        if (image.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return image;
        return client.Site() + (image.StartsWith('/') ? image : "/" + image);
    }

    private static bool IsActive(JsonElement item)
    {
        var value = Text(item, "IsActive");
        return value is null or "1" or "true";
    }

    private static string? Text(JsonElement item, string name)
    {
        if (!item.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
            return null;
        var text = value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    private static int Int(JsonElement item, string name)
    {
        var text = Text(item, name);
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;
    }

    private static decimal Money(JsonElement item, string name)
    {
        var text = Text(item, name);
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;
    }

    private static DateTimeOffset? When(JsonElement item, string name)
    {
        var text = Text(item, name);
        if (text is null)
            return null;

        if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
            return parsed;

        return DateTime.TryParse(text, CultureInfo.GetCultureInfo("tr-TR"), DateTimeStyles.AssumeLocal, out var local)
            ? new DateTimeOffset(local)
            : null;
    }
}
