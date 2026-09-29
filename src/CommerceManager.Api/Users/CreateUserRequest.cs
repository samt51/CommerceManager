using System.ComponentModel.DataAnnotations;

namespace CommerceManager.Api.Users;

public sealed class CreateUserRequest
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
    [MinLength(8)]
    public string Password { get; init; } = string.Empty;

    [Required]
    public Guid RoleId { get; init; }
}
