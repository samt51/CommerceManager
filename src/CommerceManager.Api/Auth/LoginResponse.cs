namespace CommerceManager.Api.Auth;

public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    Guid UserId,
    string UserName,
    string FullName,
    string Email,
    Guid RoleId,
    string Role);
