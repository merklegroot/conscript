using Raylib_cs;

namespace Conscript;

/// <summary>
/// Polls raw GLFW joysticks when Raylib's mapped gamepad API is unavailable.
/// </summary>
internal static class JoystickInput
{
    private const float AxisDeadzone = 0.45f;

    private static readonly byte[][] _currentButtons = new byte[NativeGlfwJoystick.MaxJoysticks][];
    private static readonly byte[][] _previousButtons = new byte[NativeGlfwJoystick.MaxJoysticks][];
    private static readonly int[] _currentHats = new int[NativeGlfwJoystick.MaxJoysticks];
    private static readonly int[] _previousHats = new int[NativeGlfwJoystick.MaxJoysticks];

    private static int _activeJoystick = -1;
    private static JoystickLayoutKind _activeLayout = JoystickLayoutKind.Generic;
    private static int _lastRawButtonPressed = -1;
    private static JoystickAction? _lastActionPressed;

    public static void BeginFrame()
    {
        _lastRawButtonPressed = -1;
        _lastActionPressed = null;
        RefreshActiveJoystick();

        for (int j = 0; j < NativeGlfwJoystick.MaxJoysticks; j++)
        {
            if (!NativeGlfwJoystick.IsPresent(j))
            {
                _currentButtons[j] = Array.Empty<byte>();
                _currentHats[j] = 0;
                continue;
            }

            int buttonCount = NativeGlfwJoystick.GetButtonCount(j);
            var currentButtons = new byte[buttonCount];
            for (int b = 0; b < buttonCount; b++)
                currentButtons[b] = NativeGlfwJoystick.IsButtonDown(j, b) ? (byte)1 : (byte)0;

            byte[] previousButtons = _previousButtons[j];
            for (int b = 0; b < buttonCount; b++)
            {
                bool wasDown = b < previousButtons.Length && previousButtons[b] != 0;
                bool isDown = currentButtons[b] != 0;
                if (isDown && !wasDown)
                    _lastRawButtonPressed = b;
            }

            _currentButtons[j] = currentButtons;
            _currentHats[j] = NativeGlfwJoystick.GetHatValue(j);
        }
    }

    public static void CommitFrame()
    {
        for (int j = 0; j < NativeGlfwJoystick.MaxJoysticks; j++)
        {
            _previousButtons[j] = _currentButtons[j];
            _previousHats[j] = _currentHats[j];
        }
    }

    public static bool AnyJoystickPresent()
    {
        for (int j = 0; j < NativeGlfwJoystick.MaxJoysticks; j++)
        {
            if (NativeGlfwJoystick.IsPresent(j))
                return true;
        }

        return false;
    }

    public static bool IsActionPressed(JoystickAction action) =>
        UseRawJoystickFallback() && IsActionTriggered(action);

    public static bool IsActionHeld(JoystickAction action) =>
        UseRawJoystickFallback() && IsActionActive(action);

    public static int GetHorizontalAxisDirection() =>
        UseRawJoystickFallback() ? GetStickAxisDirection(JoystickAction.Left, JoystickAction.Right) : 0;

    public static int GetVerticalAxisDirection() =>
        UseRawJoystickFallback() ? GetStickAxisDirection(JoystickAction.Up, JoystickAction.Down) : 0;

    public static string GetDebugStatusLine()
    {
        if (_activeJoystick < 0)
            return "GLFW raw joystick: none present";

        string name = NativeGlfwJoystick.GetName(_activeJoystick) ?? "(unnamed)";
        string last = _lastRawButtonPressed >= 0
            ? $"raw button {_lastRawButtonPressed}"
            : _lastActionPressed != null
                ? $"action {_lastActionPressed}"
                : "no press this frame";

        return $"GLFW raw joystick {_activeJoystick}: {name} ({_activeLayout}) — {last}";
    }

    public static int ActiveJoystickIndex => _activeJoystick;

    public static int LastRawButtonPressed => _lastRawButtonPressed;

    private static bool IsActionTriggered(JoystickAction action)
    {
        if (_activeJoystick < 0)
            return false;

        if (IsActionActive(action) && !WasActionActive(action))
        {
            _lastActionPressed = action;
            return true;
        }

        return false;
    }

