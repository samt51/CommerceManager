using System.ComponentModel.DataAnnotations;

namespace CommerceManager.Web.Models.Profile;

public sealed class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Mevcut şifrenizi girin.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mevcut Şifre")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni şifrenizi girin.")]
    [MinLength(8, ErrorMessage = "Yeni şifre en az 8 karakter olmalıdır.")]
    [DataType(DataType.Password)]
    [Display(Name = "Yeni Şifre")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni şifrenizi tekrar girin.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Yeni şifreler eşleşmiyor.")]
    [Display(Name = "Yeni Şifre Tekrar")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
