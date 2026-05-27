using System.Numerics;
using Conscript.Constants;
using Raylib_cs;

namespace Conscript;

internal static class ControllerDebugScreenDrawing
{
    public readonly struct ScreenLayout
    {
        public Rectangle PrevRect { get; init; }
        public Rectangle NextRect { get; init; }
        public Rectangle CloseRect { get; init; }
        public Rectangle[] TabRects { get; init; }
    }

    public static ScreenLayout DrawScreen(
        int screenWidth,
        int screenHeight,
        Font font,
        int padIndex,
        bool prevHovered,
        bool nextHovered,
        bool closeHovered,
        bool[] tabHovered)
    {
        Raylib.DrawRectangle(0, 0, screenWidth, screenHeight, new Color(0, 0, 0, 200));

        int panelX = 36;
        int panelY = 28;
        int panelW = screenWidth - 72;
        int panelH = screenHeight - 56;

        Raylib.DrawRectangle(panelX, panelY, panelW, panelH, Palette.CardBg);
        Raylib.DrawRectangleLines(panelX, panelY, panelW, panelH, Palette.CardBorder);

        Raylib.DrawTextEx(font, "CONTROLLER DEBUG",
            new Vector2(panelX + 22, panelY + 16),
            GamepadDebugLayout.TitleSize, 0.75f, Palette.TextPrimary);

        Raylib.DrawTextEx(font, "Live input from Raylib / SDL — one gamepad at a time.",
            new Vector2(panelX + 22, panelY + 50),
            GamepadDebugLayout.SubtitleSize, 0.55f, Palette.TextSecondary);

        int lastPressed = Raylib.GetGamepadButtonPressed();
        string lastLine = lastPressed > 0
            ? $"Raylib mapped button: {(GamepadButton)lastPressed}"
            : "Raylib mapped button: — (0 = none / unknown)";
        Raylib.DrawTextEx(font, lastLine,
            new Vector2(panelX + 22, panelY + 76),
            GamepadDebugLayout.MetaSize, 0.5f, Palette.TextMuted);

        Raylib.DrawTextEx(font, JoystickInput.GetDebugStatusLine(),
            new Vector2(panelX + 22, panelY + 96),
            GamepadDebugLayout.MetaSize, 0.45f, Palette.TextSecondary);

        string steamLine = GamepadConnection.HasSteamControllerConfig
            ? "Steam SDL_GAMECONTROLLERCONFIG: present"
            : "Steam SDL_GAMECONTROLLERCONFIG: not set — launch through Steam for Deck controls";
        Raylib.DrawTextEx(font, steamLine,
            new Vector2(panelX + 22, panelY + 116),
            GamepadDebugLayout.MetaSize, 0.45f, Palette.TextMuted);

        string countLine = $"Connected (mapped + raw): {GamepadConnection.ConnectedCount()}";
        Raylib.DrawTextEx(font, countLine,
            new Vector2(panelX + 22, panelY + 136),
            GamepadDebugLayout.MetaSize, 0.45f, Palette.TextMuted);

        int selectorY = panelY + 164;
        const int navBtnW = 100;
        const int navBtnH = 34;
        int tabW = 52;
        int tabH = 34;
        int tabGap = 8;
        int availableTabW = panelW - 44;
        tabW = Math.Clamp((availableTabW - (GamepadDebugLayout.MaxGamepadsToShow - 1) * tabGap) / GamepadDebugLayout.MaxGamepadsToShow, 28, tabW);
        int tabsTotalW = GamepadDebugLayout.MaxGamepadsToShow * tabW + (GamepadDebugLayout.MaxGamepadsToShow - 1) * tabGap;
        int tabsX = panelX + (panelW - tabsTotalW) / 2;

        var prevRect = new Rectangle(panelX + 22, selectorY, navBtnW, navBtnH);
        var nextRect = new Rectangle(panelX + panelW - 22 - navBtnW, selectorY, navBtnW, navBtnH);
        GameDialogUi.DrawDialogButton(prevRect, "PREV", prevHovered, font);
        GameDialogUi.DrawDialogButton(nextRect, "NEXT", nextHovered, font);

        var tabRects = new Rectangle[GamepadDebugLayout.MaxGamepadsToShow];
        for (int i = 0; i < GamepadDebugLayout.MaxGamepadsToShow; i++)
        {
            int tabX = tabsX + i * (tabW + tabGap);
            tabRects[i] = new Rectangle(tabX, selectorY, tabW, tabH);
            bool selected = i == padIndex;
            bool connected = Raylib.IsGamepadAvailable(i);
            bool hovered = tabHovered[i];

            Color tabBg = selected
                ? Palette.ButtonSelectedBg
                : hovered ? new Color(48, 52, 60, 255) : new Color(24, 26, 32, 255);
            Color tabBorder = selected
                ? Palette.ButtonSelectedBorder
                : connected ? new Color(90, 120, 95, 255) : Palette.SubtleBorder;

            Raylib.DrawRectangleRec(tabRects[i], tabBg);
            Raylib.DrawRectangleLinesEx(tabRects[i], 1.5f, tabBorder);

            string tabLabel = $"{i}";
            int labelSize = tabW >= 44 ? 18 : 16;
            int lw = (int)Raylib.MeasureTextEx(font, tabLabel, labelSize, 0.6f).X;
            Raylib.DrawTextEx(font, tabLabel,
                new Vector2(tabX + (tabW - lw) / 2f, selectorY + 7),
                labelSize, 0.6f, selected ? Palette.TextPrimary : Palette.TextSecondary);

            if (connected)
            {
                Raylib.DrawCircle(tabX + tabW - 10, selectorY + 10, 4f,
                    selected ? Palette.Positive : new Color(70, 100, 78, 255));
            }
        }

        int contentTop = selectorY + navBtnH + 18;
        int contentH = panelH - (contentTop - panelY) - 72;
        DrawDetail(font, padIndex, panelX + 22, contentTop, panelW - 44, contentH);

        int btnW = 140;
        int btnH = 36;
        int btnX = panelX + (panelW - btnW) / 2;
        int btnY = panelY + panelH - btnH - 16;
        var closeRect = new Rectangle(btnX, btnY, btnW, btnH);
        GameDialogUi.DrawDialogButton(closeRect, "CLOSE", closeHovered, font);

        Raylib.DrawTextEx(font, "Esc · Close  ·  ← → or , . to switch gamepad",
            new Vector2(panelX + 22, panelY + panelH - 28),
            GamepadDebugLayout.MetaSize, 0.45f, Palette.TextDim);

        return new ScreenLayout
        {
            PrevRect = prevRect,
            NextRect = nextRect,
            CloseRect = closeRect,
            TabRects = tabRects
        };
    }

