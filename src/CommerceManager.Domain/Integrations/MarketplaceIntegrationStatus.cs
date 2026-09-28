using CommerceManager.Domain.Orders;

namespace CommerceManager.Domain.Integrations;

public sealed class MarketplaceIntegrationStatus
{
    public Guid Id { get; private set; }

    public SalesChannel Channel { get; private set; }

    public bool IsConnected { get; private set; }

    public DateTimeOffset? LastSuccessfulSyncAt { get; private set; }

    public DateTimeOffset? LastFailedSyncAt { get; private set; }

    public string? LastError { get; private set; }

    public DateTimeOffset? LastOrderSyncAt { get; private set; }

    public DateTimeOffset? LastReturnSyncAt { get; private set; }

    public DateTimeOffset? LastProductSyncAt { get; private set; }

    public DateTimeOffset? LastInventorySyncAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    private MarketplaceIntegrationStatus()
    {
    }

    public static MarketplaceIntegrationStatus Create(
        SalesChannel channel)
    {
        return new MarketplaceIntegrationStatus
        {
            Id = Guid.NewGuid(),
            Channel = channel,
            IsConnected = false,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void MarkOrderSyncSucceeded()
    {
        var now = DateTimeOffset.UtcNow;

        IsConnected = true;
        LastSuccessfulSyncAt = now;
        LastOrderSyncAt = now;
        LastError = null;
        UpdatedAt = now;
    }

    public void MarkReturnSyncSucceeded()
    {
        var now = DateTimeOffset.UtcNow;

        IsConnected = true;
        LastSuccessfulSyncAt = now;
        LastReturnSyncAt = now;
        LastError = null;
        UpdatedAt = now;
    }

    public void MarkProductSyncSucceeded()
    {
        var now = DateTimeOffset.UtcNow;

        IsConnected = true;
        LastSuccessfulSyncAt = now;
        LastProductSyncAt = now;
        LastError = null;
        UpdatedAt = now;
    }

    public void MarkInventorySyncSucceeded()
    {
        var now = DateTimeOffset.UtcNow;

        IsConnected = true;
        LastSuccessfulSyncAt = now;
        LastInventorySyncAt = now;
        LastError = null;
        UpdatedAt = now;
    }

    public void MarkFailed(string? error)
    {
        var now = DateTimeOffset.UtcNow;

        IsConnected = false;
        LastFailedSyncAt = now;

        LastError = string.IsNullOrWhiteSpace(error)
            ? "Bilinmeyen entegrasyon hatası."
            : error.Trim();

        UpdatedAt = now;
    }
}
