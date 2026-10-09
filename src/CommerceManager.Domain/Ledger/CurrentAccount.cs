namespace CommerceManager.Domain.Ledger;

public sealed class CurrentAccount
{
    private CurrentAccount()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public CurrentAccountKind Kind { get; private set; }

    public string? Phone { get; private set; }

    public string? TaxNumber { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static CurrentAccount Create(
        string name,
        CurrentAccountKind kind,
        string? phone,
        string? taxNumber)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Account name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Phone is required.", nameof(phone));

        if (!Enum.IsDefined(kind))
            throw new ArgumentOutOfRangeException(nameof(kind));

        return new CurrentAccount
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Kind = kind,
            Phone = phone.Trim(),
            TaxNumber = Normalize(taxNumber),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
