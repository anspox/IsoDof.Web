using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IsoDof.Web.Models.Entities;

public class DofComment
{
    public int Id { get; set; }

    [Required]
    public int DofId { get; set; }
    [ForeignKey(nameof(DofId))]
    public Dof? Dof { get; set; }

    [Required]
    public int AuthorUserId { get; set; }
    [ForeignKey(nameof(AuthorUserId))]
    public AppUser? AuthorUser { get; set; }

    [Required(ErrorMessage = "Yorum boş olamaz.")]
    [MaxLength(1000, ErrorMessage = "Yorum en fazla 1000 karakter olabilir.")]
    public string Text { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}