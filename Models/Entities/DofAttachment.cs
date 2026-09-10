using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IsoDof.Web.Models.Entities;

public class DofAttachment
{
    public int Id { get; set; }

    [Required]
    public int DofId { get; set; }
    [ForeignKey(nameof(DofId))]
    public Dof? Dof { get; set; }

    [Required]
    public string FileName { get; set; } = string.Empty;

    [Required]
    public string StoredFileName { get; set; } = string.Empty;

    [Required]
    public int UploadedByUserId { get; set; }
    [ForeignKey(nameof(UploadedByUserId))]
    public AppUser? UploadedByUser { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}