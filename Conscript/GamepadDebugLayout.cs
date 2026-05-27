using Raylib_cs;

namespace Conscript;

internal static class GamepadDebugLayout
{
    public const int MaxGamepadsToShow = GamepadConnection.MaxSlots;

    public const int TitleSize = 28;
    public const int SubtitleSize = 17;
    public const int MetaSize = 16;
    public const int SectionSize = 18;
    public const int BodySize = 15;
    public const int ButtonRowSize = 20;
    public const int ButtonRowStep = 28;

    public static readonly (GamepadButton Button, string Label)[] ButtonsToShow =
    {
        (GamepadButton.GAMEPAD_BUTTON_LEFT_FACE_UP, "D-pad Up"),
        (GamepadButton.GAMEPAD_BUTTON_LEFT_FACE_RIGHT, "D-pad Right"),
        (GamepadButton.GAMEPAD_BUTTON_LEFT_FACE_DOWN, "D-pad Down"),
        (GamepadButton.GAMEPAD_BUTTON_LEFT_FACE_LEFT, "D-pad Left"),
        (GamepadButton.GAMEPAD_BUTTON_RIGHT_FACE_UP, "Face Up (Y/Triangle)"),
        (GamepadButton.GAMEPAD_BUTTON_RIGHT_FACE_RIGHT, "Face Right (B/Circle)"),
        (GamepadButton.GAMEPAD_BUTTON_RIGHT_FACE_DOWN, "Face Down (A/Cross)"),
        (GamepadButton.GAMEPAD_BUTTON_RIGHT_FACE_LEFT, "Face Left (X/Square)"),
        (GamepadButton.GAMEPAD_BUTTON_LEFT_TRIGGER_1, "LB / L1"),
        (GamepadButton.GAMEPAD_BUTTON_LEFT_TRIGGER_2, "LT / L2"),
        (GamepadButton.GAMEPAD_BUTTON_RIGHT_TRIGGER_1, "RB / R1"),
        (GamepadButton.GAMEPAD_BUTTON_RIGHT_TRIGGER_2, "RT / R2"),
        (GamepadButton.GAMEPAD_BUTTON_MIDDLE_LEFT, "Select / Back"),
        (GamepadButton.GAMEPAD_BUTTON_MIDDLE, "Guide / Home"),
        (GamepadButton.GAMEPAD_BUTTON_MIDDLE_RIGHT, "Start"),
        (GamepadButton.GAMEPAD_BUTTON_LEFT_THUMB, "L3 (left stick click)"),
        (GamepadButton.GAMEPAD_BUTTON_RIGHT_THUMB, "R3 (right stick click)"),
    };

    public static readonly (GamepadAxis Axis, string Label)[] AxesToShow =
    {
        (GamepadAxis.GAMEPAD_AXIS_LEFT_X, "Left stick X"),
        (GamepadAxis.GAMEPAD_AXIS_LEFT_Y, "Left stick Y"),
        (GamepadAxis.GAMEPAD_AXIS_RIGHT_X, "Right stick X"),
        (GamepadAxis.GAMEPAD_AXIS_RIGHT_Y, "Right stick Y"),
        (GamepadAxis.GAMEPAD_AXIS_LEFT_TRIGGER, "Left trigger axis"),
        (GamepadAxis.GAMEPAD_AXIS_RIGHT_TRIGGER, "Right trigger axis"),
    };
}
