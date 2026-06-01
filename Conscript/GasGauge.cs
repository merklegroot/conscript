using System.Numerics;
using Raylib_cs;

namespace Conscript;

/// <summary>
/// Top semicircle fuel gauge. Pivot is the center of the full circle (on the flat chord).
/// Normalized level 0 = E (left), 1 = F (right) along the usable dial arc.
/// </summary>
internal static class GasGauge
{
    private static readonly Color PlateColor = new(12, 14, 20, 255);
    private static readonly Color FaceColor = new(18, 20, 28, 255);
    private static readonly Color BezelColor = new(58, 62, 72, 255);
    private static readonly Color BezelHighlight = new(88, 92, 102, 255);
    private static readonly Color BaseColor = new(42, 46, 56, 255);
    private static readonly Color TickColor = new(150, 154, 164, 255);
    private static readonly Color TickMajorColor = new(210, 206, 196, 255);
    private static readonly Color NeedleColor = new(228, 92, 58, 255);
    private static readonly Color HubColor = new(36, 38, 46, 255);
    private static readonly Color HubRingColor = new(72, 76, 86, 255);
    private static readonly Color LabelColor = new(180, 176, 168, 255);
    private static readonly Color RedZoneColor = new(168, 48, 44, 90);
    private static readonly Color OutlineColor = new(100, 104, 114, 255);

    private const float ArcStartDeg = 180f;
    private const float ArcEndDeg = 360f;
    private const int ArcSegments = 64;
    private const float HubInnerRadius = 6f;
    private const float ReferenceRadius = 220f;

    /// <summary>Usable dial arc: inset from chord ends so E/F sit on the dome.</summary>
    private const float DialLevelEmpty = 0.12f;
    private const float DialLevelFull = 0.88f;

    public static void Draw(Vector2 pivot, float radius, float level, Font labelFont = default)
    {
        level = Math.Clamp(level, 0f, 1f);

        DrawPlate(pivot, radius);
        DrawFace(pivot, radius);
        DrawBezel(pivot, radius);
        DrawBase(pivot, radius);
        DrawRedZone(pivot, radius);
        DrawTicks(pivot, radius);
        DrawLabels(pivot, radius, labelFont);
        DrawNeedle(pivot, radius * 0.82f, ToDialLevel(level));
        DrawHub(pivot);
    }

    private static float ToDialLevel(float level) =>
        DialLevelEmpty + (DialLevelFull - DialLevelEmpty) * level;

    private static void DrawPlate(Vector2 pivot, float radius)
    {
        var outer = radius + 12f;
        DrawArcFilled(pivot, outer, PlateColor);
        DrawArcOutline(pivot, outer, 2f, OutlineColor);
    }

    private static void DrawFace(Vector2 pivot, float radius)
    {
        const float inner = 36f;
        DrawArcRing(pivot, radius - inner, radius - 8f, FaceColor);
    }

    private static void DrawBezel(Vector2 pivot, float radius)
    {
        DrawArcRing(pivot, radius - 8f, radius, BezelColor);
        DrawArcRing(pivot, radius, radius + 8f, BezelColor);
        DrawArcOutline(pivot, radius + 8f, 2f, BezelHighlight);
        DrawArcOutline(pivot, radius + 6f, 1f, BezelHighlight);
    }

    private static void DrawBase(Vector2 pivot, float radius)
    {
        var left = PointOnArc(pivot, radius + 8f, 0f);
        var right = PointOnArc(pivot, radius + 8f, 1f);
        Raylib.DrawLineEx(left, right, 6f, BaseColor);
        Raylib.DrawLineEx(left, right, 2f, BezelHighlight);
    }

    private static void DrawRedZone(Vector2 pivot, float radius)
    {
        const float redSpan = 0.22f;
        var redEnd = DialLevelEmpty + (DialLevelFull - DialLevelEmpty) * redSpan;
        DrawArcRing(
            pivot,
            radius - 34f,
            radius - 10f,
            RedZoneColor,
            DialLevelToDegrees(DialLevelEmpty),
            DialLevelToDegrees(redEnd));
    }

    private static void DrawTicks(Vector2 pivot, float radius)
    {
        for (var i = 0; i <= 10; i++)
        {
            var t = ToDialLevel(i / 10f);
            var major = i % 2 == 0;
            var inner = radius - (major ? 36f : 26f);
            var outer = radius - 12f;
            var color = major ? TickMajorColor : TickColor;
            var width = major ? 2.5f : 1.5f;
            Raylib.DrawLineEx(
                PointOnArc(pivot, inner, t),
                PointOnArc(pivot, outer, t),
                width,
                color);
        }
    }

