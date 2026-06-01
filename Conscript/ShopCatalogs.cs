namespace Conscript;

internal enum ShopKind
{
    Store,
    GasStation
}

internal static class ShopCatalogs
{
    public static string GetTitle(ShopKind kind) => kind switch
    {
        ShopKind.Store => "SHELVES",
        ShopKind.GasStation => "KIOSK",
        _ => "SHOP"
    };

    public static string[] GetEntries(ShopKind kind) => kind switch
    {
        ShopKind.Store => StoreCatalog.Entries,
        ShopKind.GasStation => GasStationCatalog.Entries,
        _ => []
    };

    public static string GetFlavorText(ShopKind kind, string name) => kind switch
    {
        ShopKind.Store => StoreCatalog.GetFlavorText(name),
        ShopKind.GasStation => GasStationCatalog.GetFlavorText(name),
        _ => ""
    };

    public static string FormatItemHint(ShopKind kind, string name) => kind switch
    {
        ShopKind.Store => StoreCatalog.FormatItemHint(name),
        ShopKind.GasStation => GasStationCatalog.FormatItemHint(name),
        _ => ""
    };

    public static string? GetFlavorTextForItem(string name)
    {
        if (Array.Exists(StoreCatalog.Entries, e => string.Equals(e, name, StringComparison.OrdinalIgnoreCase)))
            return StoreCatalog.GetFlavorText(name);

        if (Array.Exists(GasStationCatalog.Entries, e => string.Equals(e, name, StringComparison.OrdinalIgnoreCase)) ||
            string.Equals(name, GameItems.FilledGasCan, StringComparison.OrdinalIgnoreCase))
        {
            return GasStationCatalog.GetFlavorText(name);
        }

        return null;
    }

    public static string? GetItemHintForItem(string name)
    {
        if (Array.Exists(StoreCatalog.Entries, e => string.Equals(e, name, StringComparison.OrdinalIgnoreCase)))
            return StoreCatalog.FormatItemHint(name);

        if (Array.Exists(GasStationCatalog.Entries, e => string.Equals(e, name, StringComparison.OrdinalIgnoreCase)) ||
            string.Equals(name, GameItems.FilledGasCan, StringComparison.OrdinalIgnoreCase))
        {
            return GasStationCatalog.FormatItemHint(name);
        }

        return null;
    }
}
