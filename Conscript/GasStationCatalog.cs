namespace Conscript;

internal static class GasStationCatalog
{
    public static readonly string[] Entries = [GameItems.GasCan];

    public static string GetFlavorText(string name) => name switch
    {
        GameItems.GasCan =>
            "A red plastic jerry can, five liters, cap still tied to the handle. " +
            "Empty — you'll need to fill it at the pump.",
        _ => "Standard kiosk stock."
    };

    public static string FormatItemHint(string name) =>
        string.Equals(name, GameItems.GasCan, StringComparison.OrdinalIgnoreCase)
            ? "Goes in your backpack. Fill it for the truck."
            : "Goes in your backpack.";
}
