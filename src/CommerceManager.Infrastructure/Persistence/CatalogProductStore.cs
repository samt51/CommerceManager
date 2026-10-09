using CommerceManager.Application.Catalog;
using CommerceManager.Domain.Catalog;
using CommerceManager.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace CommerceManager.Infrastructure.Persistence;

public sealed class CatalogProductStore(AppDbContext db) : ICatalogProductStore
{
    public async Task<UpdateCatalogProductResult> UpdateAsync(
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
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 300)
            return new UpdateCatalogProductResult(false, "Ürün adı gerekli ve en fazla 300 karakter olabilir.", null);

        if (salePrice < 0)
            return new UpdateCatalogProductResult(false, "Satış fiyatı 0 veya daha büyük olmalı.", null);

        if (description?.Length > 2000)
            return new UpdateCatalogProductResult(false, "Açıklama en fazla 2000 karakter olabilir.", null);

        if (quantity < 0 || trendyolQuantity < 0 || websiteQuantity < 0)
            return new UpdateCatalogProductResult(false, "Stok adetleri 0 veya daha büyük olmalı.", null);

        if (trendyolQuantity + websiteQuantity > quantity)
            return new UpdateCatalogProductResult(false, "Kanal stokları eldeki stoktan büyük olamaz.", null);

        var product = await db.CatalogProducts
            .FirstOrDefaultAsync(x => x.Id == productId, cancellationToken);

        if (product is null)
            return new UpdateCatalogProductResult(false, "Ürün bulunamadı.", null);

        var trendyolBarcode = await db.CatalogBarcodes
            .Where(x => x.CatalogProductId == productId && x.Channel == SalesChannel.Trendyol)
            .Select(x => x.Barcode)
            .FirstOrDefaultAsync(cancellationToken);

        var websiteBarcode = await db.CatalogBarcodes
            .Where(x => x.CatalogProductId == productId && x.Channel == SalesChannel.Website)
            .Select(x => x.Barcode)
            .FirstOrDefaultAsync(cancellationToken);

        if (trendyolQuantity > 0 && string.IsNullOrWhiteSpace(trendyolBarcode))
            return new UpdateCatalogProductResult(false, "Trendyol stoğu için barkod gerekli.", null);

        if (websiteQuantity > 0 && string.IsNullOrWhiteSpace(websiteBarcode))
            return new UpdateCatalogProductResult(false, "E-ticaret stoğu için barkod gerekli.", null);

        product.SetDetails(name, description, salePrice, imageUrl ?? product.ImageUrl);
        product.SetQuantity(quantity);
        var barcodeError = await SyncBarcodesAsync(productId, barcodes, cancellationToken);
        if (barcodeError is not null)
            return new UpdateCatalogProductResult(false, barcodeError, null);

        await SetAllocationAsync(productId, SalesChannel.Trendyol, trendyolQuantity, cancellationToken);
        await SetAllocationAsync(productId, SalesChannel.Website, websiteQuantity, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var trendyolRows = await db.CatalogBarcodes
            .Where(x => x.CatalogProductId == productId && x.Channel == SalesChannel.Trendyol)
            .OrderBy(x => x.Barcode)
            .ToListAsync(cancellationToken);

        decimal? previousListPrice = trendyolRows.FirstOrDefault()?.ListPrice;
        if (trendyolListPrice is not null && trendyolRows.Count > 0)
            trendyolRows[0].SetListPrice(trendyolListPrice);

        if (trendyolListPrice is not null)
            await db.SaveChangesAsync(cancellationToken);

        var trendyolBarcodes = trendyolRows
            .Select(x => new CatalogBarcodeLink((int)x.Channel, x.Barcode, x.Units))
            .ToList();

        return new UpdateCatalogProductResult(true, null, trendyolBarcode, trendyolBarcodes, previousListPrice);
    }

