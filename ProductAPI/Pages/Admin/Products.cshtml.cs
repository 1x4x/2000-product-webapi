using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProductAPI.Data;
using ProductAPI.Models;
namespace ProductAPI.Pages.Admin;
public class ProductsModel(AppDbContext db):PageModel {
 [BindProperty] public Product Input {get;set;}=new();
 public List<Product> Items {get;set;}=new(); public List<ProductCategory> Categories {get;set;}=new();public List<Supplier> Suppliers {get;set;}=new();
 public string Error {get;set;}="";
 public async Task OnGetAsync(){Items=await db.Products.AsNoTracking().Include(x=>x.Supplier).Include(x=>x.Category).OrderBy(x=>x.Name).Take(500).ToListAsync();Categories=await db.ProductCategories.AsNoTracking().OrderBy(x=>x.Name).ToListAsync();Suppliers=await db.Suppliers.AsNoTracking().OrderBy(x=>x.Name).ToListAsync();}
 public async Task<IActionResult> OnPostSaveAsync(){
  if(string.IsNullOrWhiteSpace(Input.Sku)||Input.Sku.Length>80||string.IsNullOrWhiteSpace(Input.Name)||Input.Name.Length>200||Input.PurchasePrice<0||Input.RetailPrice<0||Input.ResellerPrice<0||Input.TargetStockQuantity<1||Input.ReorderThresholdPercent<1||Input.ReorderThresholdPercent>100||Input.ReorderQuantity<0||Input.Currency?.Length!=3||Input.Description?.Length>10000){Error="Invalid product details.";await OnGetAsync();return Page();}
  var sku=Input.Sku.Trim();if(await db.Products.AnyAsync(x=>x.Sku==sku&&x.Id!=Input.Id)){Error="SKU already exists.";await OnGetAsync();return Page();}
  if(Input.SupplierId.HasValue&&!await db.Suppliers.AnyAsync(x=>x.Id==Input.SupplierId)||Input.CategoryId.HasValue&&!await db.ProductCategories.AnyAsync(x=>x.Id==Input.CategoryId)){Error="Unknown category or supplier.";await OnGetAsync();return Page();}
  Product target;if(Input.Id==0){target=new Product();db.Products.Add(target);}else{target=await db.Products.FindAsync(Input.Id)??new Product();if(target.Id==0)return NotFound();}
  target.Sku=sku;target.Name=Input.Name.Trim();target.Description=Input.Description?.Trim();target.PurchasePrice=Input.PurchasePrice;target.RetailPrice=Input.RetailPrice;target.ResellerPrice=Input.ResellerPrice;target.Currency=Input.Currency.ToUpperInvariant();target.CategoryId=Input.CategoryId;target.SupplierId=Input.SupplierId;target.TargetStockQuantity=Input.TargetStockQuantity;target.ReorderThresholdPercent=Input.ReorderThresholdPercent;target.ReorderQuantity=Input.ReorderQuantity;target.Active=Input.Active;
  // Stock is deliberately not editable here: use Inventory to record an audited movement.
  await db.SaveChangesAsync();return RedirectToPage();
 }
 [BindProperty] public string CategoryName {get;set;}="";
 public async Task<IActionResult> OnPostCategoryAsync(){if(string.IsNullOrWhiteSpace(CategoryName)||CategoryName.Length>120){Error="Invalid category.";await OnGetAsync();return Page();}var name=CategoryName.Trim();if(!await db.ProductCategories.AnyAsync(x=>x.Name==name)){db.ProductCategories.Add(new ProductCategory{Name=name});await db.SaveChangesAsync();}return RedirectToPage();}
}
