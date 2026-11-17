using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProductAPI.Data;
using ProductAPI.Models;
namespace ProductAPI.Pages.Admin;
public class ResellersModel(AppDbContext db):PageModel {
 [BindProperty]public string Name {get;set;}="";[BindProperty]public string? Email {get;set;}public List<Reseller> Items=new();public async Task OnGetAsync()=>Items=await db.Resellers.OrderBy(x=>x.Name).ToListAsync();
 public async Task<IActionResult> OnPostAsync(){if(string.IsNullOrWhiteSpace(Name)||Name.Length>200)return BadRequest();db.Resellers.Add(new Reseller{Name=Name.Trim(),Email=Email});await db.SaveChangesAsync();return RedirectToPage();}
 public async Task<IActionResult> OnPostToggleAsync(int id){var r=await db.Resellers.FindAsync(id);if(r==null)return NotFound();r.Active=!r.Active;await db.SaveChangesAsync();return RedirectToPage();}
}
