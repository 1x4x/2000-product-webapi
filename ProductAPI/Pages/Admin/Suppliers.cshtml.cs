using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProductAPI.Data;
using ProductAPI.Models;
namespace ProductAPI.Pages.Admin;
public class SuppliersModel(AppDbContext db):PageModel {
 [BindProperty] public Supplier Input {get;set;}=new();
 public List<Supplier> Items {get;set;}=new();
 public string Error {get;set;}="";
 public async Task OnGetAsync()=>Items=await db.Suppliers.AsNoTracking().OrderBy(x=>x.Name).ToListAsync();
 public async Task<IActionResult> OnPostSaveAsync(){
  if(string.IsNullOrWhiteSpace(Input.Name)||Input.Name.Length>200||Input.LeadTimeDays<0||Input.LeadTimeDays>3650||Input.Email?.Length>250||Input.Country?.Length>100||Input.Notes?.Length>2000){Error="Invalid supplier information.";await OnGetAsync();return Page();}
  Supplier target;
  if(Input.Id==0){target=new Supplier();db.Suppliers.Add(target);}else{target=await db.Suppliers.FindAsync(Input.Id)??new Supplier();if(target.Id==0)return NotFound();}
  target.Name=Input.Name.Trim();target.Email=Input.Email?.Trim();target.Country=Input.Country?.Trim();target.ContactName=Input.ContactName?.Trim();target.Notes=Input.Notes?.Trim();target.LeadTimeDays=Input.LeadTimeDays;
  await db.SaveChangesAsync();return RedirectToPage();
 }
}
