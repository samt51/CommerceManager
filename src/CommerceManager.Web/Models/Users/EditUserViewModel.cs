using System.ComponentModel.DataAnnotations;

namespace CommerceManager.Web.Models.Users;

public sealed class EditUserViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
    [Display(Name = "Kullanıcı Adı")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Rol seçmelisiniz.")]
    [Display(Name = "Rol")]
    public Guid RoleId { get; set; }

    [Display(Name = "Aktif Kullanıcı")]
    public bool IsActive { get; set; }

    public List<RoleViewModel> Roles { get; set; } = [];
}