    private async Task<string?> SyncBarcodesAsync(
        Guid productId,
        IReadOnlyList<CatalogBarcodeLink> barcodes,
        CancellationToken cancellationToken)
    {
        var desired = new List<(SalesChannel Channel, string Barcode, int Units)>();

        foreach (var link in barcodes)
        {
            if (string.IsNullOrWhiteSpace(link.Barcode))
                continue;

            if (link.Units < 1)
                return "Barkodun düşeceği adet en az 1 olmalı.";

            if (!Enum.IsDefined(typeof(SalesChannel), link.Channel))
                return "Bilinmeyen satış kanalı.";

            var channel = (SalesChannel)link.Channel;

            if (channel is not (SalesChannel.Trendyol or SalesChannel.Website))
                return "Barkod yalnızca Trendyol veya e-ticaret olabilir.";

            var barcode = link.Barcode.Trim();

            if (barcode.Length > 150)
                return "Barkod en fazla 150 karakter olabilir.";

            if (desired.Any(x => x.Channel == channel && x.Barcode == barcode))
                return "Aynı barkod iki kez yazılamaz.";

            desired.Add((channel, barcode, link.Units));
        }

        var existing = await db.CatalogBarcodes
            .Where(x => x.CatalogProductId == productId)
            .ToListAsync(cancellationToken);

        foreach (var item in desired)
        {
            var owner = await db.CatalogBarcodes.FirstOrDefaultAsync(
                x => x.Channel == item.Channel && x.Barcode == item.Barcode,
                cancellationToken);

            if (owner is not null && owner.CatalogProductId != productId)
                return "Bu barkod başka bir ürüne bağlı.";
        }

        foreach (var row in existing)
        {
            if (!desired.Any(x => x.Channel == row.Channel && x.Barcode == row.Barcode))
                db.CatalogBarcodes.Remove(row);
        }

        foreach (var item in desired)
        {
            var row = existing.FirstOrDefault(
                x => x.Channel == item.Channel && x.Barcode == item.Barcode);

            if (row is null)
            {
                db.CatalogBarcodes.Add(
                    CatalogBarcode.Create(productId, item.Channel, item.Barcode, item.Units));
            }
            else
            {
                row.SetUnits(item.Units);
            }
        }

        return null;
    }

    private async Task SetAllocationAsync(
        Guid productId,
        SalesChannel channel,
        int quantity,
        CancellationToken cancellationToken)
    {
        var allocation = await db.CatalogChannelAllocations
            .FirstOrDefaultAsync(
                x => x.CatalogProductId == productId && x.Channel == channel,
                cancellationToken);

        if (allocation is null)
        {
            if (quantity == 0)
                return;

            db.CatalogChannelAllocations.Add(
                CatalogChannelAllocation.Create(productId, channel, quantity));
            return;
        }

        allocation.SetQuantity(quantity);
    }

    public async Task<IReadOnlyList<TrendyolListingRow>> TrendyolListingsAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var allocation = await db.CatalogChannelAllocations.AsNoTracking()
            .Where(x => x.CatalogProductId == productId && x.Channel == SalesChannel.Trendyol)
            .Select(x => (int?)x.Quantity)
            .FirstOrDefaultAsync(cancellationToken);

        var barcodes = await db.CatalogBarcodes.AsNoTracking()
            .Where(x => x.CatalogProductId == productId && x.Channel == SalesChannel.Trendyol)
            .OrderBy(x => x.Barcode)
            .ToListAsync(cancellationToken);

