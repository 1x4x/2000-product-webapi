using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProductAPI.Data;
using ProductAPI.Models;
namespace ProductAPI.Pages.Admin;
public class PurchaseOrderDetailModel(AppDbContext db):PageModel {
 public PurchaseOrder Order {get;set;}=null!;public List<Product> AvailableProducts {get;set;}=new();public string Error {get;set;}="";
 public async Task<IActionResult> OnGetAsync(long id){var order=await db.PurchaseOrders.AsNoTracking().Include(x=>x.Supplier).Include(x=>x.Items).ThenInclude(x=>x.Product).FirstOrDefaultAsync(x=>x.Id==id);if(order==null)return NotFound();Order=order;AvailableProducts=await db.Products.AsNoTracking().Where(x=>x.SupplierId==order.SupplierId&&x.Active).OrderBy(x=>x.Name).ToListAsync();return Page();}
 private async Task<IActionResult> Fail(long id,string message){Error=message;return await OnGetAsync(id);}
 public async Task<IActionResult> OnPostAddAsync(long id,int productId,int quantity,decimal unitCost){
  if(quantity<1||quantity>1000000||unitCost<0||unitCost>100000000)return await Fail(id,"Invalid quantity or unit cost.");
  var po=await db.PurchaseOrders.Include(x=>x.Items).FirstOrDefaultAsync(x=>x.Id==id);if(po==null)return NotFound();if(po.Status!="Draft")return await Fail(id,"Only draft orders can be edited.");
  var product=await db.Products.FindAsync(productId);if(product==null||!product.Active||product.SupplierId!=po.SupplierId)return await Fail(id,"Product must belong to this supplier.");
  if(po.Items.Any(x=>x.ProductId==productId))return await Fail(id,"Product already appears on this order.");
  po.Items.Add(new PurchaseOrderItem{ProductId=productId,Quantity=quantity,UnitCost=unitCost});await db.SaveChangesAsync();return RedirectToPage(new{id});
 }
 public async Task<IActionResult> OnPostRemoveAsync(long id,long itemId){var po=await db.PurchaseOrders.Include(x=>x.Items).FirstOrDefaultAsync(x=>x.Id==id);if(po==null)return NotFound();if(po.Status!="Draft")return await Fail(id,"Only draft orders can be edited.");var item=po.Items.FirstOrDefault(x=>x.Id==itemId);if(item==null)return NotFound();db.PurchaseOrderItems.Remove(item);await db.SaveChangesAsync();return RedirectToPage(new{id});}
 public async Task<IActionResult> OnPostOrderAsync(long id,string? reference,string? notes){var po=await db.PurchaseOrders.Include(x=>x.Items).FirstOrDefaultAsync(x=>x.Id==id);if(po==null)return NotFound();if(po.Status!="Draft"||po.Items.Count==0)return await Fail(id,"Only non-empty draft orders can be marked as ordered.");if(reference?.Length>150||notes?.Length>2000)return await Fail(id,"Reference or notes too long.");po.Reference=reference?.Trim();po.Notes=notes?.Trim();po.Status="Ordered";po.OrderedUtc=DateTime.UtcNow;await db.SaveChangesAsync();return RedirectToPage(new{id});}
 public async Task<IActionResult> OnPostCancelAsync(long id){var po=await db.PurchaseOrders.FindAsync(id);if(po==null)return NotFound();if(po.Status!="Draft"&&po.Status!="Ordered")return await Fail(id,"Only open orders can be cancelled.");po.Status="Cancelled";await db.SaveChangesAsync();return RedirectToPage(new{id});}
 public async Task<IActionResult> OnPostReceiveAsync(long id,long itemId,int quantity){
  if(quantity<1||quantity>1000000)return await Fail(id,"Invalid receipt quantity.");
  await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
  var po=await db.PurchaseOrders.Include(x=>x.Items).FirstOrDefaultAsync(x=>x.Id==id);if(po==null)return NotFound();if(po.Status!="Ordered"&&po.Status!="PartiallyReceived")return await Fail(id,"Order is not awaiting a delivery.");
  var item=po.Items.FirstOrDefault(x=>x.Id==itemId);if(item==null)return NotFound();if((long)item.ReceivedQuantity+quantity>item.Quantity)return await Fail(id,"Receipt exceeds outstanding quantity.");
  var product=await db.Products.FindAsync(item.ProductId);if(product==null||(long)product.StockQuantity+quantity>int.MaxValue)return await Fail(id,"Product missing or stock exceeds allowed maximum.");
  item.ReceivedQuantity+=quantity;product.StockQuantity+=quantity;db.StockMovements.Add(new StockMovement{ProductId=product.Id,Delta=quantity,Reason=$"Purchase order #{po.Id} receipt (line #{item.Id})"});
  if(po.Items.All(x=>x.ReceivedQuantity==x.Quantity)){po.Status="Received";po.ReceivedUtc=DateTime.UtcNow;}else po.Status="PartiallyReceived";
  await db.SaveChangesAsync();await tx.CommitAsync();return RedirectToPage(new{id});
 }
}
