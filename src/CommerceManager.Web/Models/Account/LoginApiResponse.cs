namespace CommerceManager.Web.Models.Account;

public sealed record LoginApiResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    Guid UserId,
    string UserName,
    string FullName,
    string Email,
    Guid RoleId,
    string Role);
