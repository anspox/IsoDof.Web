using Microsoft.AspNetCore.StaticFiles;

namespace IsoDof.Web.Services;

/// <summary>
/// Yüklenen dosyaları web kökünün (wwwroot) DIŞINDA saklar; böylece dosyalar statik olarak
/// herkese açık sunulmaz, yalnızca yetki kontrolü yapan controller aksiyonları üzerinden indirilir.
/// Klasör "FileStorage:RootPath" ayarıyla değiştirilebilir (varsayılan: &lt;uygulama&gt;/App_Data/uploads).
/// </summary>
public class FileStorageService : IFileStorageService
{
    private static readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".docx", ".xlsx" };
    private const long MaxSizeBytes = 10 * 1024 * 1024; // 10 MB

    private static readonly FileExtensionContentTypeProvider ContentTypes = new();

    private readonly IWebHostEnvironment _env;
    private readonly string _rootPath;

    public FileStorageService(IWebHostEnvironment env, IConfiguration configuration)
    {
        _env = env;
        var configured = configuration["FileStorage:RootPath"];
        _rootPath = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(env.ContentRootPath, "App_Data", "uploads")
            : Path.GetFullPath(configured, env.ContentRootPath);
    }

    public async Task<FileUploadResult> SaveAsync(IFormFile? file, string subFolder)
    {
        if (file == null || file.Length == 0)
            return FileUploadResult.Fail("Lütfen bir dosya seçin.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return FileUploadResult.Fail("İzin verilmeyen dosya türü. (İzin verilenler: pdf, jpg, png, docx, xlsx)");

        if (file.Length > MaxSizeBytes)
            return FileUploadResult.Fail("Dosya boyutu 10 MB'ı geçemez.");

        var uploadsFolder = Path.Combine(_rootPath, subFolder);
        Directory.CreateDirectory(uploadsFolder);

        var storedFileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, storedFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return FileUploadResult.Ok(storedFileName);
    }

    public string? GetPath(string subFolder, string storedFileName)
    {
        // Yol enjeksiyonuna karşı yalnızca dosya adını kullan.
        var safeName = Path.GetFileName(storedFileName);
        if (string.IsNullOrEmpty(safeName)) return null;

        var path = Path.Combine(_rootPath, subFolder, safeName);
        if (File.Exists(path)) return path;

        // Geriye dönük uyumluluk: eski sürüm dosyaları wwwroot/uploads altına kaydediyordu.
        var webRoot = string.IsNullOrEmpty(_env.WebRootPath) ? Path.Combine(_env.ContentRootPath, "wwwroot") : _env.WebRootPath;
        var legacyPath = Path.Combine(webRoot, "uploads", subFolder, safeName);
        return File.Exists(legacyPath) ? legacyPath : null;
    }

    public static string GetContentType(string fileName) =>
        ContentTypes.TryGetContentType(fileName, out var type) ? type : "application/octet-stream";
}