    private static void DrawLabels(Vector2 pivot, float radius, Font labelFont)
    {
        DrawLabel(pivot, radius - 58f, DialLevelEmpty, "E", radius, labelFont);
        DrawLabel(pivot, radius - 58f, DialLevelFull, "F", radius, labelFont);
    }

    private static void DrawLabel(
        Vector2 pivot,
        float labelRadius,
        float dialLevel,
        string text,
        float radius,
        Font labelFont)
    {
        var pos = PointOnArc(pivot, labelRadius, dialLevel);
        int size = Math.Max(14, (int)(24f * radius / ReferenceRadius));
        float spacing = 0.5f;

        if (labelFont.BaseSize > 0)
        {
            var width = Raylib.MeasureTextEx(labelFont, text, size, spacing).X;
            Raylib.DrawTextEx(
                labelFont,
                text,
                new Vector2(pos.X - width / 2f, pos.Y - size / 2f),
                size,
                spacing,
                LabelColor);
        }
        else
        {
            var width = Raylib.MeasureText(text, size);
            Raylib.DrawText(text, (int)(pos.X - width / 2f), (int)(pos.Y - size / 2f), size, LabelColor);
        }
    }

    private static void DrawNeedle(Vector2 pivot, float length, float dialLevel)
    {
        var angleDeg = DialLevelToDegrees(dialLevel);
        var angle = angleDeg * MathF.PI / 180f;
        var dir = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        var start = pivot + dir * HubInnerRadius;
        var tip = pivot + dir * length;
        Raylib.DrawLineEx(start, tip, 4f, NeedleColor);
        Raylib.DrawCircleV(tip, 3f, NeedleColor);
    }

    private static void DrawHub(Vector2 pivot)
    {
        Raylib.DrawCircleV(pivot, 16f, HubColor);
        Raylib.DrawCircleLines((int)pivot.X, (int)pivot.Y, 16f, HubRingColor);
        Raylib.DrawCircleV(pivot, 6f, BezelHighlight);
    }

    private static float DialLevelToDegrees(float dialLevel) =>
        ArcStartDeg + (ArcEndDeg - ArcStartDeg) * dialLevel;

    /// <summary>Point on the top semicircle; dialLevel is in [DialLevelEmpty, DialLevelFull].</summary>
    private static Vector2 PointOnArc(Vector2 pivot, float distance, float dialLevel)
    {
        var angleDeg = DialLevelToDegrees(dialLevel);
        var angle = angleDeg * MathF.PI / 180f;
        return pivot + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;
    }

    private static void DrawArcFilled(Vector2 pivot, float radius, Color color) =>
        DrawArcRing(pivot, 0f, radius, color, ArcStartDeg, ArcEndDeg);

    private static void DrawArcRing(
        Vector2 pivot,
        float innerRadius,
        float outerRadius,
        Color color,
        float startDeg = ArcStartDeg,
        float endDeg = ArcEndDeg)
    {
        var step = (endDeg - startDeg) / ArcSegments;
        var angle = startDeg;

        for (var i = 0; i < ArcSegments; i++)
        {
            var a0 = angle * MathF.PI / 180f;
            var a1 = (angle + step) * MathF.PI / 180f;

            var inner0 = pivot + new Vector2(MathF.Cos(a0), MathF.Sin(a0)) * innerRadius;
            var inner1 = pivot + new Vector2(MathF.Cos(a1), MathF.Sin(a1)) * innerRadius;
            var outer0 = pivot + new Vector2(MathF.Cos(a0), MathF.Sin(a0)) * outerRadius;
            var outer1 = pivot + new Vector2(MathF.Cos(a1), MathF.Sin(a1)) * outerRadius;

            if (innerRadius <= 0.01f)
            {
                Raylib.DrawTriangle(pivot, outer0, outer1, color);
            }
            else
            {
                Raylib.DrawTriangle(inner0, outer0, outer1, color);
                Raylib.DrawTriangle(inner0, outer1, inner1, color);
            }

            angle += step;
        }
    }

    private static void DrawArcOutline(Vector2 pivot, float radius, float thickness, Color color)
    {
        var step = (ArcEndDeg - ArcStartDeg) / ArcSegments;
        var angle = ArcStartDeg;
        var prev = PointOnArcDegrees(pivot, radius, angle);

        for (var i = 0; i < ArcSegments; i++)
        {
            angle += step;
            var next = PointOnArcDegrees(pivot, radius, angle);
            Raylib.DrawLineEx(prev, next, thickness, color);
            prev = next;
        }
    }

    private static Vector2 PointOnArcDegrees(Vector2 pivot, float distance, float angleDeg)
    {
        var angle = angleDeg * MathF.PI / 180f;
        return pivot + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;
    }
}
