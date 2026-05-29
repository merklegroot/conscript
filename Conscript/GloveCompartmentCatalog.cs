namespace Conscript;

/// <summary>Items Sergei can find in the delivery truck glove box.</summary>
internal static class GloveCompartmentCatalog
{
    public const string Crowbar = GameItems.Crowbar;
    public const string Vodka = GameItems.Vodka;
    public const string Rag = GameItems.Rag;

    public static readonly LootCatalogEntry[] Entries =
    [
        new LootCatalogEntry(
            Crowbar,
            Crowbar,
            "A short steel crowbar with old paint on the curve. Heavy enough to count as an argument.",
            "Goes in your backpack."),
        new LootCatalogEntry(
            Vodka,
            Vodka,
            "A half-liter bottle, label torn, cap re-seated crooked. It burns just looking at it.",
            "Goes in your backpack."),
        new LootCatalogEntry(
            Rag,
            Rag,
            "A greasy shop rag, stiff with old oil. Still better than your sleeve.",
            "Goes in your backpack."),
    ];

    public static int EntryCount => Entries.Length;
}
