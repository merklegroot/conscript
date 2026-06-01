using System.Numerics;
using Raylib_cs;

namespace Conscript;

/// <summary>Fuel gauge assets and needle rotation for the warehouse truck cab.</summary>
internal static class GasGaugeCatalog
{
    public const string FaceFile = "truck-gas-gauge-face.png";
    public const string NeedleFile = "truck-gas-gauge-needle.png";

    public const int TextureWidth = 1536;
    public const int TextureHeight = 1024;

    public const int LevelCount = 5;
    public const int DefaultLevel = 2;

    /// <summary>Needle points to E (lower-left) when level is empty.</summary>
    public const float NeedleRotationEmpty = 135f;

    /// <summary>Needle points to F (lower-right) when level is full.</summary>
    public const float NeedleRotationFull = -135f;

    /// <summary>Needle hub on the shared 1536×1024 gauge canvas (face + needle PNGs align 1:1).</summary>
    public const float HubPivotX = 766.5f;
    public const float HubPivotY = 906f;

    public static void DrawNeedle(
        Texture2D needleTexture,
        Rectangle faceRect,
        int level)
    {
        if (needleTexture.Id == 0)
            return;

        var needleSrc = new Rectangle(0, 0, TextureWidth, TextureHeight);
        var origin = new Vector2(
            faceRect.Width * (HubPivotX / TextureWidth),
            faceRect.Height * (HubPivotY / TextureHeight));

        // DrawTexturePro dest is the pivot in screen space; scale the full needle canvas like the face.
        var needleDest = new Rectangle(
            faceRect.X + origin.X,
            faceRect.Y + origin.Y,
            faceRect.Width,
            faceRect.Height);

        Raylib.DrawTexturePro(
            needleTexture,
            needleSrc,
            needleDest,
            origin,
            GetNeedleRotation(level),
            Color.WHITE);
    }

    public static float GetNeedleRotation(int level)
    {
        level = Math.Clamp(level, 0, LevelCount - 1);
        float t = level / (float)(LevelCount - 1);
        return NeedleRotationEmpty + t * (NeedleRotationFull - NeedleRotationEmpty);
    }

    public static string GetLevelLabel(int level) =>
        Math.Clamp(level, 0, LevelCount - 1) switch
        {
            0 => "EMPTY",
            1 => "1/4 TANK",
            2 => "1/2 TANK",
            3 => "3/4 TANK",
            _ => "FULL"
        };
}
