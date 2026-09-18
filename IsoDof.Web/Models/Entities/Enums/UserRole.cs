using System.ComponentModel.DataAnnotations;

namespace IsoDof.Web.Models.Entities.Enums;

public enum UserRole
{
    [Display(Name = "Kullanıcı")]
    Kullanici = 0,

    [Display(Name = "Yönetici (Admin)")]
    Admin = 1,

    [Display(Name = "Kalite Kontrol Sorumlusu")]
    KaliteKontrol = 2
}