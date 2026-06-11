namespace Conscript;

/// <summary>Boris (Bratva café) conversation — includes delivery job offer to Warehouse 14.</summary>
internal static class CafeOwnerDialog
{
    public enum Stage
    {
        Main,
        DeliveryOffer
    }

    public const int MainOptionCount = 3;
    public const int DeliveryOptionCount = 2;
    public const int WorkOptionIndex = 0;

    public const string Title = "БОРИС";
    public const string Subtitle = "Bratva — café owner";
    public const string WarehouseName = "Warehouse 14";
    public const string IdleText =
        "A wiry man behind the counter watches you without blinking. \"Talk. Then I decide if you're trouble.\"";

    public const string IdleTextAfterBetrayalSurvived =
        "The cup in his hand stops halfway to his lips. For a second his face has no expression at all — " +
        "then the color drains out of it. \"You.\" He sets the cup down too carefully. " +
        "\"You're supposed to be in a ditch behind " + WarehouseName + ".\"";

    public const string PickPrompt = "What do you say?";
    public const string DeliveryPickPrompt = "Do you take the job?";

    public const string DeliveryOfferText =
        "He leans in. \"Delivery run tonight. You drive the truck to " + WarehouseName +
        " on the west yards — loading bay three. No questions, no stops. Fifty thousand rubles when the cargo is inside. Yes or no?\"";

    public const string DeliveryDeclineReply =
        "He exhales smoke. \"Then stop wasting my time unless you're buying tea.\"";

    public const string DeliveryAcceptedInDialog =
        "\"Good. Keys are outside. Drive straight there — I'll know if you detour.\"";

    public static readonly string[] MainPlayerLines =
    [
        "\"I need work — off the books.\"",
        "\"I need to lie low for a few days.\"",
        "\"Just passing through.\""
    ];

    public static readonly string[] MainOwnerReplies =
    [
        "A thin smile. \"I move things that shouldn't move. Maybe I point you at a job. Maybe I sell you to someone paying more. Show me you're useful.\"",
        "\"I hide people when it suits me. I also turn them in when it pays better. Don't ask for favors you can't afford to owe.\"",
        "He laughs once, without warmth. \"Nobody 'just passes through' my café. Sit down, buy tea, and pray I decide you're worth keeping around.\""
    ];

    public static readonly string[] MainOwnerRepliesAfterBetrayal =
    [
        "\"Work?\" He laughs, but it's brittle. \"I sent you to die and you're asking for a job? " +
        "Either you're the luckiest fool in Buryatia or the stupidest. Sit. Don't touch anything.\"",
        "\"Lie low?\" His eyes flick to the door. \"Half the bratva heard about the bay by now. " +
        "You walk in here like nothing happened — what do you want from me, a blanket and a blessing?\"",
        "\"Passing through.\" He goes very quiet. \"You killed my people, took my truck, and you're passing through my café. " +
        "Drink your tea before I decide what that costs you.\""
    ];

    public const string ActiveJobReplyAfterBetrayal =
        "His jaw tightens. \"My men don't answer their phones. The bay's still smoking. " +
        "And you drove back like it was a grocery run.\" He doesn't sit. \"We have nothing left to discuss about that delivery.\"";

    public static readonly string[] DeliveryPlayerLines =
    [
        "\"I'll drive it.\"",
        "\"Not tonight.\""
    ];

    public static int GetOptionCount(Stage stage) =>
        stage == Stage.DeliveryOffer ? DeliveryOptionCount : MainOptionCount;

    public static string[] GetPlayerLines(Stage stage) =>
        stage == Stage.DeliveryOffer ? DeliveryPlayerLines : MainPlayerLines;

    public static string GetResponseText(
        Stage stage,
        int selectedOption,
        bool deliveryJobActive,
        bool warehouseAmbushersDead)
    {
        if (stage == Stage.DeliveryOffer)
        {
            return selectedOption switch
            {
                0 => DeliveryAcceptedInDialog,
                1 => DeliveryDeclineReply,
                _ => DeliveryOfferText
            };
        }

        if (selectedOption < 0)
        {
            return warehouseAmbushersDead ? IdleTextAfterBetrayalSurvived : IdleText;
        }

        if (selectedOption == WorkOptionIndex && deliveryJobActive)
        {
            return warehouseAmbushersDead
                ? ActiveJobReplyAfterBetrayal
                : "\"You're already on the delivery. Drive to " + WarehouseName + " — bay three. Don't keep me waiting.\"";
        }

        if (warehouseAmbushersDead && selectedOption < MainOptionCount)
            return MainOwnerRepliesAfterBetrayal[selectedOption];

        return selectedOption < MainOptionCount
            ? MainOwnerReplies[selectedOption]
            : IdleText;
    }
}
