namespace CommerceManager.Api.Users;

public sealed record RoleResponse(
    Guid Id,
    string Name,
    string? Description);
