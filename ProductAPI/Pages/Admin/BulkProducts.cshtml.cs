using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProductAPI.Data;
using ProductAPI.Models;
using ProductAPI.Services;

namespace ProductAPI.Pages.Admin;

public class BulkProductsModel(AppDbContext db) : PageModel
{
    public string Error { get; private set; } = "";
    public string Message { get; private set; } = "";
    public void OnGet() { }

    public async Task<IActionResult> OnPostExportAsync()
    {
        var products = await db.Products.AsNoTracking().OrderBy(x => x.Sku).ToListAsync();
        return File(new UTF8Encoding(true).GetBytes(ProductCsvService.Export(products)), "text/csv", "products.csv");
    }

    public async Task<IActionResult> OnPostImportAsync(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length is <= 0 or > 2097152)
        {
            Error = "Upload a CSV file of at most 2 MB."; return Page();
        }
        List<string[]> rows;
        try
        {
            using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
            rows = ProductCsvService.Parse(await reader.ReadToEndAsync(ct));
        }
        catch (Exception ex) when (ex is FormatException or DecoderFallbackException)
        {
            Error = "Invalid CSV format."; return Page();
        }
        if (rows.Count < 2 || rows.Count > 5001 ||
            !rows[0].SequenceEqual(ProductCsvService.Header.Split(',')) ||
            rows.Skip(1).Any(x => x.Length != 10))
        {
            Error = "Invalid header or number of records/columns."; return Page();
        }
        var prepared = new List<Product>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows.Skip(1))
        {
            var sku = CatalogRules.NormalizeSku(row[0]);
            if (sku.Length is < 1 or > 80 || row[1].Length is < 1 or > 200 ||
                row[2].Length > 10000 || !seen.Add(sku) ||
                !int.TryParse(row[9], out var target) || target < 1 ||
                !decimal.TryParse(row[5], NumberStyles.Number, CultureInfo.InvariantCulture, out var purchase) || purchase < 0 ||
                !decimal.TryParse(row[6], NumberStyles.Number, CultureInfo.InvariantCulture, out var retail) || retail < 0 ||
                !decimal.TryParse(row[7], NumberStyles.Number, CultureInfo.InvariantCulture, out var reseller) || reseller < 0 ||
                row[8].Length != 3)
            {
                Error = $"Invalid row for SKU {sku}."; return Page();
            }
            int? category = null, supplier = null;
            if (row[3] != "") { if (!int.TryParse(row[3], out var c)) { Error="Invalid category ID."; return Page(); } category=c; }
            if (row[4] != "") { if (!int.TryParse(row[4], out var s)) { Error="Invalid supplier ID."; return Page(); } supplier=s; }
            prepared.Add(new Product { Sku=sku, Name=row[1].Trim(), Description=row[2],
                CategoryId=category, SupplierId=supplier, PurchasePrice=purchase,
                RetailPrice=retail, ResellerPrice=reseller, Currency=row[8].ToUpperInvariant(),
                TargetStockQuantity=target });
        }
        var categoryIds = prepared.Where(x => x.CategoryId.HasValue).Select(x => x.CategoryId!.Value).Distinct().ToArray();
        var supplierIds = prepared.Where(x => x.SupplierId.HasValue).Select(x => x.SupplierId!.Value).Distinct().ToArray();
        if (await db.ProductCategories.CountAsync(x => categoryIds.Contains(x.Id), ct) != categoryIds.Length ||
            await db.Suppliers.CountAsync(x => supplierIds.Contains(x.Id), ct) != supplierIds.Length)
        {
            Error = "One or more category or supplier IDs do not exist."; return Page();
        }
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var skus = prepared.Select(x => x.Sku).ToArray();
        var existing = await db.Products.Where(x => skus.Contains(x.Sku)).ToDictionaryAsync(x => x.Sku, ct);
        foreach (var p in prepared)
        {
            if (!existing.TryGetValue(p.Sku, out var current)) { db.Products.Add(p); continue; }
            current.Name=p.Name; current.Description=p.Description; current.CategoryId=p.CategoryId;
            current.SupplierId=p.SupplierId; current.PurchasePrice=p.PurchasePrice;
            current.RetailPrice=p.RetailPrice; current.ResellerPrice=p.ResellerPrice;
            current.Currency=p.Currency; current.TargetStockQuantity=p.TargetStockQuantity;
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        Message = $"Imported {prepared.Count} product records.";
        return Page();
    }
}
