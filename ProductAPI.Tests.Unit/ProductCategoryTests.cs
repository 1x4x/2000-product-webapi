using ProductAPI.Models;
using Xunit;

namespace ProductAPI.Tests.Unit;

public class ProductCategoryTests
{
    [Fact]
    public void Category_CanHoldProducts()
    {
        var category = new ProductCategory { Id = 7, Name = "Adapters" };
        category.Products.Add(new Product { Sku = "USB-C-1", Name = "USB-C Adapter", CategoryId = 7 });
        Assert.Single(category.Products);
        Assert.Equal("Adapters", category.Name);
        Assert.Equal(7, category.Products[0].CategoryId);
    }

    [Fact]
    public void Product_CanBeUncategorized()
    {
        var product = new Product { Sku = "NO-CAT", Name = "Uncategorized" };
        Assert.Null(product.CategoryId);
    }
}
