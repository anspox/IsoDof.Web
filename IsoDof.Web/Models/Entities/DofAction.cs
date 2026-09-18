using System.ComponentModel.DataAnnotations;

namespace IsoDof.Web.Models.Entities;

public class DofAction
{
    public int Id { get; set; }

    public int DofId { get; set; }
    public Dof? Dof { get; set; }

    [Required, MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public int? ResponsibleUserId { get; set; }
    public AppUser? ResponsibleUser { get; set; }

    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }

    public bool IsCompleted => CompletedAt != null;
}