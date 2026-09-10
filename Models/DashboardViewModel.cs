namespace IsoDof.Web.Models;

public class DashboardViewModel
{
    public int TotalCount { get; set; }
    public int OpenCount { get; set; }
    public int OverdueCount { get; set; }
    public int ClosedCount { get; set; }
    public List<DepartmentStat> ByDepartment { get; set; } = new();
    public List<StatusStat> ByStatus { get; set; } = new();
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