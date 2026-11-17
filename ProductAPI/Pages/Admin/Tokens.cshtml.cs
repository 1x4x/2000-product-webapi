using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProductAPI.Data;
using ProductAPI.Models;
using ProductAPI.Services;
namespace ProductAPI.Pages.Admin;
public class TokensModel(AppDbContext db,ApiKeyService keys):PageModel {
 public static readonly string[] AvailableScopes=["products:read","inventory:read","orders:create","orders:read","products:write"];
 [BindProperty]public string Name {get;set;}="";[BindProperty]public int? ResellerId {get;set;}[BindProperty]public int Days {get;set;}=90;[BindProperty]public string[] Scopes {get;set;}=[];
 public List<Reseller> Resellers=new();public List<ApiKey> Items=new();public string? NewSecret;public string Error="";
 public async Task OnGetAsync()=>await Load();
 private async Task Load(){Resellers=await db.Resellers.Where(x=>x.Active).OrderBy(x=>x.Name).ToListAsync();Items=await db.ApiKeys.Include(x=>x.Reseller).OrderByDescending(x=>x.CreatedUtc).Take(200).ToListAsync();}
 public async Task<IActionResult> OnPostAsync(){if(string.IsNullOrWhiteSpace(Name)||Name.Length>150||Days<1||Days>3650||Scopes.Length==0||Scopes.Except(AvailableScopes).Any()||ResellerId!=null&&(!await db.Resellers.AnyAsync(x=>x.Id==ResellerId&&x.Active)||Scopes.Contains("products:write"))){Error="Invalid token settings.";await Load();return Page();}
 var (_,secret)=await keys.Create(Name,Days,Scopes,ResellerId,ResellerId==null);NewSecret=secret;await Load();return Page();}
 public async Task<IActionResult> OnPostRevokeAsync(int id){var token=await db.ApiKeys.FindAsync(id);if(token==null)return NotFound();token.Revoked=true;await db.SaveChangesAsync();return RedirectToPage();}
}
