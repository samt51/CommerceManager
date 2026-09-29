namespace CommerceManager.Api.Profile;

public sealed record UserProfileResponse(
    Guid Id,
    string UserName,
    string Email,
    string FullName,
    string Role,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt);
