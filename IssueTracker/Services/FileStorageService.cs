using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

public class UploadOptions
{
    public string Root { get; set; } = "wwwroot/uploads";               
    public int MaxFileSizeMb { get; set; } = 5;                        
    public string[] AllowedExtensions { get; set; } = new[] { ".jpg", ".jpeg", ".png" };
}

public class FileStorageService
{
    private readonly string _root;
    private readonly UploadOptions _opt;

    public FileStorageService(IWebHostEnvironment env, IOptions<UploadOptions> options)
    {
        _opt = options.Value;
        _root = Path.Combine(env.ContentRootPath, _opt.Root);
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(IFormFile file)
    {
        if (file is null || file.Length == 0)
            throw new InvalidOperationException("No file uploaded");

        // --- Validation ---
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext) || !_opt.AllowedExtensions.Contains(ext))
            throw new InvalidOperationException("File type not allowed");

        var maxBytes = _opt.MaxFileSizeMb * 1024 * 1024;
        if (file.Length > maxBytes)
            throw new InvalidOperationException("File too large");

        // --- Save ---
        var safeName = MakeSafeFileName(file.FileName);
        var unique = $"{Path.GetFileNameWithoutExtension(safeName)}-{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(_root, unique);

        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Return web-relative URL path (served from wwwroot/uploads)
        return $"/uploads/{unique}";
    }

    private static string MakeSafeFileName(string name)
    {
        name = name.Trim();
        name = Regex.Replace(name, @"\s+", "-");
        name = Regex.Replace(name, @"[^A-Za-z0-9\.\-_\(\)]", "");
        return string.IsNullOrWhiteSpace(name) ? "upload.bin" : name;
    }
}
