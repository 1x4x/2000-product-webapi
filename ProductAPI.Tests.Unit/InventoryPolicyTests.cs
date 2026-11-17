using ProductAPI.Services;
using Xunit;

namespace ProductAPI.Tests.Unit;

public class InventoryPolicyTests
{
    [Theory]
    [InlineData(20, 0, 100, 20, true)]
    [InlineData(21, 0, 100, 20, false)]
    [InlineData(25, 5, 100, 20, true)]
    [InlineData(50, 10, 200, 20, true)]
    [InlineData(1, 0, 0, 20, false)]
    [InlineData(0, 0, 100, 20, true)]
    public void ReorderThreshold_UsesAvailableStock(int onHand, int reserved, int target, int threshold, bool expected)
        => Assert.Equal(expected, InventoryPolicy.NeedsReorder(onHand, reserved, target, threshold));

    [Theory]
    [InlineData(18, 0, 100, 0, 82)]
    [InlineData(20, 5, 100, 0, 85)]
    [InlineData(90, 0, 100, 50, 50)]
    [InlineData(120, 0, 100, 0, 1)]
    public void ReorderQuantity_UsesFixedOrTargetTopUp(int stock, int reserved, int target, int fixedQty, int expected)
        => Assert.Equal(expected, InventoryPolicy.SuggestedReorder(stock, reserved, target, fixedQty));

    [Theory]
    [InlineData(10, 2, 8, true)]
    [InlineData(10, 2, 9, false)]
    [InlineData(10, 2, 0, false)]
    [InlineData(10, 10, 1, false)]
    public void Reservation_DoesNotOversell(int stock, int reserved, int quantity, bool expected)
        => Assert.Equal(expected, InventoryPolicy.CanReserve(stock, reserved, quantity));

    [Theory]
    [InlineData(10, 3, 7, true)]
    [InlineData(10, 3, 8, false)]
    [InlineData(10, 10, 1, false)]
    [InlineData(10, 3, -1, false)]
    public void Receipt_CannotExceedOrder(int ordered, int received, int quantity, bool expected)
        => Assert.Equal(expected, InventoryPolicy.CanReceive(ordered, received, quantity));

    [Fact]
    public void InvalidThreshold_Throws() => Assert.Throws<ArgumentOutOfRangeException>(() => InventoryPolicy.NeedsReorder(10, 0, 100, 101));

    [Fact]
    public void ApiKeyHash_IsStableAndNotPlaintext()
    {
        var hash = ApiKeyService.Hash("pa_example_key");
        Assert.Equal(hash, ApiKeyService.Hash("pa_example_key"));
        Assert.Equal(64, hash.Length);
        Assert.DoesNotContain("example", hash, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual(hash, ApiKeyService.Hash("pa_different_key"));
    }
}
