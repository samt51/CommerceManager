using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CommerceManager.Domain.Users;
using CommerceManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CommerceManager.Api.Auth;

public sealed class UserAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly JwtOptions _jwtOptions;

    public UserAuthService(
        AppDbContext dbContext,
        IPasswordHasher<User> passwordHasher,
        IOptions<JwtOptions> jwtOptions)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<LoginResponse?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var userName = request.UserName?.Trim();

        if (string.IsNullOrWhiteSpace(userName) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return null;
        }

        var user = await _dbContext.Users
            .Include(x => x.Role)
            .SingleOrDefaultAsync(
                x => x.UserName == userName,
                cancellationToken);

        if (user is null || !user.IsActive)
        {
            return null;
        }

        var passwordResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (passwordResult == PasswordVerificationResult.Failed)
        {
            return null;
        }

        if (passwordResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.SetPasswordHash(
                _passwordHasher.HashPassword(user, request.Password));
        }

        user.MarkLogin();

        await _dbContext.SaveChangesAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(_jwtOptions.ExpirationMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim("full_name", user.FullName),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("role_id", user.RoleId.ToString()),
            new Claim(ClaimTypes.Role, user.Role.Name)
        };

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtOptions.Key));

        var credentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new LoginResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt,
            user.Id,
            user.UserName,
            user.FullName,
            user.Email,
            user.RoleId,
            user.Role.Name);
    }
}
