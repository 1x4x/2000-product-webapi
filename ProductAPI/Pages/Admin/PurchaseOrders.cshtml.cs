using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProductAPI.Data;
using ProductAPI.Models;
namespace ProductAPI.Pages.Admin;
public class PurchaseOrdersModel(AppDbContext db):PageModel {
 public List<PurchaseOrder> Orders {get;set;}=new();public List<Product> LowStock {get;set;}=new();public List<Supplier> Suppliers {get;set;}=new();
 public string Error {get;set;}="";
 public async Task OnGetAsync(){Orders=await db.PurchaseOrders.AsNoTracking().Include(x=>x.Supplier).Include(x=>x.Items).OrderByDescending(x=>x.Id).Take(100).ToListAsync();var products=await db.Products.AsNoTracking().Include(x=>x.Supplier).Where(x=>x.Active&&x.SupplierId!=null).ToListAsync();LowStock=products.Where(x=>(long)(x.StockQuantity-x.ReservedQuantity)*100<=(long)x.TargetStockQuantity*x.ReorderThresholdPercent).ToList();Suppliers=await db.Suppliers.AsNoTracking().OrderBy(x=>x.Name).ToListAsync();}
 private async Task<IActionResult> Fail(string error){Error=error;await OnGetAsync();return Page();}
 public async Task<IActionResult> OnPostFromProductAsync(int productId){
  var p=await db.Products.FindAsync(productId);if(p==null||!p.Active||p.SupplierId==null)return await Fail("Product must be active and assigned to a supplier.");
  if((long)(p.StockQuantity-p.ReservedQuantity)*100>(long)p.TargetStockQuantity*p.ReorderThresholdPercent)return await Fail("Product is not below its reorder threshold.");
  var open=await db.PurchaseOrderItems.AnyAsync(i=>i.ProductId==productId&&(i.PurchaseOrder.Status=="Draft"||i.PurchaseOrder.Status=="Ordered"));if(open)return await Fail("An open purchase order already exists for this product. Review it before ordering again.");
  var qty=p.ReorderQuantity>0?p.ReorderQuantity:Math.Max(1,p.TargetStockQuantity-(p.StockQuantity-p.ReservedQuantity));
  var po=new PurchaseOrder{SupplierId=p.SupplierId.Value,Items=new(){new PurchaseOrderItem{ProductId=p.Id,Quantity=qty,UnitCost=p.PurchasePrice}}};db.PurchaseOrders.Add(po);await db.SaveChangesAsync();return RedirectToPage("/Admin/PurchaseOrderDetail",new{id=po.Id});
 }
 [BindProperty]public int SupplierId {get;set;}
 public async Task<IActionResult> OnPostCreateAsync(){if(!await db.Suppliers.AnyAsync(x=>x.Id==SupplierId))return await Fail("Select a valid supplier.");var po=new PurchaseOrder{SupplierId=SupplierId};db.PurchaseOrders.Add(po);await db.SaveChangesAsync();return RedirectToPage("/Admin/PurchaseOrderDetail",new{id=po.Id});}
}
