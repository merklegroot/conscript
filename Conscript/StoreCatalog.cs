namespace Conscript;

internal static class StoreCatalog
{
    public static readonly string[] Entries =
    [
        GameItems.BottledWater,
        GameItems.LoafOfBread,
        GameItems.CannedSoup,
        GameItems.TrashBags,
        GameItems.DuctTape,
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

    public static string FormatItemHint(string name) =>
        GameItems.IsBuildingMaterial(name) ? "Building material." : "Goes in your backpack.";
}
