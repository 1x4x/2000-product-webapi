using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProductAPI.Data;
using ProductAPI.Models;
namespace ProductAPI.Pages.Admin;
public class IndexModel(AppDbContext db):PageModel {
 public int Products,LowStock,Resellers,Tokens;public List<Product> Alerts=new();
 public async Task OnGetAsync(){Products=await db.Products.CountAsync();Resellers=await db.Resellers.CountAsync(x=>x.Active);Tokens=await db.ApiKeys.CountAsync(x=>!x.Revoked&&x.ExpiresUtc>DateTime.UtcNow);Alerts=await db.Products.Where(p=>p.Active&&p.TargetStockQuantity>0&&(p.StockQuantity-p.ReservedQuantity)*100<=p.TargetStockQuantity*p.ReorderThresholdPercent).OrderBy(p=>p.StockQuantity-p.ReservedQuantity).Take(50).ToListAsync();LowStock=await db.Products.CountAsync(p=>p.Active&&p.TargetStockQuantity>0&&(p.StockQuantity-p.ReservedQuantity)*100<=p.TargetStockQuantity*p.ReorderThresholdPercent);}
}
