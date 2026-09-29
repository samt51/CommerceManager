namespace CommerceManager.Domain.Users;

public sealed class Role
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    private Role()
    {
    }

    public Role(string name, string? description = null)
    {
        Id = Guid.NewGuid();
        Name = name.Trim();
        Description = description?.Trim();
    }
}
