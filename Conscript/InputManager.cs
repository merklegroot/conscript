using Raylib_cs;

namespace Conscript;

/// <summary>Hybrid keyboard + gamepad input for menus and the main loop.</summary>
internal static class InputManager
{
    private const float AxisDeadzone = 0.45f;
    private const int NavRepeatDelayFrames = 18;
    private const int NavRepeatIntervalFrames = 8;

    private static int _prevHorizontalAxis;
    private static int _prevVerticalAxis;
    private static int _repeatLeft;
    private static int _repeatRight;
    private static int _repeatUp;
    private static int _repeatDown;

    public static void BeginFrame()
    {
        GamepadConnection.Update();
        JoystickInput.BeginFrame();
        TickNavRepeat(ref _repeatLeft, IsHorizontalNavHeld(-1));
        TickNavRepeat(ref _repeatRight, IsHorizontalNavHeld(1));
        TickNavRepeat(ref _repeatUp, IsVerticalNavHeld(-1));
        TickNavRepeat(ref _repeatDown, IsVerticalNavHeld(1));
    }

    public static void EndFrame()
    {
        JoystickInput.CommitFrame();
        _prevHorizontalAxis = GetHorizontalAxisDirection();
        _prevVerticalAxis = GetVerticalAxisDirection();
    }

    public static bool IsCancelPressed() =>
        Raylib.IsKeyPressed(KeyboardKey.KEY_ESCAPE) ||
        IsAnyGamepadButtonPressed(
            GamepadButton.GAMEPAD_BUTTON_RIGHT_FACE_RIGHT,
            GamepadButton.GAMEPAD_BUTTON_MIDDLE_LEFT) ||
        JoystickInput.IsActionPressed(JoystickAction.Cancel);

    public static bool IsHorizontalNavLeftPressed() =>
        Raylib.IsKeyPressed(KeyboardKey.KEY_LEFT) || Raylib.IsKeyPressed(KeyboardKey.KEY_A) ||
        IsNavTriggered(-1, _prevHorizontalAxis, _repeatLeft,
            GamepadButton.GAMEPAD_BUTTON_LEFT_FACE_LEFT, JoystickAction.Left, GetHorizontalAxisDirection);

    public static bool IsHorizontalNavRightPressed() =>
        Raylib.IsKeyPressed(KeyboardKey.KEY_RIGHT) || Raylib.IsKeyPressed(KeyboardKey.KEY_D) ||
        IsNavTriggered(1, _prevHorizontalAxis, _repeatRight,
            GamepadButton.GAMEPAD_BUTTON_LEFT_FACE_RIGHT, JoystickAction.Right, GetHorizontalAxisDirection);

    public static bool IsVerticalNavUpPressed() =>
        Raylib.IsKeyPressed(KeyboardKey.KEY_UP) || Raylib.IsKeyPressed(KeyboardKey.KEY_W) ||
        IsNavTriggered(-1, _prevVerticalAxis, _repeatUp,
            GamepadButton.GAMEPAD_BUTTON_LEFT_FACE_UP, JoystickAction.Up, GetVerticalAxisDirection);

    public static bool IsVerticalNavDownPressed() =>
        Raylib.IsKeyPressed(KeyboardKey.KEY_DOWN) || Raylib.IsKeyPressed(KeyboardKey.KEY_S) ||
        IsNavTriggered(1, _prevVerticalAxis, _repeatDown,
            GamepadButton.GAMEPAD_BUTTON_LEFT_FACE_DOWN, JoystickAction.Down, GetVerticalAxisDirection);

    public static bool IsConfirmPressed() =>
        Raylib.IsKeyPressed(KeyboardKey.KEY_ENTER) || Raylib.IsKeyPressed(KeyboardKey.KEY_SPACE) ||
        IsAnyGamepadButtonPressed(
            GamepadButton.GAMEPAD_BUTTON_RIGHT_FACE_DOWN,
            GamepadButton.GAMEPAD_BUTTON_RIGHT_FACE_LEFT) ||
        JoystickInput.IsActionPressed(JoystickAction.Confirm);

    public static bool IsGamepadConnected() => GamepadConnection.AnyConnected();

