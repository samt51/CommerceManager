using System.ComponentModel.DataAnnotations;

namespace CommerceManager.Web.Models.Users;

public sealed class ResetUserPasswordViewModel
{
    public Guid UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni şifre zorunludur.")]
    [MinLength(8, ErrorMessage = "Şifre en az 8 karakter olmalıdır.")]
    [DataType(DataType.Password)]
    [Display(Name = "Yeni Şifre")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre tekrarını girin.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Şifreler eşleşmiyor.")]
    [Display(Name = "Yeni Şifre Tekrar")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
