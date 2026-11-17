using ProductAPI.Models;
using ProductAPI.Services;
using Xunit;

namespace ProductAPI.Tests.Unit;

public class CatalogRulesTests
{
    [Theory]
    [InlineData(10, 10, true)]
    [InlineData(10, 11, false)]
    [InlineData(10, 0, false)]
    public void TransfersCannotOverdraw(int available, int qty, bool expected)
        => Assert.Equal(expected, CatalogRules.CanTransfer(available, qty));

    [Theory]
    [InlineData("Pending", true)]
    [InlineData("Shipped", false)]
    [InlineData("Cancelled", false)]
    public void CancellationIsLimitedToPendingOrders(string status, bool expected)
        => Assert.Equal(expected, CatalogRules.CanCancel(status));

    [Fact]
    public void CsvRoundTripPreservesCommasAndQuotes()
    {
        var csv = ProductCsvService.Export(new[] { new Product { Sku="A1", Name="Cable, \"Premium\"", TargetStockQuantity=10 } });
        var rows = ProductCsvService.Parse(csv);
        Assert.Equal(ProductCsvService.Header.Split(','), rows[0]);
        Assert.Equal("Cable, \"Premium\"", rows[1][1]);
    }

    [Fact]
    public void VariantCannotReserveMoreThanStock()
        => Assert.False(CatalogRules.IsValidVariant(2, 3, 10, 8));
}
