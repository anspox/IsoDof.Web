using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IsoDof.Web.Models.Entities.Enums;

namespace IsoDof.Web.Models.Entities;

/// <summary>
/// Bir DÖF'ün durum değişikliklerinin denetim izi (audit trail).
/// Kim, ne zaman, hangi durumdan hangi duruma geçirdi bilgisini tutar.
/// </summary>
public class DofStatusHistory
{
    public int Id { get; set; }

    [Required]
    public int DofId { get; set; }
    [ForeignKey(nameof(DofId))]
    public Dof? Dof { get; set; }

    /// <summary>Önceki durum. Kayıt ilk oluşturulduğunda null olur.</summary>
    public DofStatus? OldStatus { get; set; }

    [Required]
    public DofStatus NewStatus { get; set; }

    [Required]
    public int ChangedByUserId { get; set; }
    [ForeignKey(nameof(ChangedByUserId))]
    public AppUser? ChangedByUser { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? Note { get; set; }
}