    private static bool IsActionActive(JoystickAction action)
    {
        if (_activeJoystick < 0)
            return false;

        if (JoystickLayout.TryGetFaceButton(_activeLayout, action, out int faceButton) &&
            IsRawButtonDown(_activeJoystick, faceButton))
            return true;

        if (JoystickLayout.TryGetDpadButton(_activeLayout, action, out int dpadButton) &&
            IsRawButtonDown(_activeJoystick, dpadButton))
            return true;

        if (JoystickLayout.IsHatDirection(_activeLayout, action, _currentHats[_activeJoystick]))
            return true;

        if (!JoystickLayout.TryGetStickAxes(_activeLayout, out int axisX, out int axisY))
            return false;

        float x = NativeGlfwJoystick.GetAxisValue(_activeJoystick, axisX);
        float y = NativeGlfwJoystick.GetAxisValue(_activeJoystick, axisY);

        return action switch
        {
            JoystickAction.Left => x < -AxisDeadzone,
            JoystickAction.Right => x > AxisDeadzone,
            JoystickAction.Up => y < -AxisDeadzone,
            JoystickAction.Down => y > AxisDeadzone,
            _ => false
        };
    }

    private static bool WasActionActive(JoystickAction action)
    {
        if (_activeJoystick < 0)
            return false;

        if (JoystickLayout.TryGetFaceButton(_activeLayout, action, out int faceButton) &&
            WasRawButtonDown(_activeJoystick, faceButton))
            return true;

        if (JoystickLayout.TryGetDpadButton(_activeLayout, action, out int dpadButton) &&
            WasRawButtonDown(_activeJoystick, dpadButton))
            return true;

        if (JoystickLayout.IsHatDirection(_activeLayout, action, _previousHats[_activeJoystick]))
            return true;

        if (!JoystickLayout.TryGetStickAxes(_activeLayout, out int axisX, out int axisY))
            return false;

        float x = NativeGlfwJoystick.GetAxisValue(_activeJoystick, axisX);
        float y = NativeGlfwJoystick.GetAxisValue(_activeJoystick, axisY);

        return action switch
        {
            JoystickAction.Left => x < -AxisDeadzone,
            JoystickAction.Right => x > AxisDeadzone,
            JoystickAction.Up => y < -AxisDeadzone,
            JoystickAction.Down => y > AxisDeadzone,
            _ => false
        };
    }

    private static int GetStickAxisDirection(JoystickAction negative, JoystickAction positive)
    {
        if (IsActionActive(negative))
            return -1;
        if (IsActionActive(positive))
            return 1;
        return 0;
    }

    private static bool IsRawButtonDown(int joystickId, int button)
    {
        if (button < 0 || joystickId < 0 || joystickId >= _currentButtons.Length)
            return false;

        byte[] current = _currentButtons[joystickId];
        return button < current.Length && current[button] != 0;
    }

    private static bool WasRawButtonDown(int joystickId, int button)
    {
        if (button < 0 || joystickId < 0 || joystickId >= _previousButtons.Length)
            return false;

        byte[] previous = _previousButtons[joystickId];
        return button < previous.Length && previous[button] != 0;
    }

    private static bool UseRawJoystickFallback()
    {
        for (int i = 0; i < GamepadConnection.MaxSlots; i++)
        {
            if (Raylib.IsGamepadAvailable(i))
                return false;
        }

        return true;
    }

    private static void RefreshActiveJoystick()
    {
        int bestScore = -1;
        int bestIndex = -1;
        JoystickLayoutKind bestLayout = JoystickLayoutKind.Generic;

        for (int j = 0; j < NativeGlfwJoystick.MaxJoysticks; j++)
        {
            if (!NativeGlfwJoystick.IsPresent(j))
                continue;

            string? name = NativeGlfwJoystick.GetName(j);
            JoystickLayoutKind layout = JoystickLayout.Detect(name);
            int score = NativeGlfwJoystick.GetButtonCount(j);
            if (layout == JoystickLayoutKind.SteamDeck)
                score += 100;
            else if (layout == JoystickLayoutKind.SteamVirtual)
                score += 50;

            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = j;
                bestLayout = layout;
            }
        }

        if (bestIndex >= 0)
        {
            _activeJoystick = bestIndex;
            _activeLayout = bestLayout;
            return;
        }

        _activeJoystick = -1;
        _activeLayout = JoystickLayoutKind.Generic;
    }
}
