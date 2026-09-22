namespace IsoDof.Web.Services;

public class FileUploadResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? StoredFileName { get; init; }

    public static FileUploadResult Fail(string message) => new() { Success = false, ErrorMessage = message };
    public static FileUploadResult Ok(string storedFileName) => new() { Success = true, StoredFileName = storedFileName };
}

public interface IFileStorageService
{
    Task<FileUploadResult> SaveAsync(IFormFile? file, string subFolder);

    /// <summary>Kayıtlı dosyanın tam yolunu döner; dosya yoksa null.</summary>
    string? GetPath(string subFolder, string storedFileName);
}
