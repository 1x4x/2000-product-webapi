using Microsoft.EntityFrameworkCore;
using ProductAPI.Models;
namespace ProductAPI.Data;
public class AppDbContext(DbContextOptions<AppDbContext> options):DbContext(options) {
 public DbSet<Product> Products=>Set<Product>(); public DbSet<ProductCategory> ProductCategories=>Set<ProductCategory>(); public DbSet<Supplier> Suppliers=>Set<Supplier>(); public DbSet<Reseller> Resellers=>Set<Reseller>(); public DbSet<ApiKey> ApiKeys=>Set<ApiKey>(); public DbSet<StockMovement> StockMovements=>Set<StockMovement>(); public DbSet<ResellerOrder> ResellerOrders=>Set<ResellerOrder>(); public DbSet<ResellerOrderItem> ResellerOrderItems=>Set<ResellerOrderItem>(); public DbSet<PurchaseOrder> PurchaseOrders=>Set<PurchaseOrder>(); public DbSet<PurchaseOrderItem> PurchaseOrderItems=>Set<PurchaseOrderItem>();
 protected override void OnModelCreating(ModelBuilder b) {
  b.Entity<Product>().ToTable("product").HasIndex(x=>x.Sku).IsUnique(); b.Entity<ProductCategory>().ToTable("productcategory");
  b.Entity<Product>().Property(x=>x.PurchasePrice).HasPrecision(18,2); b.Entity<Product>().Property(x=>x.RetailPrice).HasPrecision(18,2); b.Entity<Product>().Property(x=>x.ResellerPrice).HasPrecision(18,2);
  b.Entity<ResellerOrderItem>().Property(x=>x.UnitPrice).HasPrecision(18,2); b.Entity<PurchaseOrderItem>().Property(x=>x.UnitCost).HasPrecision(18,2);
  b.Entity<ApiKey>().HasIndex(x=>x.Hash).IsUnique();
 }
}
