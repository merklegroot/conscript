using Raylib_cs;

namespace Conscript;

/// <summary>Keyboard + gamepad bindings shared across menus and the main loop.</summary>
internal static class GameInput
{
    public static void Update() => GamepadConnection.Update();

    public static bool IsCancelPressed() =>
        Raylib.IsKeyPressed(KeyboardKey.KEY_ESCAPE) ||
        IsAnyGamepadButtonPressed(GamepadButton.GAMEPAD_BUTTON_RIGHT_FACE_RIGHT);

    public static bool IsHorizontalNavLeftPressed() =>
        Raylib.IsKeyPressed(KeyboardKey.KEY_LEFT) || Raylib.IsKeyPressed(KeyboardKey.KEY_A) ||
        IsAnyGamepadButtonPressed(GamepadButton.GAMEPAD_BUTTON_LEFT_FACE_LEFT);

    public static bool IsHorizontalNavRightPressed() =>
        Raylib.IsKeyPressed(KeyboardKey.KEY_RIGHT) || Raylib.IsKeyPressed(KeyboardKey.KEY_D) ||
        IsAnyGamepadButtonPressed(GamepadButton.GAMEPAD_BUTTON_LEFT_FACE_RIGHT);

    public static bool IsVerticalNavUpPressed() =>
        Raylib.IsKeyPressed(KeyboardKey.KEY_UP) || Raylib.IsKeyPressed(KeyboardKey.KEY_W) ||
        IsAnyGamepadButtonPressed(GamepadButton.GAMEPAD_BUTTON_LEFT_FACE_UP);

    public static bool IsVerticalNavDownPressed() =>
        Raylib.IsKeyPressed(KeyboardKey.KEY_DOWN) || Raylib.IsKeyPressed(KeyboardKey.KEY_S) ||
        IsAnyGamepadButtonPressed(GamepadButton.GAMEPAD_BUTTON_LEFT_FACE_DOWN);

    public static bool IsConfirmPressed() =>
        Raylib.IsKeyPressed(KeyboardKey.KEY_ENTER) || Raylib.IsKeyPressed(KeyboardKey.KEY_SPACE) ||
        IsAnyGamepadButtonPressed(GamepadButton.GAMEPAD_BUTTON_RIGHT_FACE_DOWN);

    public static bool IsAnyGamepadButtonPressed(GamepadButton button)
    {
        for (int i = 0; i < GamepadConnection.MaxSlots; i++)
        {
            if (Raylib.IsGamepadButtonPressed(i, button))
                return true;
        }

        return false;
    }
}
