using CommerceManager.Domain.Users;
using CommerceManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CommerceManager.Api.Profile;

public sealed class UserProfileService
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UserProfileService(
        AppDbContext dbContext,
        IPasswordHasher<User> passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserProfileResponse?> GetAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == userId && x.IsActive)
            .Select(x => new UserProfileResponse(
                x.Id,
                x.UserName,
                x.Email,
                x.FullName,
                x.Role.Name,
                x.CreatedAt,
                x.LastLoginAt))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .SingleOrDefaultAsync(
                x => x.Id == userId && x.IsActive,
                cancellationToken);

        if (user is null)
        {
            return (false, "Kullanıcı bulunamadı.");
        }

        var email = request.Email.Trim();

        var emailExists = await _dbContext.Users
            .AnyAsync(
                x => x.Id != userId && x.Email == email,
                cancellationToken);

        if (emailExists)
        {
            return (false, "Bu e-posta adresi başka bir kullanıcı tarafından kullanılıyor.");
        }

        user.Update(
            user.UserName,
            email,
            request.FullName,
            user.RoleId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .SingleOrDefaultAsync(
                x => x.Id == userId && x.IsActive,
                cancellationToken);

        if (user is null)
        {
            return (false, "Kullanıcı bulunamadı.");
        }

        var verification = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.CurrentPassword);

        if (verification == PasswordVerificationResult.Failed)
        {
            return (false, "Mevcut şifreniz hatalı.");
        }

        if (request.CurrentPassword == request.NewPassword)
        {
            return (false, "Yeni şifreniz mevcut şifrenizden farklı olmalıdır.");
        }

        user.SetPasswordHash(
            _passwordHasher.HashPassword(user, request.NewPassword));

        await _dbContext.SaveChangesAsync(cancellationToken);

        return (true, null);
    }
}
