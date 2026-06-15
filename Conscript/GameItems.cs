namespace Conscript;

/// <summary>Inventory item display names, charge limits, and embedded icon paths.</summary>
internal static class GameItems
{
    public const string BottledWater = "Bottled Water";
    public const string EmptyBottle = "Empty Bottle of Water";
    public const int BottledWaterMaxSips = 4;

    public const string CannedSoup = "Canned Soup";
    public const string EmptyCan = "Empty Can";
    public const int CannedSoupMaxServings = 3;

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
    public const string FoldedPaper = "Folded Paper";
    public const string Note = "Note";
    public const string BlankPaper = "Blank Paper";
    public const string Knife = "Knife";
    public const string Molotov = "Molotov";
    public const string LitMolotov = "Lit Molotov";
    public const string GasCan = "Gas Can";
    public const string FilledGasCan = "Filled Gas Can";

    public const string FoldedPaperMessage =
        "For the last time, Vitya,\n" +
        "Tomorrow is the day.\n" +
        "Treason or not, it will be done.\n" +
        "Severe consequences will follow.";

    public const string CrateNoteFile = "crate-note.png";
    public const string FoldedPaperNoteFile = "folded-paper-note.png";
    public const string BlankPaperNoteFile = "blank-sheet-note.png";

    public static readonly Dictionary<string, string> IconFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Knife"]         = "items.knife.png",
        ["Lighter"]       = "items.lighter.png",
        ["Phone"]         = "items.phone.png",
        [BurnerPhone]     = "items.phone.png",
        [Crowbar]         = "items.crowbar.png",
        [Vodka]           = "items.vodka.png",
        [Rag]             = "items.rag.png",
        [FoldedPaper]     = FoldedPaperNoteFile,
        [Note]            = CrateNoteFile,
        [BlankPaper]      = BlankPaperNoteFile,
        [Molotov]         = "items.molotov.png",
        [LitMolotov]      = "items.lit-molotov.png",
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
        [GasCan]          = "items.gas-can.png",
        [FilledGasCan]    = "items.gas-can.png",
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

    public static bool IsFoldedPaper(string name) =>
        string.Equals(name, FoldedPaper, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(name, Note, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(name, BlankPaper, StringComparison.OrdinalIgnoreCase);

    public static string GetReadableNoteImageFile(string itemName)
    {
        if (string.Equals(itemName, Note, StringComparison.OrdinalIgnoreCase))
            return CrateNoteFile;
        if (string.Equals(itemName, BlankPaper, StringComparison.OrdinalIgnoreCase))
            return BlankPaperNoteFile;
        return FoldedPaperNoteFile;
    }

    /// <summary>Backpack items that should not show a USE action (empty containers, build-only materials).</summary>
    public static bool IsExcludedFromUse(string name) =>
        string.Equals(name, EmptyCan, StringComparison.OrdinalIgnoreCase) ||
        IsBuildingMaterial(name);

    /// <summary>Items that can harm someone when guided onto a scene target.</summary>
    public static bool IsWeapon(string name) =>
        string.Equals(name, Knife, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(name, Crowbar, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(name, Molotov, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(name, LitMolotov, StringComparison.OrdinalIgnoreCase);

    public static bool IsGasCanItem(string? name) =>
        !string.IsNullOrEmpty(name) &&
        (string.Equals(name, GasCan, StringComparison.OrdinalIgnoreCase) ||
         string.Equals(name, FilledGasCan, StringComparison.OrdinalIgnoreCase));
}
