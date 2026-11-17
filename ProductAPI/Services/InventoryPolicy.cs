namespace ProductAPI.Services;

/// <summary>Pure inventory rules shared by the UI and purchasing workflow.</summary>
public static class InventoryPolicy
{
    public static int Available(int onHand, int reserved) => checked(onHand - reserved);

    public static bool NeedsReorder(int onHand, int reserved, int target, int thresholdPercent)
    {
        if (target <= 0) return false;
        if (thresholdPercent is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(thresholdPercent));
        return (long)Available(onHand, reserved) * 100 <= (long)target * thresholdPercent;
    }

    public static int SuggestedReorder(int onHand, int reserved, int target, int fixedQuantity)
    {
        if (target < 1) throw new ArgumentOutOfRangeException(nameof(target));
        if (fixedQuantity < 0) throw new ArgumentOutOfRangeException(nameof(fixedQuantity));
        return fixedQuantity > 0 ? fixedQuantity : (int)Math.Min(int.MaxValue, Math.Max(1L, (long)target - Available(onHand, reserved)));
    }

    public static bool CanReserve(int onHand, int reserved, int requested) =>
        requested > 0 && onHand >= 0 && reserved >= 0 && (long)reserved + requested <= onHand;

    public static bool CanReceive(int ordered, int received, int quantity) =>
        ordered > 0 && received >= 0 && received <= ordered && quantity > 0 && (long)received + quantity <= ordered;
}
