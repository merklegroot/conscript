namespace Conscript;

internal static class GasStationCatalog
{
    public static readonly string[] Entries = [GameItems.GasCan];

    public static string GetFlavorText(string name) => name switch
    {
        GameItems.GasCan =>
            "A red plastic jerry can, five liters, cap still tied to the handle. " +
            "Empty — fill it at a pump.",
        GameItems.FilledGasCan =>
            "The jerry can is full of diesel, sloshing when you tilt it. " +
            "Heavy enough to get the truck moving again.",
        _ => "Standard kiosk stock."
    };

    public static string FormatItemHint(string name) =>
        name switch
        {
            _ when string.Equals(name, GameItems.GasCan, StringComparison.OrdinalIgnoreCase) =>
                "Goes in your backpack. Fill it at a pump.",
            _ when string.Equals(name, GameItems.FilledGasCan, StringComparison.OrdinalIgnoreCase) =>
                "Ready to pour into the truck.",
            _ => "Goes in your backpack."
        };
}
