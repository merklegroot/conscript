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
                    IsMoney: false,
                    MoneyAmount: 0,
                    Flavor: "A cheap burner, screen spiderwebbed from the heat. Two missed calls from a number with no name.",
                    EffectHint: "Goes in your backpack."),
                new(
                    GameItems.FoldedPaper,
                    GameItems.FoldedPaper,
                    IsMoney: false,
                    MoneyAmount: 0,
                    Flavor: "A grease-stained half-sheet folded into his breast pocket. Handwriting in block letters — a note to someone named Vitya.",
                    EffectHint: "Goes in your backpack. Read it from your pack."),
                new(
                    "Knife",
                    "Knife",
                    IsMoney: false,
                    MoneyAmount: 0,
                    Flavor: "A kitchen knife with a taped handle. The blade is nicked but still sharp enough.",
                    EffectHint: "Goes in your backpack."),
                new(
                    "Cash",
                    "Knife",
                    IsMoney: true,
                    MoneyAmount: 12_000,
                    Flavor: "A roll of damp hundreds stuffed in the inside pocket of a scorched jacket.",
                    EffectHint: "Added to your money."),
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
                    IsMoney: false,
                    MoneyAmount: 0,
                    Flavor: "A steel lighter, still warm. Someone scratched initials into the bottom plate.",
                    EffectHint: "Goes in your backpack."),
                new(
                    "Cash",
                    "Knife",
                    IsMoney: true,
                    MoneyAmount: 8_500,
                    Flavor: "Folded bills in a clip, half-charred at the edges. Enough to hurt if you drop it.",
                    EffectHint: "Added to your money."),
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
