namespace CommerceManager.Domain.Cash;

public sealed class CashMovement
{
    private CashMovement()
    {
    }

    public Guid Id { get; private set; }

    public CashDirection Direction { get; private set; }

    public decimal Amount { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public Guid? ExpenseId { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public static CashMovement Create(
        CashDirection direction,
        decimal amount,
        string description,
        DateTimeOffset occurredAt,
        Guid? expenseId = null)
    {
        if (!Enum.IsDefined(direction))
            throw new ArgumentOutOfRangeException(nameof(direction));

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        return new CashMovement
        {
            Id = Guid.NewGuid(),
            Direction = direction,
            Amount = amount,
            Description = description.Trim(),
            ExpenseId = expenseId,
            OccurredAt = occurredAt
        };
    }
}
