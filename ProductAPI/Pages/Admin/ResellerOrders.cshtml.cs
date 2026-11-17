using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProductAPI.Data;
using ProductAPI.Models;
using ProductAPI.Services;

namespace ProductAPI.Pages.Admin;

public class ResellerOrdersModel(AppDbContext db) : PageModel
{
    public List<ResellerOrder> Orders { get; private set; } = new();
    public string Error { get; private set; } = "";
    public async Task OnGetAsync() => Orders = await db.ResellerOrders.AsNoTracking()
        .Include(x => x.Reseller).Include(x => x.Items).Include(x => x.Shipments)
        .OrderByDescending(x => x.Id).Take(200).ToListAsync();

    public async Task<IActionResult> OnPostCancelAsync(long id)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var order = await db.ResellerOrders.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id);
        if (order is null) return NotFound();
        if (!CatalogRules.CanCancel(order.Status)) return await Fail("Only pending orders can be cancelled.");
        var ids = order.Items.Select(x => x.ProductId).ToArray();
        var products = await db.Products.Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
        foreach (var item in order.Items)
        {
            if (!products.TryGetValue(item.ProductId, out var p) || p.ReservedQuantity < item.Quantity)
                return await Fail("Reservation inconsistency; cancellation stopped.");
            p.ReservedQuantity -= item.Quantity;
        }
        order.Status = "Cancelled";
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostShipAsync(long id, string? carrier, string? tracking)
    {
        if (carrier?.Length > 120 || tracking?.Length > 120)
            return await Fail("Carrier and tracking number must be 120 characters or fewer.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var order = await db.ResellerOrders.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id);
        if (order is null) return NotFound();
        if (!CatalogRules.CanShip(order.Status)) return await Fail("Only pending orders can be shipped.");
        var ids = order.Items.Select(x => x.ProductId).ToArray();
        var products = await db.Products.Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
        foreach (var item in order.Items)
        {
            if (!products.TryGetValue(item.ProductId, out var p) || p.ReservedQuantity < item.Quantity ||
                p.StockQuantity < item.Quantity) return await Fail("Insufficient stock to ship.");
            p.ReservedQuantity -= item.Quantity;
            p.StockQuantity -= item.Quantity;
            db.StockMovements.Add(new StockMovement { ProductId=p.Id, Delta=-item.Quantity, Reason=$"Reseller order #{id} shipped" });
        }
        order.Status = "Shipped";
        db.OrderShipments.Add(new OrderShipment { ResellerOrderId=id, Carrier=carrier?.Trim(), TrackingNumber=tracking?.Trim() });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return RedirectToPage();
    }

    private async Task<IActionResult> Fail(string message)
    {
        Error = message; await OnGetAsync(); return Page();
    }
}
