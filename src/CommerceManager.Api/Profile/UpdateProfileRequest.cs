using System.ComponentModel.DataAnnotations;

namespace CommerceManager.Api.Profile;

public sealed class UpdateProfileRequest
{
    [Required]
    [MaxLength(100)]
    public string FullName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; init; } = string.Empty;
}
