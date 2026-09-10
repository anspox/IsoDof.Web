
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IsoDof.Web.Models.Entities.Enums;

namespace IsoDof.Web.Models.Entities;

public class Dof
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Başlık zorunludur.")]
    [MaxLength(200, ErrorMessage = "Başlık en fazla 200 karakter olabilir.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Açıklama zorunludur.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tür seçiniz.")]
    public DofType Type { get; set; }

    [Required(ErrorMessage = "Kaynak seçiniz.")]
    public DofSource Source { get; set; }

    public DofStatus Status { get; set; } = DofStatus.Acik;

    [Range(1, int.MaxValue, ErrorMessage = "Departman seçiniz.")]
    public int DepartmentId { get; set; }
    public Department? Department { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Açan kişi seçiniz.")]
    public int CreatedByUserId { get; set; }
    [ForeignKey(nameof(CreatedByUserId))]
    public AppUser? CreatedByUser { get; set; }

    [Required(ErrorMessage = "Atanan kişi seçiniz.")]
    [Range(1, int.MaxValue, ErrorMessage = "Atanan kişi seçiniz.")]
    public int? AssignedToUserId { get; set; }
    [ForeignKey(nameof(AssignedToUserId))]
    public AppUser? AssignedToUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required(ErrorMessage = "Son tarih zorunludur.")]
    [DataType(DataType.Date)]
    public DateTime? DueDate { get; set; }
    public DateTime? ClosedAt { get; set; }

    public ICollection<DofAction> Actions { get; set; } = new List<DofAction>();
    public ICollection<DofComment> Comments { get; set; } = new List<DofComment>();
    public ICollection<DofAttachment> Attachments { get; set; } = new List<DofAttachment>();    
    public bool IsOverdue => DueDate.HasValue && DateTime.UtcNow > DueDate.Value && Status != DofStatus.Kapatildi && Status != DofStatus.Reddedildi;

}