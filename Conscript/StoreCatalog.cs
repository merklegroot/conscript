namespace Conscript;

internal static class StoreCatalog
{
    public static readonly (string name, int price, int satiationDelta, int hydrationDelta, int healthDelta)[] Entries =
    [
        (GameItems.BottledWater,  65,   0, +18, +2),
        (GameItems.LoafOfBread, 140, +22,  +2, +3),
        (GameItems.CannedSoup,  195, +28,  +8, +5),
        (GameItems.TrashBags,    85,   0,   0,  0),
        (GameItems.DuctTape,    120,   0,   0,  0),
    ];

    public static string GetFlavorText(string name) => name switch
    {
        GameItems.BottledWater => "A plastic bottle of still water from the cooler.",
        GameItems.LoafOfBread => "A dense loaf, still soft enough to tear by hand.",
        GameItems.CannedSoup => "Tinned soup — heat is optional when you are desperate.",
        GameItems.TrashBags => "Heavy-duty plastic bags. Building material for improvised shelter.",
        GameItems.DuctTape => "Strong adhesive tape. Building material — pairs with trash bags for a crude tent.",
        _ => "Standard kiosk stock."
    };

    public static string FormatEffects(string name, int satiation, int hydration, int health)
    {
        if (GameItems.IsBuildingMaterial(name))
            return "Building material.";

        var parts = new List<string>();
        if (satiation > 0)
            parts.Add($"Food +{satiation}");
        if (hydration > 0)
            parts.Add($"Hydration +{hydration}");
        if (health > 0)
            parts.Add($"Health +{health}");
        return parts.Count > 0 ? string.Join("  ·  ", parts) : "No immediate stat effect.";
    }
}
