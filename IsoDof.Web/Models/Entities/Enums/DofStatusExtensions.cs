namespace IsoDof.Web.Models.Entities.Enums;

public static class DofStatusExtensions
{
    /// <summary>Durumun kullanıcıya gösterilen Türkçe adı.</summary>
    public static string ToLabel(this DofStatus status) => status switch
    {
        DofStatus.Acik => "Açık",
        DofStatus.Incelemede => "İncelemede",
        DofStatus.FaaliyetPlanlandi => "Faaliyet Planlandı",
        DofStatus.Kapatildi => "Kapatıldı",
        DofStatus.Reddedildi => "Reddedildi",
        _ => status.ToString()
    };
}
