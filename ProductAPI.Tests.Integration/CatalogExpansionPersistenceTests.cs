using Microsoft.EntityFrameworkCore;
using ProductAPI.Data;
using ProductAPI.Models;
using Xunit;

namespace ProductAPI.Tests.Integration;

public class CatalogExpansionPersistenceTests
{
    [Fact]
    public async Task VariantWarehouseAndShipmentRelationshipsPersist()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new AppDbContext(options);
        var product = new Product { Sku = "PARENT-1", Name = "Cable" };
        var variant = new ProductVariant { Sku = "CABLE-BLACK", Color = "Black", Product = product };
        var warehouse = new Warehouse { Code = "STO", Name = "Stockholm" };
        db.ProductVariants.Add(variant);
        db.WarehouseStocks.Add(new WarehouseStock { Product = product, Warehouse = warehouse, Bin = "A1", Quantity = 12 });
        await db.SaveChangesAsync();
        await using var verify = new AppDbContext(options);
        var loaded = await verify.Products.Include(x => x.Variants).SingleAsync();
        Assert.Single(loaded.Variants);
        Assert.Equal("Black", loaded.Variants[0].Color);
        Assert.Equal(12, await verify.WarehouseStocks.Select(x => x.Quantity).SingleAsync());
    }
}
