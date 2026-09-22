using System.ComponentModel.DataAnnotations;

namespace IsoDof.Web.Models;

public class ChangePasswordViewModel
{
    [Display(Name = "Mevcut Şifre")]
    [Required(ErrorMessage = "Mevcut şifre zorunludur.")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Display(Name = "Yeni Şifre")]
    [Required(ErrorMessage = "Yeni şifre zorunludur.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Display(Name = "Yeni Şifre (Tekrar)")]
    [Required(ErrorMessage = "Yeni şifre tekrarı zorunludur.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Yeni şifreler eşleşmiyor.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