    private static bool IsNavTriggered(
        int direction,
        int previousAxisDirection,
        int repeatCounter,
        GamepadButton dpadButton,
        JoystickAction joystickAction,
        Func<int> getAxisDirection)
    {
        if (IsAnyGamepadButtonPressed(dpadButton))
            return true;

        if (JoystickInput.IsActionPressed(joystickAction))
            return true;

        int axisDirection = getAxisDirection();
        if (axisDirection == direction && previousAxisDirection != direction)
            return true;

        return repeatCounter == NavRepeatDelayFrames ||
            (repeatCounter > NavRepeatDelayFrames &&
             (repeatCounter - NavRepeatDelayFrames) % NavRepeatIntervalFrames == 0);
    }

    private static bool IsHorizontalNavHeld(int direction)
    {
        if (direction < 0 && (Raylib.IsKeyDown(KeyboardKey.KEY_LEFT) || Raylib.IsKeyDown(KeyboardKey.KEY_A)))
            return true;
        if (direction > 0 && (Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT) || Raylib.IsKeyDown(KeyboardKey.KEY_D)))
            return true;

        GamepadButton button = direction < 0
            ? GamepadButton.GAMEPAD_BUTTON_LEFT_FACE_LEFT
            : GamepadButton.GAMEPAD_BUTTON_LEFT_FACE_RIGHT;
        if (IsAnyGamepadButtonDown(button))
            return true;

        JoystickAction action = direction < 0 ? JoystickAction.Left : JoystickAction.Right;
        if (JoystickInput.IsActionHeld(action))
            return true;

        return GetHorizontalAxisDirection() == direction;
    }

    private static bool IsVerticalNavHeld(int direction)
    {
        if (direction < 0 && (Raylib.IsKeyDown(KeyboardKey.KEY_UP) || Raylib.IsKeyDown(KeyboardKey.KEY_W)))
            return true;
        if (direction > 0 && (Raylib.IsKeyDown(KeyboardKey.KEY_DOWN) || Raylib.IsKeyDown(KeyboardKey.KEY_S)))
            return true;

        GamepadButton button = direction < 0
            ? GamepadButton.GAMEPAD_BUTTON_LEFT_FACE_UP
            : GamepadButton.GAMEPAD_BUTTON_LEFT_FACE_DOWN;
        if (IsAnyGamepadButtonDown(button))
            return true;

        JoystickAction action = direction < 0 ? JoystickAction.Up : JoystickAction.Down;
        if (JoystickInput.IsActionHeld(action))
            return true;

        return GetVerticalAxisDirection() == direction;
    }

    private static int GetHorizontalAxisDirection()
    {
        for (int i = 0; i < GamepadConnection.MaxSlots; i++)
        {
            if (!Raylib.IsGamepadAvailable(i))
                continue;

            float x = Raylib.GetGamepadAxisMovement(i, GamepadAxis.GAMEPAD_AXIS_LEFT_X);
            if (x < -AxisDeadzone)
                return -1;
            if (x > AxisDeadzone)
                return 1;
        }

        return JoystickInput.GetHorizontalAxisDirection();
    }

    private static int GetVerticalAxisDirection()
    {
        for (int i = 0; i < GamepadConnection.MaxSlots; i++)
        {
            if (!Raylib.IsGamepadAvailable(i))
                continue;

            float y = Raylib.GetGamepadAxisMovement(i, GamepadAxis.GAMEPAD_AXIS_LEFT_Y);
            if (y < -AxisDeadzone)
                return -1;
            if (y > AxisDeadzone)
                return 1;
        }

        return JoystickInput.GetVerticalAxisDirection();
    }

    private static void TickNavRepeat(ref int counter, bool held)
    {
        if (held)
            counter++;
        else
            counter = 0;
    }

    private static bool IsAnyGamepadButtonPressed(params GamepadButton[] buttons)
    {
        for (int i = 0; i < GamepadConnection.MaxSlots; i++)
        {
            if (!Raylib.IsGamepadAvailable(i))
                continue;

            foreach (GamepadButton button in buttons)
            {
                if (Raylib.IsGamepadButtonPressed(i, button))
                    return true;
            }
        }

        return false;
    }

    private static bool IsAnyGamepadButtonDown(GamepadButton button)
    {
        for (int i = 0; i < GamepadConnection.MaxSlots; i++)
        {
            if (Raylib.IsGamepadAvailable(i) && Raylib.IsGamepadButtonDown(i, button))
                return true;
        }

        return false;
    }
}
