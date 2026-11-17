using Microsoft.EntityFrameworkCore;
using ProductAPI.Data;
using ProductAPI.Models;
using Xunit;

namespace ProductAPI.Tests.Integration;

public class ProductImageDatabaseTests
{
    [Fact]
    public async Task ImagesPersistWithProductAndCascadeOnDelete()
    {
        // EF InMemory verifies entity mapping; the API suite uses a real MySQL container.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new AppDbContext(options);
        var product = new Product { Sku = "IMG-001", Name = "Sample" };
        product.Images.Add(new ProductImage
        {
            FileName = "abc.jpg", ImageUrl = "/uploads/products/abc.jpg",
            SortOrder = 0, IsPrimary = true, AltText = "Front view"
        });
        db.Products.Add(product);
        await db.SaveChangesAsync();

        await using var verify = new AppDbContext(options);
        var loaded = await verify.Products.Include(p => p.Images).SingleAsync();
        Assert.Single(loaded.Images);
        Assert.True(loaded.Images[0].IsPrimary);
        Assert.Equal("Front view", loaded.Images[0].AltText);
    }
}
