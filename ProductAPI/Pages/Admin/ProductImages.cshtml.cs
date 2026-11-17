using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProductAPI.Data;
using ProductAPI.Models;
using ProductAPI.Services;

namespace ProductAPI.Pages.Admin;

public class ProductImagesModel(AppDbContext db, LocalProductImageStore store) : PageModel
{
    public Product Product { get; private set; } = null!;
    public List<ProductImage> Images { get; private set; } = new();
    public string Error { get; private set; } = "";

    public async Task<IActionResult> OnGetAsync(int id) => await LoadAsync(id);

    private async Task<IActionResult> LoadAsync(int id)
    {
        var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (product is null) return NotFound();
        Product = product;
        Images = await db.ProductImages.AsNoTracking().Where(x => x.ProductId == id)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync();
        return Page();
    }

    private async Task<IActionResult> FailAsync(int id, string error)
    {
        Error = error;
        return await LoadAsync(id);
    }

    public async Task<IActionResult> OnPostUploadAsync(int id, List<IFormFile> files, CancellationToken ct)
    {
        if (files is null || files.Count == 0 || files.Count > ProductImagePolicy.MaxImages)
            return await FailAsync(id, "Select 1 to 20 images.");

        // Buffer and validate all images before changing the database or disk.
        var prepared = new List<(byte[] bytes, string extension, string alt)>();
        foreach (var file in files)
        {
            if (file.Length <= 0 || file.Length > ProductImagePolicy.MaxBytes)
                return await FailAsync(id, "Each image must be between 1 byte and 5 MB.");
            await using var stream = file.OpenReadStream();
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, ct);
            if (memory.Length > ProductImagePolicy.MaxBytes)
                return await FailAsync(id, "Image too large.");
            var bytes = memory.ToArray();
            var extension = ProductImagePolicy.DetectExtension(bytes);
            if (extension is null)
                return await FailAsync(id, "Only JPEG, PNG and WebP images are supported.");
            prepared.Add((bytes, extension, Path.GetFileNameWithoutExtension(file.FileName)[..Math.Min(100, Path.GetFileNameWithoutExtension(file.FileName).Length)]));
        }

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (!await db.Products.AnyAsync(p => p.Id == id, ct)) return NotFound();
        var existing = await db.ProductImages.Where(x => x.ProductId == id)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync(ct);
        if (!ProductImagePolicy.CanAdd(existing.Count, prepared.Count))
        {
            await tx.RollbackAsync(ct);
            return await FailAsync(id, "This product may have at most 20 images.");
        }

        var saved = new List<string>();
        try
        {
            var sort = existing.Count == 0 ? 0 : existing.Max(x => x.SortOrder) + 1;
            foreach (var item in prepared)
            {
                await using var input = new MemoryStream(item.bytes);
                var (fileName, url) = await store.SaveAsync(input, item.extension, ct);
                saved.Add(fileName);
                db.ProductImages.Add(new ProductImage
                {
                    ProductId = id, FileName = fileName, ImageUrl = url,
                    AltText = item.alt, SortOrder = sort++, IsPrimary = existing.Count == 0 && saved.Count == 1
                });
            }
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            foreach (var name in saved) store.Delete(name);
            throw;
        }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostPrimaryAsync(int id, long imageId)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var images = await db.ProductImages.Where(x => x.ProductId == id).ToListAsync();
        if (!images.Any(x => x.Id == imageId)) return NotFound();
        foreach (var image in images) image.IsPrimary = image.Id == imageId;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAltAsync(int id, long imageId, string? altText)
    {
        if (altText?.Length > 255) return await FailAsync(id, "Alt text must be 255 characters or fewer.");
        var image = await db.ProductImages.FirstOrDefaultAsync(x => x.ProductId == id && x.Id == imageId);
        if (image is null) return NotFound();
        image.AltText = altText?.Trim();
        await db.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostMoveAsync(int id, long imageId, int direction)
    {
        if (direction is not (-1 or 1)) return BadRequest();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var images = await db.ProductImages.Where(x => x.ProductId == id)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync();
        var index = images.FindIndex(x => x.Id == imageId);
        if (index < 0) return NotFound();
        var other = index + direction;
        if (other >= 0 && other < images.Count)
        {
            (images[index].SortOrder, images[other].SortOrder) = (images[other].SortOrder, images[index].SortOrder);
            await db.SaveChangesAsync();
        }
        await tx.CommitAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostReorderAsync(int id, string order)
    {
        var ids = (order ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (ids.Length > ProductImagePolicy.MaxImages || ids.Any(x => !long.TryParse(x, out _)))
            return BadRequest();
        var parsed = ids.Select(long.Parse).ToArray();
        if (parsed.Distinct().Count() != parsed.Length) return BadRequest();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var images = await db.ProductImages.Where(x => x.ProductId == id).ToListAsync();
        if (parsed.Length != images.Count || images.Any(x => !parsed.Contains(x.Id)))
            return BadRequest();
        for (var i = 0; i < parsed.Length; i++)
            images.Single(x => x.Id == parsed[i]).SortOrder = i;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, long imageId)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var images = await db.ProductImages.Where(x => x.ProductId == id)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync();
        var image = images.FirstOrDefault(x => x.Id == imageId);
        if (image is null) return NotFound();
        db.ProductImages.Remove(image);
        if (image.IsPrimary && images.Count > 1)
            images.First(x => x.Id != imageId).IsPrimary = true;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        store.Delete(image.FileName);
        return RedirectToPage(new { id });
    }
}
