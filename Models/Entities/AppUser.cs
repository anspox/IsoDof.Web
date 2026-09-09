using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IsoDof.Web.Models.Entities;

public class AppUser
{
    public int Id { get; set; }

    [Display(Name = "Ad Soyad")]
    [Required(ErrorMessage = "Ad Soyad alanı zorunludur.")]
    [MaxLength(100, ErrorMessage = "Ad Soyad en fazla 100 karakter olabilir.")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "E-posta")]
    [Required(ErrorMessage = "E-posta alanı zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    [MaxLength(200, ErrorMessage = "E-posta en fazla 200 karakter olabilir.")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Departman")]
    [Required(ErrorMessage = "Lütfen bir departman seçiniz.")]
    [Range(1, int.MaxValue, ErrorMessage = "Lütfen bir departman seçiniz.")]
    public int? DepartmentId { get; set; }
    
    public Department? Department { get; set; }

    [InverseProperty(nameof(Dof.CreatedByUser))]
    public ICollection<Dof> CreatedDofs { get; set; } = new List<Dof>();

    [InverseProperty(nameof(Dof.AssignedToUser))]
    public ICollection<Dof> AssignedDofs { get; set; } = new List<Dof>();
}
