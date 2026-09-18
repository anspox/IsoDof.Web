using System.ComponentModel.DataAnnotations;

namespace IsoDof.Web.Models.Entities;

public class Department
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Code { get; set; }

    [Display(Name = "Kalite Kontrol Sorumlusu")]
    public int? QualityResponsibleUserId { get; set; }
    public AppUser? QualityResponsibleUser { get; set; }

    public ICollection<Dof> Dofs { get; set; } = new List<Dof>();
}