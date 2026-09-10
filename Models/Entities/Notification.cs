using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IsoDof.Web.Models.Entities;

/// <summary>
/// Kullanıcıya gösterilen uygulama içi bildirim (çan menüsü).
/// </summary>
public class Notification
{
    public int Id { get; set; }

    [Required]
    public int RecipientUserId { get; set; }
    [ForeignKey(nameof(RecipientUserId))]
    public AppUser? RecipientUser { get; set; }

    [Required]
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    /// <summary>Tıklanınca gidilecek göreli URL (örn. /Dofs/Details/5).</summary>
    [MaxLength(300)]
    public string? Url { get; set; }

    /// <summary>Material Symbols ikon adı.</summary>
    [MaxLength(50)]
    public string Icon { get; set; } = "notifications";

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
