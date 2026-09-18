namespace IsoDof.Web.Services;

public class FileStorageService : IFileStorageService
{
    private static readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".docx", ".xlsx" };
    private const long MaxSizeBytes = 10 * 1024 * 1024; // 10 MB

    private readonly IWebHostEnvironment _env;

    public FileStorageService(IWebHostEnvironment env)
    {
        _env = env;
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

        var webRoot = string.IsNullOrEmpty(_env.WebRootPath) ? "wwwroot" : _env.WebRootPath;
        var uploadsFolder = Path.Combine(webRoot, "uploads", subFolder);
        Directory.CreateDirectory(uploadsFolder);

        var storedFileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, storedFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return FileUploadResult.Ok(storedFileName);
    }
}
