namespace Conscript;

/// <summary>Items Sergei can find in the delivery truck glove box.</summary>
internal static class GloveCompartmentCatalog
{
    public const string Crowbar = GameItems.Crowbar;
    public const string Vodka = GameItems.Vodka;
    public const string Rag = GameItems.Rag;

    public readonly record struct Entry(
        string Name,
        string IconItemName,
        bool IsMoney,
        int MoneyAmount,
        string Flavor,
        string EffectHint);

    public static readonly Entry[] Entries =
    [
        new(
            Crowbar,
            Crowbar,
            IsMoney: false,
            MoneyAmount: 0,
            Flavor: "A short steel crowbar with old paint on the curve. Heavy enough to count as an argument.",
            EffectHint: "Goes in your backpack."),
        new(
            Vodka,
            Vodka,
            IsMoney: false,
            MoneyAmount: 0,
            Flavor: "A half-liter bottle, label torn, cap re-seated crooked. It burns just looking at it.",
            EffectHint: "Goes in your backpack."),
        new(
            Rag,
            Rag,
            IsMoney: false,
            MoneyAmount: 0,
            Flavor: "A greasy shop rag, stiff with old oil. Still better than your sleeve.",
            EffectHint: "Goes in your backpack."),
    ];

    public static int EntryCount => Entries.Length;
}
