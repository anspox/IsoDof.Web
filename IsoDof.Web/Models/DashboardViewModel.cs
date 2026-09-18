using IsoDof.Web.Models.Entities;

namespace IsoDof.Web.Models;

public class DashboardViewModel
{
    public int TotalCount { get; set; }
    public int OpenCount { get; set; }
    public int OverdueCount { get; set; }
    public int ClosedCount { get; set; }
    public List<DepartmentStat> ByDepartment { get; set; } = new();
    public List<StatusStat> ByStatus { get; set; } = new();

    // Rol ve Kullanıcı Bilgisi
    public string UserRole { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }

    // Kalite Kontrol ve Kullanıcı için Özet Listeler
    public List<Dof> UrgentDofs { get; set; } = new();
    public List<Dof> RecentDofs { get; set; } = new();

    // Çalışan İstatistikleri
    public int MyOpenedCount { get; set; }
    public int MyAssignedCount { get; set; }
    public int PendingActionsCount { get; set; }
}

public class DepartmentStat
{
    public string DepartmentName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class StatusStat
{
    public string StatusName { get; set; } = string.Empty;
    public int Count { get; set; }
}