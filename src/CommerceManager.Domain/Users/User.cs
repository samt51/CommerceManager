namespace CommerceManager.Domain.Users;

public sealed class User
{
    public Guid Id { get; private set; }

    public string UserName { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public string FullName { get; private set; } = string.Empty;

    public Guid RoleId { get; private set; }

    public Role Role { get; private set; } = null!;

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? LastLoginAt { get; private set; }

    private User()
    {
    }

    public User(
        string userName,
        string email,
        string passwordHash,
        string fullName,
        Guid roleId)
    {
        Id = Guid.NewGuid();
        UserName = userName.Trim();
        Email = email.Trim();
        PasswordHash = passwordHash;
        FullName = fullName.Trim();
        RoleId = roleId;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(
        string userName,
        string email,
        string fullName,
        Guid roleId)
    {
        UserName = userName.Trim();
        Email = email.Trim();
        FullName = fullName.Trim();
        RoleId = roleId;
    }

    public void SetPasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }

    public void MarkLogin()
    {
        LastLoginAt = DateTimeOffset.UtcNow;
    }
}
