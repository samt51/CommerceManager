namespace CommerceManager.Domain.Cash;

public sealed class Expense
{
    private Expense()
    {
    }

    public Guid Id { get; private set; }

    public Guid ExpenseTypeId { get; private set; }

    public decimal Amount { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; private set; }

    public static Expense Create(
        Guid expenseTypeId,
        decimal amount,
        string description,
        DateTimeOffset occurredAt)
    {
        if (expenseTypeId == Guid.Empty)
            throw new ArgumentException("Expense type is required.", nameof(expenseTypeId));

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        return new Expense
        {
            Id = Guid.NewGuid(),
            ExpenseTypeId = expenseTypeId,
            Amount = amount,
            Description = description.Trim(),
            OccurredAt = occurredAt
        };
    }
}
