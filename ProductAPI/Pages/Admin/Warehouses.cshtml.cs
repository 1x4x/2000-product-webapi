using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProductAPI.Data;
using ProductAPI.Models;

namespace ProductAPI.Pages.Admin;

public class WarehousesModel(AppDbContext db) : PageModel
{
    public List<Warehouse> Warehouses { get; private set; } = new();
    public List<WarehouseStock> Stocks { get; private set; } = new();
    public List<Product> Products { get; private set; } = new();
    public string Error { get; private set; } = "";

    public async Task OnGetAsync()
    {
        Warehouses = await db.Warehouses.AsNoTracking().OrderBy(x => x.Code).ToListAsync();
        Stocks = await db.WarehouseStocks.AsNoTracking().Include(x => x.Warehouse).Include(x => x.Product)
            .OrderBy(x => x.Warehouse.Code).Take(500).ToListAsync();
        Products = await db.Products.AsNoTracking().OrderBy(x => x.Sku).Take(1000).ToListAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync(string code, string name, string? address)
    {
        code = (code ?? "").Trim().ToUpperInvariant();
        name = (name ?? "").Trim();
        if (code.Length is < 1 or > 80 || name.Length is < 1 or > 120 || address?.Length > 255 ||
            await db.Warehouses.AnyAsync(x => x.Code == code))
        {
            Error = "Invalid warehouse or duplicate code.";
            await OnGetAsync(); return Page();
        }
        db.Warehouses.Add(new Warehouse { Code = code, Name = name, Address = address?.Trim() });
        await db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostStockAsync(int warehouseId, int productId, string? bin, int quantity)
    {
        if (quantity < 0 || bin?.Length > 80 || !await db.Warehouses.AnyAsync(x => x.Id == warehouseId) ||
            !await db.Products.AnyAsync(x => x.Id == productId))
        {
            Error = "Invalid warehouse, product, bin or quantity.";
            await OnGetAsync(); return Page();
        }
        var normalizedBin = (bin ?? "").Trim();
        var stock = await db.WarehouseStocks.FirstOrDefaultAsync(x =>
            x.WarehouseId == warehouseId && x.ProductId == productId &&
            x.ProductVariantId == null && x.Bin == normalizedBin);
        if (stock is null)
        {
            stock = new WarehouseStock { WarehouseId = warehouseId, ProductId = productId, Bin = normalizedBin };
            db.WarehouseStocks.Add(stock);
        }
        stock.Quantity = quantity;
        await db.SaveChangesAsync();
        return RedirectToPage();
    }
}
