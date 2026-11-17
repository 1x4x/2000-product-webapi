using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProductAPI.Data;
using ProductAPI.Models;
using ProductAPI.Services;

namespace ProductAPI.Pages.Admin;

public class VariantsModel(AppDbContext db) : PageModel
{
    public Product Product { get; private set; } = null!;
    public List<ProductVariant> Variants { get; private set; } = new();
    public string Error { get; private set; } = "";

    public async Task<IActionResult> OnGetAsync(int id) => await Load(id);
    private async Task<IActionResult> Load(int id)
    {
        var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (product is null) return NotFound();
        Product = product;
        Variants = await db.ProductVariants.AsNoTracking().Where(x => x.ProductId == id).OrderBy(x => x.Sku).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync(int id, string sku, string? color, string? size,
        string? material, decimal retail, decimal reseller)
    {
        sku = CatalogRules.NormalizeSku(sku ?? "");
        if (sku.Length is < 1 or > 80 || color?.Length > 80 || size?.Length > 80 ||
            material?.Length > 80 || retail < 0 || reseller < 0 ||
            await db.ProductVariants.AnyAsync(x => x.Sku == sku) ||
            await db.Products.AnyAsync(x => x.Sku == sku))
        {
            Error = "Invalid or duplicate variant SKU, attributes or prices.";
            return await Load(id);
        }
        if (!await db.Products.AnyAsync(x => x.Id == id)) return NotFound();
        db.ProductVariants.Add(new ProductVariant
        {
            ProductId = id, Sku = sku, Color = color?.Trim(), Size = size?.Trim(),
            Material = material?.Trim(), RetailPrice = retail, ResellerPrice = reseller
        });
        await db.SaveChangesAsync();
        return RedirectToPage(new { id });
    }
}
