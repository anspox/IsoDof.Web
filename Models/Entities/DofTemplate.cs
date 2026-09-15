using System.ComponentModel.DataAnnotations;
using IsoDof.Web.Models.Entities.Enums;

namespace IsoDof.Web.Models.Entities;

/// <summary>Sık açılan DÖF konuları için tek tıkla doldurulabilen hazır şablon.</summary>
public class DofTemplate
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Şablon adı zorunludur.")]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Başlık şablonu zorunludur.")]
    [MaxLength(200)]
    public string TitleTemplate { get; set; } = string.Empty;

    [Required(ErrorMessage = "Açıklama şablonu zorunludur.")]
    public string DescriptionTemplate { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tür seçiniz.")]
    public DofType Type { get; set; }

    [Required(ErrorMessage = "Kaynak seçiniz.")]
    public DofSource Source { get; set; }

    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
