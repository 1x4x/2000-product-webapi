using ProductAPI.Services;
using Xunit;

namespace ProductAPI.Tests.Unit;

public class StockConsistencyTests
{
    [Fact]
    public void TransferCannotExceedSourceLocation()
    {
        Assert.False(CatalogRules.CanTransfer(2, 3));
        Assert.True(CatalogRules.CanTransfer(2, 2));
    }

    [Fact]
    public void InvalidVariantStockIsRejected()
    {
        Assert.False(CatalogRules.IsValidVariant(5, 6, 20m, 10m));
        Assert.True(CatalogRules.IsValidVariant(5, 5, 20m, 10m));
    }

    [Fact]
    public void ShippedOrdersCannotBeCancelledOrShippedAgain()
    {
        Assert.False(CatalogRules.CanCancel("Shipped"));
        Assert.False(CatalogRules.CanShip("Shipped"));
    }
}
