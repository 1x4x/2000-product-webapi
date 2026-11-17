using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProductAPI.Data;
using ProductAPI.Models;\nusing ProductAPI.Services;

namespace ProductAPI.Pages.Admin;

public class WarehousesModel(AppDbContext db) : PageModel
{
    public List<Warehouse> Warehouses { get; private set; } = new();
    public List<WarehouseStock> Stocks { get; private set; } = new();
    public List<Product> Products { get; private set; } = new(); public List<ProductVariant> Variants { get; private set; } = new();
    public string Error { get; private set; } = "";

    public async Task OnGetAsync()
    {
        Warehouses = await db.Warehouses.AsNoTracking().OrderBy(x => x.Code).ToListAsync();
        Stocks = await db.WarehouseStocks.AsNoTracking().Include(x => x.Warehouse).Include(x => x.Product).Include(x => x.ProductVariant)
            .OrderBy(x => x.Warehouse.Code).Take(500).ToListAsync();
        Products = await db.Products.AsNoTracking().OrderBy(x => x.Sku).Take(1000).ToListAsync(); Variants = await db.ProductVariants.AsNoTracking().OrderBy(x => x.Sku).Take(1000).ToListAsync();
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

    public async Task<IActionResult> OnPostTransferAsync(int fromStockId, int toWarehouseId, int quantity, string? toBin)
    {
        if (quantity < 1 || toBin?.Length > 80) { Error = "Invalid transfer quantity or bin."; await OnGetAsync(); return Page(); }
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var source = await db.WarehouseStocks.FindAsync(fromStockId);
        if (source is null || !CatalogRules.CanTransfer(source.Quantity, quantity) ||
            !await db.Warehouses.AnyAsync(x => x.Id == toWarehouseId))
        { Error = "Insufficient source stock or invalid warehouse."; await OnGetAsync(); return Page(); }
        var bin = (toBin ?? "").Trim();
        if (source.WarehouseId == toWarehouseId && source.Bin == bin)
        { Error = "Source and destination must differ."; await OnGetAsync(); return Page(); }
        var dest = await db.WarehouseStocks.FirstOrDefaultAsync(x =>
            x.WarehouseId == toWarehouseId && x.ProductId == source.ProductId &&
            x.ProductVariantId == source.ProductVariantId && x.Bin == bin);
        if (dest is null)
        {
            dest = new WarehouseStock { WarehouseId = toWarehouseId, ProductId = source.ProductId,
                ProductVariantId = source.ProductVariantId, Bin = bin };
            db.WarehouseStocks.Add(dest);
        }
        if ((long)dest.Quantity + quantity > int.MaxValue)
        { Error = "Destination stock overflow."; await OnGetAsync(); return Page(); }
        source.Quantity -= quantity;
        dest.Quantity += quantity;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostStockAsync(int warehouseId, int productId, int? variantId, string? bin, int quantity)
    {
        if (quantity < 0 || bin?.Length > 80 || !await db.Warehouses.AnyAsync(x => x.Id == warehouseId) ||
            !await db.Products.AnyAsync(x => x.Id == productId))
        {
            Error = "Invalid warehouse, product, bin or quantity.";
            await OnGetAsync(); return Page();
        }
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var product = await db.Products.FindAsync(productId);
        var variant = variantId.HasValue ? await db.ProductVariants.FindAsync(variantId.Value) : null;
        if (product is null || (variantId.HasValue && (variant is null || variant.ProductId != productId)))
        {
            Error = "Invalid product variant."; await OnGetAsync(); return Page();
        }
        var normalizedBin = (bin ?? "").Trim();
        var stock = await db.WarehouseStocks.FirstOrDefaultAsync(x =>
            x.WarehouseId == warehouseId && x.ProductId == productId &&
            x.ProductVariantId == variantId && x.Bin == normalizedBin);
        if (stock is null)
        {
            stock = new WarehouseStock { WarehouseId = warehouseId, ProductId = productId, ProductVariantId = variantId, Bin = normalizedBin };
            db.WarehouseStocks.Add(stock);
        }
        var delta = quantity - stock.Quantity;
        var current = variant is null ? product.StockQuantity : variant.StockQuantity;
        var reserved = variant is null ? product.ReservedQuantity : variant.ReservedQuantity;
        if ((long)current + delta < reserved || (long)current + delta > int.MaxValue)
        {
            Error = "Location count would reduce available stock below reservations.";
            await tx.RollbackAsync(); await OnGetAsync(); return Page();
        }
        if (variant is null)
        {
            product.StockQuantity += delta;
            if (delta != 0) db.StockMovements.Add(new StockMovement { ProductId = productId, Delta = delta, Reason = $"Warehouse {warehouseId}, bin {normalizedBin}" });
        }
        else variant.StockQuantity += delta;
        stock.Quantity = quantity;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return RedirectToPage();
    }
}
