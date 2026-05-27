using Raylib_cs;

namespace Conscript;

/// <summary>
/// Keeps SDL gamepad mappings in sync — especially for Steam Deck / Steam Input reconnects.
/// Steam's <c>SDL_GAMECONTROLLERCONFIG</c> must win over bundled mappings when present.
/// </summary>
internal static class GamepadConnection
{
    public const int MaxSlots = 16;

    private const int RemapIntervalFrames = 30;

    private static int _framesWithoutPad;

    public static void Initialize() => ApplyMappings();

    public static void Update()
    {
        if (AnyConnected())
        {
            _framesWithoutPad = 0;
            return;
        }

        _framesWithoutPad++;
        if (_framesWithoutPad >= RemapIntervalFrames)
        {
            _framesWithoutPad = 0;
            ApplyMappings();
        }
    }

    public static bool AnyConnected()
    {
        for (int i = 0; i < MaxSlots; i++)
        {
            if (Raylib.IsGamepadAvailable(i))
                return true;
        }

        return false;
    }

    public static int ConnectedCount()
    {
        int count = 0;
        for (int i = 0; i < MaxSlots; i++)
        {
            if (Raylib.IsGamepadAvailable(i))
                count++;
        }

        return count;
    }

    public static int FirstConnectedIndex()
    {
        for (int i = 0; i < MaxSlots; i++)
        {
            if (Raylib.IsGamepadAvailable(i))
                return i;
        }

        return -1;
    }

    public static bool HasSteamControllerConfig =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SDL_GAMECONTROLLERCONFIG"));

    public static void ApplyMappings()
    {
        string? steamConfig = Environment.GetEnvironmentVariable("SDL_GAMECONTROLLERCONFIG");
        if (!string.IsNullOrWhiteSpace(steamConfig))
        {
            // Launched via Steam: use only Steam's mapping so we don't clobber it with bundled fallbacks.
            string normalized = steamConfig.Trim().Replace(';', '\n');
            Raylib.SetGamepadMappings(normalized);
            return;
        }

        Raylib.SetGamepadMappings(GamepadMappings.SdlMappings);
    }
}
