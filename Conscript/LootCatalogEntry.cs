namespace Conscript;

internal readonly record struct LootCatalogEntry(
    string Name,
    string IconItemName,
    bool IsMoney,
    int MoneyAmount,
    string Flavor,
    string EffectHint);
