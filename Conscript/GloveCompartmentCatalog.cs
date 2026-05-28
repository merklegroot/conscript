namespace Conscript;

/// <summary>Items Sergei can find in the delivery truck glove box.</summary>
internal static class GloveCompartmentCatalog
{
    public const string CashEnvelope = "Cash Envelope";
    public const string BurnerPhone = "Burner Phone";

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
            CashEnvelope,
            CashEnvelope,
            IsMoney: true,
            MoneyAmount: 15_000,
            Flavor: "A grease-stained paper band holding mixed bills — probably the last driver's emergency float.",
            EffectHint: "Adds 15,000 ₽ to your money."),
        new(
            BurnerPhone,
            "Phone",
            IsMoney: false,
            MoneyAmount: 0,
            Flavor: "A scratched prepaid Nokia, still on. The last contact is labeled only \"B.\"",
            EffectHint: "Goes in your backpack."),
    ];

    public static int EntryCount => Entries.Length;
}