    public static void DrawDetail(Font font, int gamepad, int x, int y, int width, int height)
    {
        Raylib.DrawRectangle(x, y, width, height, new Color(12, 14, 18, 255));
        Raylib.DrawRectangleLines(x, y, width, height, Palette.SubtleBorder);

        int pad = 18;
        int cy = y + pad;
        int innerW = width - pad * 2;

        bool connected = Raylib.IsGamepadAvailable(gamepad);
        string status = connected ? "Connected" : "Not connected";
        Color statusColor = connected ? Palette.Positive : Palette.TextDim;

        string header = $"Gamepad {gamepad}";
        Raylib.DrawTextEx(font, header, new Vector2(x + pad, cy), 22, 0.7f, Palette.TextPrimary);
        int statusW = (int)Raylib.MeasureTextEx(font, status, GamepadDebugLayout.BodySize, 0.55f).X;
        Raylib.DrawTextEx(font, status,
            new Vector2(x + width - pad - statusW, cy + 2),
            GamepadDebugLayout.BodySize, 0.55f, statusColor);
        cy += 30;

        if (!connected)
        {
            string maybeName = Raylib.GetGamepadName_(gamepad);
            if (string.IsNullOrWhiteSpace(maybeName))
                maybeName = "(no SDL gamepad name)";
            GamepadDebugDrawing.DrawTruncatedLine(font, maybeName, x + pad, ref cy, innerW, GamepadDebugLayout.BodySize, Palette.TextSecondary);
            cy += 6;

            Raylib.DrawTextEx(font, "No device on this slot. Mappings re-apply every ~0.5s while disconnected.",
                new Vector2(x + pad, cy), GamepadDebugLayout.BodySize, 0.55f, Palette.TextSecondary);
            cy += 22;

            DrawRawJoystickButtons(font, x + pad, ref cy, innerW);
            return;
        }

        string name = Raylib.GetGamepadName_(gamepad);
        if (string.IsNullOrWhiteSpace(name))
            name = "(unnamed device)";
        GamepadDebugDrawing.DrawTruncatedLine(font, name, x + pad, ref cy, innerW, GamepadDebugLayout.BodySize, Palette.TextSecondary);
        cy += 6;

        int axisCount = Raylib.GetGamepadAxisCount(gamepad);
        Raylib.DrawTextEx(font, $"Axis count: {axisCount}",
            new Vector2(x + pad, cy), GamepadDebugLayout.MetaSize, 0.5f, Palette.TextMuted);
        cy += 28;

        int leftColW = innerW / 2 - 12;
        int rightColX = x + pad + leftColW + 24;
        int rightColW = innerW - leftColW - 24;
        int leftY = cy;

        float lx = Raylib.GetGamepadAxisMovement(gamepad, GamepadAxis.GAMEPAD_AXIS_LEFT_X);
        float ly = Raylib.GetGamepadAxisMovement(gamepad, GamepadAxis.GAMEPAD_AXIS_LEFT_Y);
        float rx = Raylib.GetGamepadAxisMovement(gamepad, GamepadAxis.GAMEPAD_AXIS_RIGHT_X);
        float ry = Raylib.GetGamepadAxisMovement(gamepad, GamepadAxis.GAMEPAD_AXIS_RIGHT_Y);

        int stickSize = 56;
        int stickRowY = leftY;
        Raylib.DrawTextEx(font, "Sticks", new Vector2(x + pad, stickRowY),
            GamepadDebugLayout.SectionSize, 0.55f, Palette.TextMuted);
        stickRowY += 24;

        int stickCenterY = stickRowY + stickSize + 8;
        GamepadDebugDrawing.DrawStick(x + pad + stickSize, stickCenterY, stickSize, lx, ly, Palette.Hydration);
        GamepadDebugDrawing.DrawStick(x + pad + stickSize * 2 + 36, stickCenterY, stickSize, rx, ry, Palette.Energy);
        Raylib.DrawTextEx(font, "Left", new Vector2(x + pad + stickSize - 18, stickRowY + 4),
            GamepadDebugLayout.MetaSize, 0.45f, Palette.TextDim);
        Raylib.DrawTextEx(font, "Right", new Vector2(x + pad + stickSize * 2 + 18, stickRowY + 4),
            GamepadDebugLayout.MetaSize, 0.45f, Palette.TextDim);

        int axisY = stickCenterY + stickSize + 22;
        Raylib.DrawTextEx(font, "Axes", new Vector2(x + pad, axisY),
            GamepadDebugLayout.SectionSize, 0.55f, Palette.TextMuted);
        axisY += 24;

        foreach (var (axis, label) in GamepadDebugLayout.AxesToShow)
        {
            float value = Raylib.GetGamepadAxisMovement(gamepad, axis);
            Raylib.DrawTextEx(font, label, new Vector2(x + pad, axisY),
                GamepadDebugLayout.BodySize, 0.45f, Palette.TextDim);
            GamepadDebugDrawing.DrawAxisBar(x + pad, axisY + 20, leftColW, 10, value);
            Raylib.DrawTextEx(font, $"{value:F3}",
                new Vector2(x + pad + leftColW - 52, axisY + 2),
                GamepadDebugLayout.BodySize, 0.45f, Palette.TextSecondary);
            axisY += 38;
        }

        Raylib.DrawTextEx(font, "Buttons", new Vector2(rightColX, leftY),
            GamepadDebugLayout.SectionSize, 0.55f, Palette.TextMuted);
        int btnY = leftY + 24;
        int btnColW = (rightColW - 12) / 2;
        int btnCount = GamepadDebugLayout.ButtonsToShow.Length;
        int rowsPerCol = (btnCount + 1) / 2;

        for (int i = 0; i < btnCount; i++)
        {
            var (button, label) = GamepadDebugLayout.ButtonsToShow[i];
            int col = i / rowsPerCol;
            int row = i % rowsPerCol;
            int bx = rightColX + col * (btnColW + 12);
            int by = btnY + row * GamepadDebugLayout.ButtonRowStep;

            bool down = Raylib.IsGamepadButtonDown(gamepad, button);
            bool pressed = Raylib.IsGamepadButtonPressed(gamepad, button);
            bool released = Raylib.IsGamepadButtonReleased(gamepad, button);

            Color dot = down ? Palette.Positive : Palette.SubtleBorder;
            if (pressed)
                dot = Palette.ActionFlash;
            else if (released)
                dot = Palette.Satiation;

            Raylib.DrawCircle(bx + 7, by + 11, 6f, dot);

            string suffix = pressed ? "  pressed" : released ? "  released" : down ? "  down" : "";
            Color textColor = down || pressed ? Palette.TextPrimary : Palette.TextDim;
            Raylib.DrawTextEx(font, label + suffix, new Vector2(bx + 18, by + 2),
                GamepadDebugLayout.ButtonRowSize, 0.45f, textColor);
        }
    }

