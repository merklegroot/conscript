namespace Conscript;

/// <summary>Items inside the sealed wooden crate in Warehouse 14.</summary>
internal static class WarehouseCrateLootCatalog
{
    public static readonly LootCatalogEntry[] Entries =
    [
        new LootCatalogEntry(
            GameItems.Note,
            GameItems.Note,
            "A creased half-sheet folded into the straw packing. Block letters, no signature.",
            "Goes in your backpack. Read it from your pack."),
        new LootCatalogEntry(
            GameItems.Vodka,
            GameItems.Vodka,
            "A half-liter bottle tucked in straw packing. The label is gone; the glass is cold.",
            "Goes in your backpack."),
        new LootCatalogEntry(
            GameItems.Rag,
            GameItems.Rag,
            "A stiff shop rag, still damp with something that isn't water.",
            "Goes in your backpack."),
    ];

    public static int EntryCount => Entries.Length;
}
