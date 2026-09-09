
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IsoDof.Web.Models.Entities.Enums;

namespace IsoDof.Web.Models.Entities;

public class Dof
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public DofType Type { get; set; }
    public DofSource Source { get; set; }
    public DofStatus Status { get; set; } = DofStatus.Acik;

    public int DepartmentId { get; set; }
    public Department? Department { get; set; }

    public int CreatedByUserId { get; set; }
    [ForeignKey(nameof(CreatedByUserId))]
    public AppUser? CreatedByUser { get; set; }

    public int? AssignedToUserId { get; set; }
    [ForeignKey(nameof(AssignedToUserId))]
    public AppUser? AssignedToUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    public DateTime? ClosedAt { get; set; }

    public ICollection<DofAction> Actions { get; set; } = new List<DofAction>();

    public bool IsOverdue => DueDate.HasValue && DateTime.UtcNow > DueDate.Value && Status != DofStatus.Kapatildi && Status != DofStatus.Reddedildi;

}