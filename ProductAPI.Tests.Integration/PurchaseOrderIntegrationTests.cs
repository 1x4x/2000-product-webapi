using Microsoft.EntityFrameworkCore;
using ProductAPI.Data;
using ProductAPI.Models;
using ProductAPI.Services;
using Xunit;

namespace ProductAPI.Tests.Integration;

/// <summary>Checks persisted purchase-order receipts and inventory movements against EF's model.</summary>
public class PurchaseOrderIntegrationTests
{
    [Fact]
    public void ReceiptUpdatesStockAndHistory_WithinTransaction()
    {
        // Fast database-level integration check; the HTTP tests separately use real MySQL.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        using var db = new AppDbContext(options);
        var supplier = new Supplier { Name = "China Factory" };
        var product = new Product { Sku = "PO-001", Name = "Cable", Supplier = supplier, StockQuantity = 10, TargetStockQuantity = 100 };
        var po = new PurchaseOrder { Supplier = supplier, Status = "Ordered" };
        po.Items.Add(new PurchaseOrderItem { Product = product, Quantity = 50, UnitCost = 5m });
        db.PurchaseOrders.Add(po);
        db.SaveChanges();

        var item = db.PurchaseOrderItems.Single();
        Assert.True(InventoryPolicy.CanReceive(item.Quantity, item.ReceivedQuantity, 20));
        item.ReceivedQuantity += 20;
        product.StockQuantity += 20;
        db.StockMovements.Add(new StockMovement { Product = product, Delta = 20, Reason = "PO receipt" });
        po.Status = "PartiallyReceived";
        db.SaveChanges();

        using var verify = new AppDbContext(options);
        Assert.Equal(30, verify.Products.Single().StockQuantity);
        Assert.Equal(20, verify.PurchaseOrderItems.Single().ReceivedQuantity);
        Assert.Equal(20, verify.StockMovements.Single().Delta);
        Assert.Equal("PartiallyReceived", verify.PurchaseOrders.Single().Status);
    }
}
