using System.ComponentModel.DataAnnotations;

namespace IsoDof.Web.Models;

public class LoginViewModel
{
    [Display(Name = "E-posta")]
    [Required(ErrorMessage = "E-posta alanı zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Şifre")]
    [Required(ErrorMessage = "Şifre alanı zorunludur.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}