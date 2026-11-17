using Microsoft.AspNetCore.Hosting;
namespace ProductAPI.Services;

/// <summary>Stores uploads beneath the app's wwwroot, using server-generated names only.</summary>
public class LocalProductImageStore(IWebHostEnvironment environment)
{
    private string Root => Path.Combine(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"), "uploads", "products");

    public async Task<(string fileName, string url)> SaveAsync(Stream source, string extension, CancellationToken ct)
    {
        Directory.CreateDirectory(Root);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var destination = Path.Combine(Root, fileName);
        await using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync:true);
        try { await source.CopyToAsync(output, ct); }
        catch { output.Close(); File.Delete(destination); throw; }
        return (fileName, $"/uploads/products/{fileName}");
    }

    public void Delete(string fileName)
    {
        if (fileName != Path.GetFileName(fileName) || string.IsNullOrWhiteSpace(fileName)) return;
        var path = Path.Combine(Root, fileName);
        if (File.Exists(path)) File.Delete(path);
    }
}
