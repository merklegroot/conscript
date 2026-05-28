namespace Conscript;

/// <summary>Inventory item display names, charge limits, and embedded icon paths.</summary>
internal static class GameItems
{
    public const string BottledWater = "Bottled Water";
    public const string EmptyBottle = "Empty Bottle of Water";
    public const int BottledWaterMaxSips = 4;
    public const int BottledWaterHydrationPerSip = 25;

    public const string CannedSoup = "Canned Soup";
    public const string EmptyCan = "Empty Can";
    public const int CannedSoupMaxServings = 3;
    public const int CannedSoupSatiationPerServing = 12;
    public const int CannedSoupHydrationPerServing = 3;
    public const int CannedSoupHealthPerServing = 2;

    public const string TrashBags = "Trash Bags";
    public const string DuctTape = "Duct Tape";
    public const string Raccoon = "Raccoon";
    public const string Rabbit = "Rabbit";
    public const string Firewood = "Firewood";
    public const string Rocks = "Rocks";
    public const int TrashBagsMaxUses = 3;
    public const int DuctTapeMaxUses = 3;

    public const string LoafOfBread = "Loaf of Bread";
    public const string BurnerPhone = "Burner Phone";
    public const string Crowbar = "Crowbar";
    public const string Vodka = "Vodka";
    public const string Rag = "Rag";
    public const string Molotov = "Molotov";

    public static readonly Dictionary<string, string> IconFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Knife"]         = "items.knife.png",
        ["Lighter"]       = "items.lighter.png",
        ["Phone"]         = "items.phone.png",
        [BurnerPhone]     = "items.phone.png",
        [Crowbar]         = "items.crowbar.png",
        [Vodka]           = "items.vodka.png",
        [Rag]             = "items.rag.png",
        [Molotov]         = "items.vodka.png",
        [BottledWater]    = "items.bottled-water.png",
        [EmptyBottle]     = "items.empty-bottle.png",
        [LoafOfBread]     = "items.loaf-of-bread.png",
        [CannedSoup]      = "items.canned-soup.png",
        [EmptyCan]        = "items.empty-can.png",
        [TrashBags]       = "items.trash-bags.png",
        [DuctTape]        = "items.duct-tape.png",
        [Raccoon]         = "items.raccoon.png",
        [Rabbit]          = "items.rabbit.png",
        [Firewood]        = "items.firewood.png",
        [Rocks]           = "items.rocks.png",
    };

    public static int GetMaxCharges(string itemName)
    {
        if (string.Equals(itemName, BottledWater, StringComparison.OrdinalIgnoreCase))
            return BottledWaterMaxSips;
        if (string.Equals(itemName, CannedSoup, StringComparison.OrdinalIgnoreCase))
            return CannedSoupMaxServings;
        if (string.Equals(itemName, TrashBags, StringComparison.OrdinalIgnoreCase))
            return TrashBagsMaxUses;
        if (string.Equals(itemName, DuctTape, StringComparison.OrdinalIgnoreCase))
            return DuctTapeMaxUses;
        return 0;
    }

    public static bool IsBuildingMaterial(string name) =>
        string.Equals(name, TrashBags, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(name, DuctTape, StringComparison.OrdinalIgnoreCase);
}
