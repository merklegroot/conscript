using System.Numerics;
using Raylib_cs;

namespace Conscript;

internal static class GameToolbarIcons
{
    public static void DrawRestart(float cx, float cy, float size, Color color)
    {
        float r = size * 0.38f;
        float thick = Math.Max(1.6f, size * 0.13f);

        const float arcStart = 38f;
        const float arcEnd = 302f;
        const int segments = 28;
        float span = arcEnd - arcStart;

        for (int i = 0; i < segments; i++)
        {
            float t0 = (arcStart + span * i / segments) * MathF.PI / 180f;
            float t1 = (arcStart + span * (i + 1) / segments) * MathF.PI / 180f;
            Raylib.DrawLineEx(
                new Vector2(cx + MathF.Cos(t0) * r, cy + MathF.Sin(t0) * r),
                new Vector2(cx + MathF.Cos(t1) * r, cy + MathF.Sin(t1) * r),
                thick, color);
        }

        float headAngle = arcStart * MathF.PI / 180f;
        float hx = cx + MathF.Cos(headAngle) * r;
        float hy = cy + MathF.Sin(headAngle) * r;
        float tangent = headAngle + MathF.PI / 2f;
        float ah = size * 0.24f;

        float tx = hx + MathF.Cos(tangent) * ah;
        float ty = hy + MathF.Sin(tangent) * ah;
        Raylib.DrawLineEx(new Vector2(hx, hy), new Vector2(tx, ty), thick, color);

        float wing = tangent - 2.35f;
        Raylib.DrawLineEx(
            new Vector2(tx, ty),
            new Vector2(tx + MathF.Cos(wing) * ah * 0.55f, ty + MathF.Sin(wing) * ah * 0.55f),
            thick, color);

        wing = tangent + 2.35f;
        Raylib.DrawLineEx(
            new Vector2(tx, ty),
            new Vector2(tx + MathF.Cos(wing) * ah * 0.55f, ty + MathF.Sin(wing) * ah * 0.55f),
            thick, color);
    }

    public static void DrawReticle(float cx, float cy, float size, Color color)
    {
        float thick = Math.Max(1.4f, size * 0.11f);
        float arm = size * 0.42f;
        float gap = size * 0.12f;

        Raylib.DrawLineEx(new Vector2(cx - arm, cy), new Vector2(cx - gap, cy), thick, color);
        Raylib.DrawLineEx(new Vector2(cx + gap, cy), new Vector2(cx + arm, cy), thick, color);
        Raylib.DrawLineEx(new Vector2(cx, cy - arm), new Vector2(cx, cy - gap), thick, color);
        Raylib.DrawLineEx(new Vector2(cx, cy + gap), new Vector2(cx, cy + arm), thick, color);

        float r = size * 0.34f;
        Raylib.DrawCircleLines((int)cx, (int)cy, r, color);
    }

    public static void DrawController(float cx, float cy, float size, Color color)
    {
        float bodyW = size * 0.82f;
        float bodyH = size * 0.46f;
        float thick = Math.Max(1.4f, size * 0.11f);
        var body = new Rectangle(cx - bodyW / 2f, cy - bodyH / 2f, bodyW, bodyH);

        Raylib.DrawRectangleRoundedLines(body, 0.4f, 8, thick, color);

        float padCx = cx - bodyW * 0.22f;
        float arm = size * 0.11f;
        Raylib.DrawRectangle(
            (int)(padCx - arm / 2f), (int)(cy - arm * 1.1f),
            (int)arm, (int)(arm * 2.2f), color);
        Raylib.DrawRectangle(
            (int)(padCx - arm * 1.1f), (int)(cy - arm / 2f),
            (int)(arm * 2.2f), (int)arm, color);

        float btnCx = cx + bodyW * 0.2f;
        float btnR = Math.Max(1.5f, size * 0.07f);
        Raylib.DrawCircleV(new Vector2(btnCx - btnR * 1.6f, cy - btnR * 1.1f), btnR, color);
        Raylib.DrawCircleV(new Vector2(btnCx + btnR * 1.4f, cy + btnR * 1.2f), btnR, color);

        float bumpR = Math.Max(1.2f, size * 0.06f);
        Raylib.DrawCircleV(new Vector2(cx - bodyW * 0.32f, cy + bodyH * 0.42f), bumpR, color);
        Raylib.DrawCircleV(new Vector2(cx + bodyW * 0.32f, cy + bodyH * 0.42f), bumpR, color);
    }
}
