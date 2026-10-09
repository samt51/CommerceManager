namespace CommerceManager.Domain.Cash;

public sealed class ExpenseType
{
    private ExpenseType()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public static ExpenseType Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Expense type name is required.", nameof(name));

        return new ExpenseType
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            IsActive = true
        };
    }
}
