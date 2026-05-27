namespace Conscript;

/// <summary>
/// SDL2 controller mappings to help Raylib recognize Steam Input devices on Steam Deck.
/// </summary>
internal static class GamepadMappings
{
    // These mapping lines are sourced from SDL's built-in controller mapping database (SDL2).
    // Raylib uses SDL's GameController layer under the hood; without these, some Steam Input
    // devices can show up as "joystick" instead of "gamepad" and won't be visible to
    // Raylib.IsGamepadAvailable / IsGamepadButtonPressed.
    public const string SdlMappings =
        // Steam Virtual Gamepad (common when running under Steam Input)
        "03000000de280000ff11000000007701,Steam Virtual Gamepad," +
        "a:b0,b:b1,back:b6,dpdown:b12,dpleft:b13,dpright:b11,dpup:b10,leftshoulder:b4,leftstick:b8," +
        "lefttrigger:a4,leftx:a1,lefty:a0~,rightshoulder:b5,rightstick:b9,righttrigger:a5,rightx:a3,righty:a2~," +
        "start:b7,x:b2,y:b3,\n" +
        // Alternate Steam Virtual Gamepad mapping variant
        "03000000de280000ff11000001000000,Steam Virtual Gamepad," +
        "a:b0,b:b1,back:b6,dpdown:h0.4,dpleft:h0.8,dpright:h0.2,dpup:h0.1,guide:b8,leftshoulder:b4,leftstick:b9," +
        "lefttrigger:a2,leftx:a0,lefty:a1,rightshoulder:b5,rightstick:b10,righttrigger:a5,rightx:a3,righty:a4," +
        "start:b7,x:b2,y:b3,\n" +
        // Steam Controller (multiple hardware revisions / modes)
        "03000000de2800000112000001000000,Steam Controller," +
        "a:b0,b:b1,back:b6,dpdown:b14,dpleft:b15,dpright:b13,dpup:b12,guide:b8,leftshoulder:b4,leftstick:b9," +
        "lefttrigger:a2,leftx:a0,lefty:a1,paddle1:b11,paddle2:b10,rightshoulder:b5,righttrigger:a3,start:b7,x:b2,y:b3,\n" +
        "03000000de2800000211000001000000,Steam Controller," +
        "a:b0,b:b1,back:b6,dpdown:b14,dpleft:b15,dpright:b13,dpup:b12,guide:b8,leftshoulder:b4,leftstick:b9," +
        "lefttrigger:a2,leftx:a0,lefty:a1,paddle1:b11,paddle2:b10,rightshoulder:b5,righttrigger:a3,start:b7,x:b2,y:b3,\n" +
        "03000000de280000fc11000001000000,Steam Controller," +
        "a:b0,b:b1,back:b6,dpdown:b14,dpleft:b15,dpright:b13,dpup:b12,guide:b8,leftshoulder:b4,leftstick:b9," +
        "lefttrigger:a2,leftx:a0,lefty:a1,rightshoulder:b5,rightstick:b10,righttrigger:a5,rightx:a3,righty:a4,start:b7,x:b2,y:b3,\n" +
        // Steam Deck built-in controls
        "03000000de2800000512000011010000,Steam Deck," +
        "a:b3,b:b4,back:b11,dpdown:b17,dpleft:b18,dpright:b19,dpup:b16,guide:b13,leftshoulder:b7,leftstick:b14," +
        "lefttrigger:a9,leftx:a0,lefty:a1,misc1:b2,paddle1:b21,paddle2:b20,paddle3:b23,paddle4:b22,rightshoulder:b8," +
        "rightstick:b15,righttrigger:a8,rightx:a2,righty:a3,start:b12,x:b5,y:b6,";
}

