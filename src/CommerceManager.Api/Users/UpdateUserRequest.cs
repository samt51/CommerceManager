using System.ComponentModel.DataAnnotations;

namespace CommerceManager.Api.Users;

public sealed class UpdateUserRequest
{
    [Required]
    [MaxLength(100)]
    public string UserName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(250)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string FullName { get; init; } = string.Empty;

    [Required]
    public Guid RoleId { get; init; }

    public bool IsActive { get; init; }
}
