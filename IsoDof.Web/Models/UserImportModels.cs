namespace IsoDof.Web.Models;

/// <summary>Yüklenen dosyadan okunan ham satır.</summary>
public class UserImportRow
{
    public int RowNumber { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Department { get; set; }
    public string? Role { get; set; }
    public string? SicilNo { get; set; }
    public string? Password { get; set; }
}

/// <summary>Bir satırın işlenme sonucu.</summary>
public class UserImportRowResult
{
    public int RowNumber { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public bool Success { get; set; }
    public string? Error { get; set; }
}

public class UserImportResultViewModel
{
    public int TotalRows { get; set; }
    public int AddedCount { get; set; }
    public int SkippedCount => Results.Count(r => !r.Success);
    public string DefaultPassword { get; set; } = "";
    public List<UserImportRowResult> Results { get; set; } = new();
    public bool HasRun { get; set; }
}
