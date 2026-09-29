using CommerceManager.Domain.Users;
using CommerceManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CommerceManager.Api.Users;

public sealed class UserManagementService
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UserManagementService(
        AppDbContext dbContext,
        IPasswordHasher<User> passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public async Task<List<UserListItemResponse>> GetUsersAsync(
        CancellationToken cancellationToken)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .OrderBy(x => x.FullName)
            .Select(x => new UserListItemResponse(
                x.Id,
                x.UserName,
                x.Email,
                x.FullName,
                x.RoleId,
                x.Role.Name,
                x.IsActive,
                x.CreatedAt,
                x.LastLoginAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RoleResponse>> GetRolesAsync(
        CancellationToken cancellationToken)
    {
        return await _dbContext.Roles
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new RoleResponse(
                x.Id,
                x.Name,
                x.Description))
            .ToListAsync(cancellationToken);
    }

    public async Task<(bool Success, string? Error)> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var userName = request.UserName.Trim();
        var email = request.Email.Trim();

        if (await _dbContext.Users.AnyAsync(
                x => x.UserName == userName,
                cancellationToken))
        {
            return (false, "Bu kullanıcı adı zaten kullanılıyor.");
        }

        if (await _dbContext.Users.AnyAsync(
                x => x.Email == email,
                cancellationToken))
        {
            return (false, "Bu e-posta adresi zaten kullanılıyor.");
        }

        var roleExists = await _dbContext.Roles.AnyAsync(
            x => x.Id == request.RoleId,
            cancellationToken);

        if (!roleExists)
        {
            return (false, "Geçerli bir rol seçin.");
        }

        var user = new User(
            userName,
            email,
            string.Empty,
            request.FullName,
            request.RoleId);

        user.SetPasswordHash(
            _passwordHasher.HashPassword(user, request.Password));

        _dbContext.Users.Add(user);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(
        Guid targetUserId,
        Guid currentUserId,
        UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .Include(x => x.Role)
            .SingleOrDefaultAsync(
                x => x.Id == targetUserId,
                cancellationToken);

        if (user is null)
        {
            return (false, "Kullanıcı bulunamadı.");
        }

        var role = await _dbContext.Roles
            .SingleOrDefaultAsync(
                x => x.Id == request.RoleId,
                cancellationToken);

        if (role is null)
        {
            return (false, "Geçerli bir rol seçin.");
        }

        var userName = request.UserName.Trim();
        var email = request.Email.Trim();

        if (await _dbContext.Users.AnyAsync(
                x => x.Id != targetUserId &&
                     x.UserName == userName,
                cancellationToken))
        {
            return (false, "Bu kullanıcı adı zaten kullanılıyor.");
        }

        if (await _dbContext.Users.AnyAsync(
                x => x.Id != targetUserId &&
                     x.Email == email,
                cancellationToken))
        {
            return (false, "Bu e-posta adresi zaten kullanılıyor.");
        }

        if (targetUserId == currentUserId && !request.IsActive)
        {
            return (false, "Kendi hesabınızı pasif duruma getiremezsiniz.");
        }

        var removingAdmin =
            string.Equals(
                user.Role.Name,
                "Admin",
                StringComparison.OrdinalIgnoreCase)
            &&
            (
                !string.Equals(
                    role.Name,
                    "Admin",
                    StringComparison.OrdinalIgnoreCase)
                ||
                !request.IsActive
            );

        if (removingAdmin)
        {
            var activeAdminCount = await _dbContext.Users
                .CountAsync(
                    x => x.IsActive &&
                         x.Role.Name == "Admin",
                    cancellationToken);

            if (activeAdminCount <= 1)
            {
                return (
                    false,
                    "Sistemde en az bir aktif Admin kullanıcısı bulunmalıdır.");
            }
        }

        user.Update(
            userName,
            email,
            request.FullName,
            request.RoleId);

        user.SetActive(request.IsActive);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ResetPasswordAsync(
        Guid targetUserId,
        ResetUserPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .SingleOrDefaultAsync(
                x => x.Id == targetUserId,
                cancellationToken);

        if (user is null)
        {
            return (false, "Kullanıcı bulunamadı.");
        }

        user.SetPasswordHash(
            _passwordHasher.HashPassword(
                user,
                request.NewPassword));

        await _dbContext.SaveChangesAsync(cancellationToken);

        return (true, null);
    }
}
