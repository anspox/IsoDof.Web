using System.ComponentModel.DataAnnotations;

namespace IsoDof.Web.Models.Entities;

public class Department
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Code { get; set; }

    public ICollection<Dof> Dofs { get; set; } = new List<Dof>();
}