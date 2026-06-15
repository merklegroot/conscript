namespace Conscript;

/// <summary>Wooden storage crate stacked against the cellar wall.</summary>
internal static class CafeBasementCrateDialog
{
    public const string Title = "STORAGE CRATE";

    public const string SealedDescription =
        "An old wooden crate tucked under the pipes. The lid is warped and the nails have pulled loose from the damp.";

    public const string OpenedWithPaper =
        "The lid hangs open on bent nails. Inside, folded into the straw packing: a blank sheet of paper.";

    public const string EmptyBody =
        "The lid hangs open on bent nails. Only straw packing remains — you already took what was inside.";

    public static string GetBodyText(bool opened, bool paperTaken)
    {
        if (!opened)
            return SealedDescription;

        if (!paperTaken)
            return OpenedWithPaper;

        return EmptyBody;
    }

    public static bool CanOpen(bool opened) => !opened;

    public static bool CanTake(bool opened, bool paperTaken) => opened && !paperTaken;
}
