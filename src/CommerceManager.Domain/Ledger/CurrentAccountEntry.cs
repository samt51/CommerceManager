namespace CommerceManager.Domain.Ledger;

public sealed class CurrentAccountEntry
{
    private CurrentAccountEntry()
    {
    }

    public Guid Id { get; private set; }

    public Guid CurrentAccountId { get; private set; }

    public LedgerDirection Direction { get; private set; }

    public decimal Amount { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; private set; }

    public static CurrentAccountEntry Create(
        Guid currentAccountId,
        LedgerDirection direction,
        decimal amount,
        string description,
        DateTimeOffset occurredAt)
    {
        if (currentAccountId == Guid.Empty)
            throw new ArgumentException("Account is required.", nameof(currentAccountId));

        if (!Enum.IsDefined(direction))
            throw new ArgumentOutOfRangeException(nameof(direction));

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        return new CurrentAccountEntry
        {
            Id = Guid.NewGuid(),
            CurrentAccountId = currentAccountId,
            Direction = direction,
            Amount = amount,
            Description = description.Trim(),
            OccurredAt = occurredAt
        };
    }
}
