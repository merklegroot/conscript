namespace Conscript;

/// <summary>Sealed wooden crate in the warehouse interior aisle.</summary>
internal static class WarehouseCrateDialog
{
    public const string Title = "WOODEN CRATE";

    public const string Description =
        "A heavy shipping crate, banded with rusted steel. The lid is nailed shut and stamped with faded handling marks you can't read.";

    public const string NoToolHint =
        "The nails are sunk flush. You'd need something with leverage — a crowbar, maybe — to force it open.";

    public const string OpenedBody =
        "The lid hangs crooked on bent nails. Whatever was inside is already gone — or never made it this far.";

    public const string OpenActionLabel = "PRY OPEN";

    public static string GetBodyText(bool hasCrowbar, bool opened)
    {
        if (opened)
            return OpenedBody;

        if (hasCrowbar)
            return Description;

        return Description + "\n\n" + NoToolHint;
    }
}
