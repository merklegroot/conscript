namespace Conscript;

internal enum JoystickLayoutKind
{
    Generic,
    SteamVirtual,
    SteamDeck
}

/// <summary>Maps menu actions to raw GLFW joystick button/hat indices for Valve controllers.</summary>
internal static class JoystickLayout
{
    private const int HatUp = 1;
    private const int HatRight = 2;
    private const int HatDown = 4;
    private const int HatLeft = 8;

    public static JoystickLayoutKind Detect(string? joystickName)
    {
        if (string.IsNullOrWhiteSpace(joystickName))
            return JoystickLayoutKind.Generic;

        if (joystickName.Contains("Deck", StringComparison.OrdinalIgnoreCase))
            return JoystickLayoutKind.SteamDeck;

        if (joystickName.Contains("Steam", StringComparison.OrdinalIgnoreCase) ||
            joystickName.Contains("Valve", StringComparison.OrdinalIgnoreCase))
            return JoystickLayoutKind.SteamVirtual;

        return JoystickLayoutKind.Generic;
    }

    public static bool TryGetFaceButton(JoystickLayoutKind layout, JoystickAction action, out int button)
    {
        button = layout switch
        {
            JoystickLayoutKind.SteamDeck => action switch
            {
                JoystickAction.Confirm => 3,
                JoystickAction.Cancel => 4,
                _ => -1
            },
            JoystickLayoutKind.SteamVirtual or JoystickLayoutKind.Generic => action switch
            {
                JoystickAction.Confirm => 0,
                JoystickAction.Cancel => 1,
                _ => -1
            },
            _ => -1
        };

        return button >= 0;
    }

    public static bool TryGetDpadButton(JoystickLayoutKind layout, JoystickAction action, out int button)
    {
        button = layout switch
        {
            JoystickLayoutKind.SteamDeck => action switch
            {
                JoystickAction.Up => 16,
                JoystickAction.Down => 17,
                JoystickAction.Left => 18,
                JoystickAction.Right => 19,
                _ => -1
            },
            _ => -1
        };

        return button >= 0;
    }

    public static bool IsHatDirection(JoystickLayoutKind layout, JoystickAction action, int hatValue)
    {
        if (layout != JoystickLayoutKind.SteamVirtual && layout != JoystickLayoutKind.Generic)
            return false;

        return action switch
        {
            JoystickAction.Up => (hatValue & HatUp) != 0,
            JoystickAction.Down => (hatValue & HatDown) != 0,
            JoystickAction.Left => (hatValue & HatLeft) != 0,
            JoystickAction.Right => (hatValue & HatRight) != 0,
            _ => false
        };
    }

    public static bool TryGetStickAxes(JoystickLayoutKind layout, out int axisX, out int axisY)
    {
        axisX = 0;
        axisY = 1;
        return layout is JoystickLayoutKind.SteamDeck or JoystickLayoutKind.SteamVirtual or JoystickLayoutKind.Generic;
    }
}

internal enum JoystickAction
{
    Confirm,
    Cancel,
    Up,
    Down,
    Left,
    Right
}
