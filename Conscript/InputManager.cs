using Raylib_cs;

namespace Conscript;

/// <summary>Hybrid keyboard + gamepad input. Call <see cref="Initialize"/> once after <see cref="Raylib.InitWindow"/>.</summary>
internal static class InputManager
{
    public const int MaxGamepads = 16;

    private static bool _initialized;
    private static int _activeGamepad = -1;

    public static int ActiveGamepad => _activeGamepad;

    public static void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;
        LoadSteamDeckMappings();
        RefreshGamepad();
    }

    /// <summary>Re-detect gamepad slot; re-apply mappings when a pad newly appears (GLFW quirk).</summary>
    public static void RefreshGamepad()
    {
        int previous = _activeGamepad;
        _activeGamepad = -1;

        for (int i = 0; i < MaxGamepads; i++)
        {
            if (!Raylib.IsGamepadAvailable(i))
                continue;

            _activeGamepad = i;
            break;
        }

        if (_activeGamepad >= 0 && _activeGamepad != previous)
            LoadSteamDeckMappings();
    }

    public static bool IsGamepadConnected => _activeGamepad >= 0;

    public static bool IsCancelPressed() =>
        Raylib.IsKeyPressed(KeyboardKey.KEY_ESCAPE) ||
        GamepadPressed(GamepadButton.GAMEPAD_BUTTON_RIGHT_FACE_RIGHT);

    public static bool IsHorizontalNavLeftPressed() =>
        Raylib.IsKeyPressed(KeyboardKey.KEY_LEFT) || Raylib.IsKeyPressed(KeyboardKey.KEY_A) ||
        GamepadPressed(GamepadButton.GAMEPAD_BUTTON_LEFT_FACE_LEFT);

    public static bool IsHorizontalNavRightPressed() =>
        Raylib.IsKeyPressed(KeyboardKey.KEY_RIGHT) || Raylib.IsKeyPressed(KeyboardKey.KEY_D) ||
        GamepadPressed(GamepadButton.GAMEPAD_BUTTON_RIGHT_FACE_RIGHT);

    public static bool IsVerticalNavUpPressed() =>
        Raylib.IsKeyPressed(KeyboardKey.KEY_UP) || Raylib.IsKeyPressed(KeyboardKey.KEY_W) ||
        GamepadPressed(GamepadButton.GAMEPAD_BUTTON_LEFT_FACE_UP);

    public static bool IsVerticalNavDownPressed() =>
        Raylib.IsKeyPressed(KeyboardKey.KEY_DOWN) || Raylib.IsKeyPressed(KeyboardKey.KEY_S) ||
        GamepadPressed(GamepadButton.GAMEPAD_BUTTON_LEFT_FACE_DOWN);

    public static bool IsConfirmPressed() =>
        Raylib.IsKeyPressed(KeyboardKey.KEY_ENTER) || Raylib.IsKeyPressed(KeyboardKey.KEY_SPACE) ||
        GamepadPressed(GamepadButton.GAMEPAD_BUTTON_RIGHT_FACE_DOWN);

    private static bool GamepadPressed(GamepadButton button) =>
        _activeGamepad >= 0 && Raylib.IsGamepadButtonPressed(_activeGamepad, button);

    private static void LoadSteamDeckMappings()
    {
        const string mappings = """
            03000000de280000ff11000001000000,Steam Virtual Gamepad,a:b0,b:b1,x:b2,y:b3,back:b4,start:b6,leftstick:b7,rightstick:b8,leftshoulder:b9,rightshoulder:b10,dpdown:h0.4,dpleft:h0.8,dpright:h0.2,dpup:h0.1,leftx:a0,lefty:a1,rightx:a2,righty:a3,lefttrigger:a4,righttrigger:a5,platform:Linux
            028e04504c0500000000000000000000,Steam Deck,a:b0,b:b1,x:b2,y:b3,back:b4,guide:b5,start:b6,leftstick:b7,rightstick:b8,leftshoulder:b9,rightshoulder:b10,dpdown:b15,dpleft:b16,dpright:b17,dpup:b18,leftx:a0,lefty:a1,rightx:a2,righty:a3,lefttrigger:a4,righttrigger:a5,platform:Linux
            03000000750e00000603000001000000,Steam Controller,a:b0,b:b1,y:b2,x:b3,guide:b4,back:b5,start:b6,leftstick:b7,rightstick:b8,leftshoulder:b9,rightshoulder:b10,dpup:b11,dpdown:b12,dpleft:b13,dpright:b14,leftx:a0,lefty:a1,rightx:a2,righty:a3,lefttrigger:a4,righttrigger:a5,platform:Linux
            """;

        Raylib.SetGamepadMappings(mappings);
    }
}
