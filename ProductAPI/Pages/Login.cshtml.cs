using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace ProductAPI.Pages;
public class LoginModel(IConfiguration config):PageModel {
 [BindProperty] public string Username {get;set;}="";[BindProperty] public string Password {get;set;}="";public string Error {get;set;}="";
 public async Task<IActionResult> OnPostAsync(){
 var expectedUser=config["Admin:Username"];var expectedHash=config["Admin:PasswordSha256"];
 if(string.IsNullOrEmpty(expectedUser)||string.IsNullOrEmpty(expectedHash)) {Error="Admin credentials are not configured.";return Page();}
 var actual=SHA256.HashData(Encoding.UTF8.GetBytes(Password));byte[] expected;
 try{expected=Convert.FromHexString(expectedHash);}catch{Error="Invalid server credential configuration.";return Page();}
 if(Username!=expectedUser||expected.Length!=actual.Length||!CryptographicOperations.FixedTimeEquals(actual,expected)){Error="Invalid credentials.";return Page();}
 await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.Name,Username),new Claim(ClaimTypes.Role,"Admin")},CookieAuthenticationDefaults.AuthenticationScheme)));
 return RedirectToPage("/Admin/Index");
 }
}
