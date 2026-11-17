using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ProductAPI.Data;
using ProductAPI.Models;
using ProductAPI.Services;

var builder=WebApplication.CreateBuilder(args);
var connection=builder.Configuration.GetConnectionString("DefaultConnection");
if(string.IsNullOrWhiteSpace(connection)) throw new InvalidOperationException("Set ConnectionStrings__DefaultConnection via user secrets or environment variables.");
builder.Services.AddDbContext<AppDbContext>(o=>o.UseMySql(connection,ServerVersion.AutoDetect(connection)));
builder.Services.AddScoped<ApiKeyService>();
builder.Services.AddRazorPages(o=>o.Conventions.AuthorizeFolder("/Admin"));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o=>{o.LoginPath="/Login";o.Cookie.HttpOnly=true;o.Cookie.SecurePolicy=CookieSecurePolicy.Always;o.Cookie.SameSite=SameSiteMode.Strict;o.ExpireTimeSpan=TimeSpan.FromHours(8);});
builder.Services.AddAuthorization(); builder.Services.AddAntiforgery();
builder.Services.AddRateLimiter(o=>{o.RejectionStatusCode=429;o.AddFixedWindowLimiter("api",x=>{x.PermitLimit=120;x.Window=TimeSpan.FromMinutes(1);x.QueueLimit=0;});});
var app=builder.Build();
if(!app.Environment.IsDevelopment()){app.UseExceptionHandler("/Error");app.UseHsts();}
app.UseHttpsRedirection();app.UseStaticFiles();app.UseRouting();app.UseRateLimiter();app.UseAuthentication();app.UseAuthorization();app.MapRazorPages();
app.MapGet("/",()=>Results.Redirect("/admin"));
var api=app.MapGroup("/api/v1").RequireRateLimiting("api");
api.MapGet("/products",async(HttpContext http,AppDbContext db,ApiKeyService keys)=>{
 var key=await keys.Validate(http.Request.Headers["X-API-Key"],"products:read");if(key==null)return Results.Unauthorized();
 return Results.Ok(await db.Products.AsNoTracking().Where(p=>p.Active).Select(p=>new {p.Id,p.Sku,p.Name,p.Description,p.CategoryId,p.RetailPrice,p.ResellerPrice,p.Currency,AvailableQuantity=p.StockQuantity-p.ReservedQuantity}).ToListAsync());
});
api.MapGet("/products/{id:int}",async(int id,HttpContext http,AppDbContext db,ApiKeyService keys)=>{
 var key=await keys.Validate(http.Request.Headers["X-API-Key"],"products:read");if(key==null)return Results.Unauthorized();
 var p=await db.Products.AsNoTracking().Where(p=>p.Id==id&&p.Active).Select(p=>new {p.Id,p.Sku,p.Name,p.Description,p.CategoryId,p.RetailPrice,p.ResellerPrice,p.Currency,AvailableQuantity=p.StockQuantity-p.ReservedQuantity}).FirstOrDefaultAsync();return p==null?Results.NotFound():Results.Ok(p);
});
api.MapGet("/categories",async Task<IResult>(HttpContext http,AppDbContext db,ApiKeyService keys)=>{if(await keys.Validate(http.Request.Headers["X-API-Key"],"products:read") is null)return Results.Unauthorized();return Results.Ok(await db.ProductCategories.AsNoTracking().ToListAsync());});
api.MapGet("/inventory",async Task<IResult>(HttpContext http,AppDbContext db,ApiKeyService keys)=>{if(await keys.Validate(http.Request.Headers["X-API-Key"],"inventory:read") is null)return Results.Unauthorized();return Results.Ok(await db.Products.AsNoTracking().Where(x=>x.Active).Select(x=>new {x.Id,x.Sku,Available=x.StockQuantity-x.ReservedQuantity}).ToListAsync());});
api.MapGet("/me",async(HttpContext http,ApiKeyService keys)=>{var key=await keys.Validate(http.Request.Headers["X-API-Key"],"products:read");return key==null?Results.Unauthorized():Results.Ok(new {key.Name,key.ResellerId,key.IsAdmin,key.ExpiresUtc,key.Scopes});});
api.MapPost("/orders",async(CreateOrder request,HttpContext http,AppDbContext db,ApiKeyService keys)=>{
 var key=await keys.Validate(http.Request.Headers["X-API-Key"],"orders:create");if(key==null)return Results.Unauthorized();if(key.ResellerId==null)return Results.BadRequest("A reseller token is required.");
 if(request.Items==null||request.Items.Count==0||request.Items.Count>100||request.Items.Any(i=>i.Quantity<1||i.Quantity>10000)||request.Items.GroupBy(i=>i.ProductId).Any(g=>g.Count()>1))return Results.BadRequest("Invalid order items.");
 // SERIALIZABLE transaction prevents overselling from concurrent order requests.
 await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
 var ids=request.Items.Select(i=>i.ProductId).ToArray();var products=await db.Products.Where(p=>ids.Contains(p.Id)&&p.Active).ToDictionaryAsync(p=>p.Id);
 foreach(var item in request.Items)if(!products.TryGetValue(item.ProductId,out var p)||p.StockQuantity-p.ReservedQuantity<item.Quantity)return Results.Conflict("Insufficient stock or unavailable product.");
 var order=new ResellerOrder{ResellerId=key.ResellerId.Value};
 foreach(var item in request.Items){var p=products[item.ProductId];p.ReservedQuantity+=item.Quantity;order.Items.Add(new ResellerOrderItem{ProductId=p.Id,Quantity=item.Quantity,UnitPrice=p.ResellerPrice});}
 db.ResellerOrders.Add(order);await db.SaveChangesAsync();await tx.CommitAsync();return Results.Created($"/api/v1/orders/{order.Id}",new {order.Id,order.Status});
});
api.MapGet("/orders/{id:long}",async(long id,HttpContext http,AppDbContext db,ApiKeyService keys)=>{
 var key=await keys.Validate(http.Request.Headers["X-API-Key"],"orders:read");if(key==null)return Results.Unauthorized();var order=await db.ResellerOrders.AsNoTracking().Include(x=>x.Items).FirstOrDefaultAsync(x=>x.Id==id&&x.ResellerId==key.ResellerId);return order==null?Results.NotFound():Results.Ok(new {order.Id,order.Status,order.CreatedUtc,order.Items});
});
app.Run();
record OrderLine(int ProductId,int Quantity);
record CreateOrder(List<OrderLine> Items);