    private static void DrawRawJoystickButtons(Font font, int x, ref int y, int innerW)
    {
        int joy = JoystickInput.ActiveJoystickIndex;
        if (joy < 0)
        {
            Raylib.DrawTextEx(font, "No GLFW raw joystick — try replugging or launch via Steam.",
                new Vector2(x, y), GamepadDebugLayout.BodySize, 0.55f, Palette.TextMuted);
            return;
        }

        string name = NativeGlfwJoystick.GetName(joy) ?? "(unnamed)";
        GamepadDebugDrawing.DrawTruncatedLine(font, $"Raw device: {name}", x, ref y, innerW, GamepadDebugLayout.BodySize, Palette.TextSecondary);
        y += 8;

        int count = NativeGlfwJoystick.GetButtonCount(joy);
        Raylib.DrawTextEx(font, $"Raw buttons (0–{Math.Max(0, count - 1)}):",
            new Vector2(x, y), GamepadDebugLayout.MetaSize, 0.5f, Palette.TextMuted);
        y += 22;

        for (int b = 0; b < count && b < 24; b++)
        {
            bool down = NativeGlfwJoystick.IsButtonDown(joy, b);
            Color dot = down ? Palette.Positive : Palette.SubtleBorder;
            Raylib.DrawCircle(x + 8, y + 10, 5f, dot);
            Raylib.DrawTextEx(font, $"btn {b}", new Vector2(x + 20, y + 2),
                GamepadDebugLayout.BodySize, 0.45f, down ? Palette.TextPrimary : Palette.TextDim);
            y += 22;
        }
    }
}
