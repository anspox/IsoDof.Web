using System.Security.Claims;
using IsoDof.Web.Models.Entities;

namespace IsoDof.Web.Services;

/// <summary>
/// Bir kullanıcının belirli bir DÖF kaydını görüp üzerinde işlem yapıp yapamayacağını belirleyen tek kural.
/// Admin: tüm kayıtlar. Kalite Kontrol: kendi departmanının kayıtları.
/// Diğer kullanıcılar: yalnızca açtıkları veya kendilerine atanan kayıtlar.
/// </summary>
public static class DofAccess
{
    public static bool CanAccess(ClaimsPrincipal user, Dof dof, int currentUserId, int? currentUserDepartmentId)
    {
        if (user.IsInRole("Admin"))
        {
            return true;
        }

        if (user.IsInRole("KaliteKontrol"))
        {
            // Liste sorgusuyla aynı kural: departmanı tanımlı olmayan kalite sorumlusu hiçbir kaydı göremez.
            return currentUserDepartmentId.HasValue && dof.DepartmentId == currentUserDepartmentId.Value;
        }

        return dof.AssignedToUserId == currentUserId || dof.CreatedByUserId == currentUserId;
    }
}
