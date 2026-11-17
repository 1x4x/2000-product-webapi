using System.ComponentModel.DataAnnotations;

namespace ProductAPI.Models;

// Variant SKUs and stock are independent of the parent product's base SKU.
public class ProductVariant
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    [MaxLength(80)] public string Sku { get; set; } = "";
    [MaxLength(80)] public string? Color { get; set; }
    [MaxLength(80)] public string? Size { get; set; }
    [MaxLength(80)] public string? Material { get; set; }
    public decimal ResellerPrice { get; set; }
    public decimal RetailPrice { get; set; }
    public int StockQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public bool Active { get; set; } = true;
}
public class Warehouse
{
    public int Id { get; set; }
    [MaxLength(120)] public string Name { get; set; } = "";
    [MaxLength(80)] public string Code { get; set; } = "";
    [MaxLength(255)] public string? Address { get; set; }
}
public class WarehouseStock
{
    public int Id { get; set; }
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }
    [MaxLength(80)] public string? Bin { get; set; }
    public int Quantity { get; set; }
}
public class OrderShipment
{
    public long Id { get; set; }
    public long ResellerOrderId { get; set; }
    public ResellerOrder ResellerOrder { get; set; } = null!;
    [MaxLength(120)] public string? Carrier { get; set; }
    [MaxLength(120)] public string? TrackingNumber { get; set; }
    public DateTime ShippedUtc { get; set; } = DateTime.UtcNow;
}