        return barcodes.Select(x => new TrendyolListingRow(
            x.Barcode,
            x.Units,
            allocation is null ? null : allocation.Value / Math.Max(x.Units, 1),
            x.ListPrice)).ToList();
    }

    public async Task<SetTrendyolStockResult> SetTrendyolStockAsync(
        Guid productId,
        int quantity,
        CancellationToken cancellationToken)
    {
        if (quantity < 0)
            return new SetTrendyolStockResult(false, "Trendyol stoğu 0 veya daha büyük olmalı.", null);

        var product = await db.CatalogProducts
            .FirstOrDefaultAsync(x => x.Id == productId, cancellationToken);

        if (product is null)
            return new SetTrendyolStockResult(false, "Ürün bulunamadı.", null);

        if (quantity > product.Quantity)
        {
            return new SetTrendyolStockResult(
                false,
                "Trendyol stoğu eldeki stoktan büyük olamaz.",
                null);
        }

        var barcode = await db.CatalogBarcodes
            .Where(x => x.CatalogProductId == productId && x.Channel == SalesChannel.Trendyol)
            .Select(x => x.Barcode)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(barcode))
            return new SetTrendyolStockResult(false, "Bu üründe Trendyol barkodu yok.", null);

        var allocation = await db.CatalogChannelAllocations
            .FirstOrDefaultAsync(
                x => x.CatalogProductId == productId && x.Channel == SalesChannel.Trendyol,
                cancellationToken);

        if (allocation is null)
        {
            db.CatalogChannelAllocations.Add(
                CatalogChannelAllocation.Create(productId, SalesChannel.Trendyol, quantity));
        }
        else
        {
            allocation.SetQuantity(quantity);
        }

        await db.SaveChangesAsync(cancellationToken);

        return new SetTrendyolStockResult(true, null, barcode);
    }

    public async Task<CreateCatalogProductResult> AddAsync(
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
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return new CreateCatalogProductResult(
                false,
                "Ürün adı gerekli.",
                null);
        }

        if (name.Trim().Length > 300)
        {
            return new CreateCatalogProductResult(
                false,
                "Ürün adı en fazla 300 karakter olabilir.",
                null);
        }

        if (quantity < 0)
        {
            return new CreateCatalogProductResult(
                false,
                "Stok adedi 0 veya daha büyük olmalı.",
                null);
        }

        if (salePrice < 0)
        {
            return new CreateCatalogProductResult(
                false,
                "Satış fiyatı 0 veya daha büyük olmalı.",
                null);
        }

        if (description?.Length > 2000)
        {
            return new CreateCatalogProductResult(
                false,
                "Açıklama en fazla 2000 karakter olabilir.",
                null);
        }

        if (trendyolUnits < 1 || websiteUnits < 1)
        {
            return new CreateCatalogProductResult(
                false,
                "Barkodun düşeceği adet en az 1 olmalı.",
                null);
        }

        var links = new List<(SalesChannel Channel, string Barcode, int Units)>();

        var trendyol = Normalize(trendyolBarcode);
        var website = Normalize(websiteBarcode);

        if (trendyol is not null)
            links.Add((SalesChannel.Trendyol, trendyol, trendyolUnits));

        if (website is not null)
            links.Add((SalesChannel.Website, website, websiteUnits));

        if (extraBarcodes is not null)
        {
            foreach (var extra in extraBarcodes)
            {
                if (string.IsNullOrWhiteSpace(extra.Barcode) || extra.Units < 1)
                    continue;

                if (!Enum.IsDefined(typeof(SalesChannel), extra.Channel))
                    continue;

                var channel = (SalesChannel)extra.Channel;

                if (channel is not (SalesChannel.Trendyol or SalesChannel.Website))
                    continue;

                var barcode = extra.Barcode.Trim();

                if (links.Any(x => x.Channel == channel && x.Barcode == barcode))
                    continue;

                links.Add((channel, barcode, extra.Units));
            }
        }

        foreach (var link in links)
        {
            if (link.Barcode.Length > 150)
            {
                return new CreateCatalogProductResult(
                    false,
                    "Barkod en fazla 150 karakter olabilir.",
                    null);
            }

            var taken = await db.CatalogBarcodes.AnyAsync(
                x => x.Channel == link.Channel &&
                     x.Barcode == link.Barcode,
                cancellationToken);

            if (taken)
            {
                var channelName = link.Channel == SalesChannel.Trendyol
                    ? "Trendyol"
                    : "site";

                return new CreateCatalogProductResult(
                    false,
                    $"Bu {channelName} barkodu başka bir ürüne bağlı.",
                    null);
            }
        }

        var product = CatalogProduct.Create(name, quantity, salePrice, description, imageUrl);
        db.CatalogProducts.Add(product);

        foreach (var link in links)
        {
            db.CatalogBarcodes.Add(
                CatalogBarcode.Create(
                    product.Id,
                    link.Channel,
                    link.Barcode,
                    link.Units));
        }

        var seenChannels = new HashSet<SalesChannel>();

        foreach (var allocation in allocations)
        {
            if (allocation.Quantity <= 0)
                continue;

            if (!Enum.IsDefined(typeof(SalesChannel), allocation.Channel))
            {
                return new CreateCatalogProductResult(
                    false,
                    "Bilinmeyen satış kanalı.",
                    null);
            }

            var channel = (SalesChannel)allocation.Channel;

            if (channel == SalesChannel.Retail)
            {
                return new CreateCatalogProductResult(
                    false,
                    "Perakende payı ayrıca girilmez. Genel stoktan diğer kanallar düşünce kalan adettir.",
                    null);
            }

            if (!seenChannels.Add(channel))
            {
                return new CreateCatalogProductResult(
                    false,
                    "Aynı kanal için birden fazla pay girilemez.",
                    null);
            }

            db.CatalogChannelAllocations.Add(
                CatalogChannelAllocation.Create(
                    product.Id,
                    channel,
                    allocation.Quantity));
        }

        await db.SaveChangesAsync(cancellationToken);

        return new CreateCatalogProductResult(true, null, product.Id);
    }

    private static string? Normalize(string? barcode) =>
        string.IsNullOrWhiteSpace(barcode)
            ? null
            : barcode.Trim();
}
