namespace CommerceManager.Api.Users;

public sealed record UserListItemResponse(
    Guid Id,
    string UserName,
    string Email,
    string FullName,
    Guid RoleId,
    string Role,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt);
