namespace Conscript;

internal readonly record struct LootCatalogEntry(
    string Name,
    string IconItemName,
    string Flavor,
    string EffectHint);
