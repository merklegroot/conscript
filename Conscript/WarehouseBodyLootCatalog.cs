namespace Conscript;

/// <summary>Loot on the two bratdvas in the warehouse aftermath scene.</summary>
internal static class WarehouseBodyLootCatalog
{
    public const int BodyCount = 2;
    public const int MaxItemsPerBody = 4;

    public readonly record struct Body(
        string Title,
        string SearchHint,
        float RegionX,
        float RegionY,
        float RegionW,
        float RegionH,
        LootCatalogEntry[] Items);

    public static readonly Body[] Bodies =
    [
        new(
            Title: "BRATDVA BY THE DOOR",
            SearchHint: "Search the body",
            RegionX: 0.494f,
            RegionY: 0.633f,
            RegionW: 0.221f,
            RegionH: 0.101f,
            Items:
            [
                new(
                    GameItems.BurnerPhone,
                    GameItems.BurnerPhone,
                    "A cheap burner, screen spiderwebbed from the heat. Two missed calls from a number with no name.",
                    "Goes in your backpack."),
                new(
                    GameItems.FoldedPaper,
                    GameItems.FoldedPaper,
                    "A grease-stained half-sheet folded into his breast pocket. Handwriting in block letters — a note to someone named Vitya.",
                    "Goes in your backpack. Read it from your pack."),
                new(
                    "Knife",
                    "Knife",
                    "A kitchen knife with a taped handle. The blade is nicked but still sharp enough.",
                    "Goes in your backpack."),
            ]),
        new(
            Title: "BRATDVA ON THE CONCRETE",
            SearchHint: "Search the body",
            RegionX: 0.562f,
            RegionY: 0.757f,
            RegionW: 0.282f,
            RegionH: 0.126f,
            Items:
            [
                new(
                    "Lighter",
                    "Lighter",
                    "A steel lighter, still warm. Someone scratched initials into the bottom plate.",
                    "Goes in your backpack."),
            ]),
    ];

    public static int TotalLootCount
    {
        get
        {
            int count = 0;
            foreach (Body body in Bodies)
                count += body.Items.Length;

            return count;
        }
    }

    public static int ToGlobalIndex(int bodyIndex, int itemIndex)
    {
        int index = 0;
        for (int b = 0; b < bodyIndex; b++)
            index += Bodies[b].Items.Length;

        return index + itemIndex;
    }

    public static bool TryToBodyItemIndex(int globalIndex, out int bodyIndex, out int itemIndex)
    {
        bodyIndex = -1;
        itemIndex = -1;
        int cursor = 0;

        for (int b = 0; b < Bodies.Length; b++)
        {
            int itemCount = Bodies[b].Items.Length;
            if (globalIndex >= cursor && globalIndex < cursor + itemCount)
            {
                bodyIndex = b;
                itemIndex = globalIndex - cursor;
                return true;
            }

            cursor += itemCount;
        }

        return false;
    }
}
