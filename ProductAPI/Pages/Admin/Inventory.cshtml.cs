using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProductAPI.Data;
using ProductAPI.Models;
namespace ProductAPI.Pages.Admin;
public class InventoryModel(AppDbContext db):PageModel {
 public List<Product> Items=new();public string Error {get;set;}="";public async Task OnGetAsync()=>Items=await db.Products.OrderBy(x=>x.Name).Take(200).ToListAsync();
 public async Task<IActionResult> OnPostAsync(int productId,int delta,string reason){await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);var p=await db.Products.FindAsync(productId);if(p==null||delta==0||string.IsNullOrWhiteSpace(reason)||reason.Length>300||((long)p.StockQuantity+delta)<p.ReservedQuantity||((long)p.StockQuantity+delta)>int.MaxValue){Error="Invalid adjustment or insufficient unreserved stock.";await tx.RollbackAsync();await OnGetAsync();return Page();}p.StockQuantity+=delta;db.StockMovements.Add(new StockMovement{ProductId=p.Id,Delta=delta,Reason=reason});await db.SaveChangesAsync();await tx.CommitAsync();return RedirectToPage();}
}
