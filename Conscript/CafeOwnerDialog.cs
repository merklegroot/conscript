namespace Conscript;

/// <summary>
/// Industrial-district café owner — sketchy fixer tied to local crime.
/// Dialogue only for now; later choices may help the player or set up betrayal.
/// </summary>
internal static class CafeOwnerDialog
{
    public const int OptionCount = 4;

    public const string Title = "БОРИС";
    public const string Subtitle = "Bratva — café owner";
    public const string IdleText =
        "A wiry man behind the counter watches you without blinking. \"Talk. Then I decide if you're trouble.\"";
    public const string PickPrompt = "What do you say?";

    public static readonly string[] PlayerLines =
    [
        "\"Do the patrols come through here?\"",
        "\"I need work — off the books.\"",
        "\"I need to lie low for a few days.\"",
        "\"Just passing through.\""
    ];

    public static readonly string[] OwnerReplies =
    [
        "He taps ash into a saucer. \"Sometimes. I know which door they use and who takes money to look away. That can help you — or bury you, if you cross me.\"",
        "A thin smile. \"I move things that shouldn't move. Maybe I point you at a job. Maybe I sell you to someone paying more. Show me you're useful.\"",
        "\"I hide people when it suits me. I also turn them in when it pays better. Don't ask for favors you can't afford to owe.\"",
        "He laughs once, without warmth. \"Nobody 'just passes through' my café. Sit down, buy tea, and pray I decide you're worth keeping around.\""
    ];

    public static string GetResponseText(int selectedOption) =>
        selectedOption >= 0 && selectedOption < OptionCount
            ? OwnerReplies[selectedOption]
            : IdleText;
}
