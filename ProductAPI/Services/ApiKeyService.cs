using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ProductAPI.Data;
using ProductAPI.Models;
namespace ProductAPI.Services;
public class ApiKeyService(AppDbContext db) {
 public static string Hash(string key)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
 public async Task<(ApiKey Record,string Secret)> Create(string name,int days,string[] scopes,int? resellerId,bool admin=false) {
  if(days<1||days>3650) throw new ArgumentOutOfRangeException(nameof(days));
  var secret="pa_"+Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
  var record=new ApiKey {Name=name,Prefix=secret[..11],Hash=Hash(secret),ExpiresUtc=DateTime.UtcNow.AddDays(days),Scopes=string.Join(',',scopes.Distinct()),ResellerId=resellerId,IsAdmin=admin};
  db.ApiKeys.Add(record); await db.SaveChangesAsync(); return (record,secret);
 }
 public async Task<ApiKey?> Validate(string? secret,string scope) {
  if(string.IsNullOrWhiteSpace(secret)||!secret.StartsWith("pa_")||secret.Length>200) return null;
  var hash=Hash(secret); var key=await db.ApiKeys.Include(x=>x.Reseller).FirstOrDefaultAsync(x=>x.Hash==hash && !x.Revoked && x.ExpiresUtc>DateTime.UtcNow);
  if(key==null||(!key.IsAdmin && (key.Reseller==null||!key.Reseller.Active))||!key.Scopes.Split(',').Contains(scope)) return null;
  key.LastUsedUtc=DateTime.UtcNow; await db.SaveChangesAsync(); return key;
 }
}
