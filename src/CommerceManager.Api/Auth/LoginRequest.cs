namespace CommerceManager.Api.Auth;

public sealed record LoginRequest(
    string UserName,
    string Password);
