namespace Conscript;

/// <summary>
/// SDL environment for Raylib gamepads. Must run before <see cref="Raylib_cs.Raylib.InitWindow"/>.
/// </summary>
internal static class GamepadEnvironment
{
    /// <summary>
    /// Steam Virtual Gamepad (0x28de/0x11ff). When launched via Steam, this wrapper can
    /// appear before GLFW starts; Raylib then never sees real button events.
    /// </summary>
    private const string IgnoreSteamVirtualPad = "0x28de/0x11ff";

    public static void ConfigureForRaylib()
    {
        Environment.SetEnvironmentVariable("SDL_GAMECONTROLLER_IGNORE_DEVICES", IgnoreSteamVirtualPad);
    }
}
