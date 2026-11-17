using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProductAPI.Data;

namespace ProductAPI.Pages.Admin;

public class ReportsModel(AppDbContext db) : PageModel
{
    public int ProductCount { get; private set; }
    public decimal InventoryValue { get; private set; }
    public decimal OrderValue { get; private set; }
    public int OpenPurchaseOrders { get; private set; }
    public List<TopProduct> TopProducts { get; private set; } = new();
    public record TopProduct(string Sku, string Name, int Quantity, decimal Value);

    public async Task OnGetAsync()
    {
        ProductCount = await db.Products.CountAsync();
        InventoryValue = await db.Products.SumAsync(x => x.PurchasePrice * x.StockQuantity) + await (from v in db.ProductVariants join p in db.Products on v.ProductId equals p.Id select p.PurchasePrice * v.StockQuantity).SumAsync();
        OrderValue = await db.ResellerOrderItems.Select(x => x.UnitPrice * x.Quantity).SumAsync();
        OpenPurchaseOrders = await db.PurchaseOrders.CountAsync(x =>
            x.Status == "Draft" || x.Status == "Ordered" || x.Status == "PartiallyReceived");
        TopProducts = await (from item in db.ResellerOrderItems
            join product in db.Products on item.ProductId equals product.Id
            group new { item, product } by new { product.Sku, product.Name } into g
            orderby g.Sum(x => x.item.Quantity) descending
            select new TopProduct(g.Key.Sku, g.Key.Name,
                g.Sum(x => x.item.Quantity), g.Sum(x => x.item.UnitPrice * x.item.Quantity)))
            .Take(20).ToListAsync();
    }
}
