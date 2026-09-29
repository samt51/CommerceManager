using System.ComponentModel.DataAnnotations;

namespace CommerceManager.Api.Users;

public sealed class ResetUserPasswordRequest
{
    [Required]
    [MinLength(8)]
    public string NewPassword { get; init; } = string.Empty;

    [Required]
    [Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; init; } = string.Empty;
}
