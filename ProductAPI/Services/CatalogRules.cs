namespace ProductAPI.Services;

public static class CatalogRules
{
    public static bool CanTransfer(int availableAtSource, int quantity) =>
        quantity > 0 && availableAtSource >= quantity;

    public static bool CanCancel(string status) => status == "Pending";

    public static bool CanShip(string status) => status == "Pending";

    public static string NormalizeSku(string sku) => sku.Trim().ToUpperInvariant();

    public static bool IsValidVariant(int stock, int reserved, decimal retail, decimal reseller) =>
        stock >= 0 && reserved >= 0 && reserved <= stock && retail >= 0 && reseller >= 0;
}
