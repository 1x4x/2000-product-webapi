using Microsoft.EntityFrameworkCore;
using ProductAPI.Models;
namespace ProductAPI.Data;
public class AppDbContext(DbContextOptions<AppDbContext> options):DbContext(options) {
 public DbSet<Product> Products=>Set<Product>(); public DbSet<ProductVariant> ProductVariants=>Set<ProductVariant>(); public DbSet<Warehouse> Warehouses=>Set<Warehouse>(); public DbSet<WarehouseStock> WarehouseStocks=>Set<WarehouseStock>(); public DbSet<OrderShipment> OrderShipments=>Set<OrderShipment>(); public DbSet<ProductImage> ProductImages=>Set<ProductImage>(); public DbSet<ProductCategory> ProductCategories=>Set<ProductCategory>(); public DbSet<Supplier> Suppliers=>Set<Supplier>(); public DbSet<Reseller> Resellers=>Set<Reseller>(); public DbSet<ApiKey> ApiKeys=>Set<ApiKey>(); public DbSet<StockMovement> StockMovements=>Set<StockMovement>(); public DbSet<ResellerOrder> ResellerOrders=>Set<ResellerOrder>(); public DbSet<ResellerOrderItem> ResellerOrderItems=>Set<ResellerOrderItem>(); public DbSet<PurchaseOrder> PurchaseOrders=>Set<PurchaseOrder>(); public DbSet<PurchaseOrderItem> PurchaseOrderItems=>Set<PurchaseOrderItem>();
 protected override void OnModelCreating(ModelBuilder b) {
  b.Entity<Product>().ToTable("product").HasIndex(x=>x.Sku).IsUnique(); b.Entity<ProductCategory>().ToTable("productcategory");
  b.Entity<Product>().Property(x=>x.PurchasePrice).HasPrecision(18,2); b.Entity<Product>().Property(x=>x.RetailPrice).HasPrecision(18,2); b.Entity<Product>().Property(x=>x.ResellerPrice).HasPrecision(18,2);
  b.Entity<ResellerOrderItem>().Property(x=>x.UnitPrice).HasPrecision(18,2); b.Entity<PurchaseOrderItem>().Property(x=>x.UnitCost).HasPrecision(18,2);
  b.Entity<ApiKey>().HasIndex(x=>x.Hash).IsUnique();
  b.Entity<ProductVariant>().ToTable("productvariant").HasIndex(x=>x.Sku).IsUnique();
  b.Entity<ProductVariant>().HasOne(x=>x.Product).WithMany(x=>x.Variants).HasForeignKey(x=>x.ProductId).OnDelete(DeleteBehavior.Cascade);
  b.Entity<ProductVariant>().Property(x=>x.RetailPrice).HasPrecision(18,2);
  b.Entity<ProductVariant>().Property(x=>x.ResellerPrice).HasPrecision(18,2);
  b.Entity<Warehouse>().ToTable("warehouse").HasIndex(x=>x.Code).IsUnique();
  b.Entity<WarehouseStock>().ToTable("warehousestock");
  b.Entity<WarehouseStock>().HasOne(x=>x.Product).WithMany().HasForeignKey(x=>x.ProductId).OnDelete(DeleteBehavior.Restrict);
  b.Entity<WarehouseStock>().HasOne(x=>x.ProductVariant).WithMany().HasForeignKey(x=>x.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
  b.Entity<OrderShipment>().ToTable("ordershipment").HasOne(x=>x.ResellerOrder).WithMany(x=>x.Shipments).HasForeignKey(x=>x.ResellerOrderId);
  b.Entity<ProductImage>().ToTable("productimage").HasOne(x=>x.Product).WithMany(x=>x.Images).HasForeignKey(x=>x.ProductId).OnDelete(DeleteBehavior.Cascade);
  b.Entity<ProductImage>().HasIndex(x=>new{x.ProductId,x.SortOrder});
 }
}
