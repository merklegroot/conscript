using System.Runtime.InteropServices;

namespace Conscript;

/// <summary>
/// Raw GLFW joystick queries through symbols exported by the native raylib library.
/// Used when Raylib's gamepad layer does not see a mapped controller (common on Steam Deck).
/// </summary>
internal static class NativeGlfwJoystick
{
    private const string RaylibNative = "raylib";

    private const int Pressed = 1;

    public const int MaxJoysticks = 16;

    public static bool IsPresent(int joystickId) => glfwJoystickPresent(joystickId) == Pressed;

    public static string? GetName(int joystickId)
    {
        if (!IsPresent(joystickId))
            return null;

        IntPtr namePtr = glfwGetJoystickName(joystickId);
        return namePtr == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(namePtr);
    }

    public static int GetButtonCount(int joystickId)
    {
        if (!IsPresent(joystickId))
            return 0;

        glfwGetJoystickButtons(joystickId, out int count);
        return count;
    }

    public static bool IsButtonDown(int joystickId, int button)
    {
        if (button < 0 || !IsPresent(joystickId))
            return false;

        IntPtr buttonsPtr = glfwGetJoystickButtons(joystickId, out int count);
        if (buttonsPtr == IntPtr.Zero || button >= count)
            return false;

        return Marshal.ReadByte(buttonsPtr, button) == Pressed;
    }

    public static int GetHatCount(int joystickId)
    {
        if (!IsPresent(joystickId))
            return 0;

        glfwGetJoystickHats(joystickId, out int count);
        return count;
    }

    public static int GetHatValue(int joystickId, int hat = 0)
    {
        if (hat < 0 || !IsPresent(joystickId))
            return 0;

        IntPtr hatsPtr = glfwGetJoystickHats(joystickId, out int count);
        if (hatsPtr == IntPtr.Zero || hat >= count)
            return 0;

        return Marshal.ReadByte(hatsPtr, hat);
    }

    public static int GetAxisCount(int joystickId)
    {
        if (!IsPresent(joystickId))
            return 0;

        glfwGetJoystickAxes(joystickId, out int count);
        return count;
    }

    public static float GetAxisValue(int joystickId, int axis)
    {
        if (axis < 0 || !IsPresent(joystickId))
            return 0f;

        IntPtr axesPtr = glfwGetJoystickAxes(joystickId, out int count);
        if (axesPtr == IntPtr.Zero || axis >= count)
            return 0f;

        return Marshal.PtrToStructure<float>(IntPtr.Add(axesPtr, axis * sizeof(float)));
    }

    [DllImport(RaylibNative, CallingConvention = CallingConvention.Cdecl)]
    private static extern int glfwJoystickPresent(int jid);

    [DllImport(RaylibNative, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr glfwGetJoystickName(int jid);

    [DllImport(RaylibNative, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr glfwGetJoystickButtons(int jid, out int count);

    [DllImport(RaylibNative, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr glfwGetJoystickHats(int jid, out int count);

    [DllImport(RaylibNative, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr glfwGetJoystickAxes(int jid, out int count);
}
