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

    /// <summary>Needle shaft crop in the needle texture — hub ring is on the face art.</summary>
    public const float NeedleSrcX = 707f;
    public const float NeedleSrcY = 19f;
    public const float NeedleSrcW = 120f;
    public const float NeedleSrcH = 860f;
    public const float NeedlePivotSrcX = 59.5f;
    public const float NeedlePivotSrcY = 859f;

    /// <summary>Shaft pivot on the shared gauge canvas (face + needle PNGs are aligned).</summary>
    public const float FacePivotXRatio = (NeedleSrcX + NeedlePivotSrcX) / TextureWidth;
    public const float FacePivotYRatio = (NeedleSrcY + NeedlePivotSrcY) / TextureHeight;

    public static void DrawNeedle(
        Texture2D needleTexture,
        Rectangle faceRect,
        int level)
    {
        if (needleTexture.Id == 0)
            return;

        float destX = faceRect.X + faceRect.Width * (NeedleSrcX / TextureWidth);
        float destY = faceRect.Y + faceRect.Height * (NeedleSrcY / TextureHeight);
        float destW = faceRect.Width * (NeedleSrcW / TextureWidth);
        float destH = faceRect.Height * (NeedleSrcH / TextureHeight);
        float originX = destW * (NeedlePivotSrcX / NeedleSrcW);
        float originY = destH * (NeedlePivotSrcY / NeedleSrcH);

        var needleDest = new Rectangle(destX, destY, destW, destH);
        var needleSrc = new Rectangle(NeedleSrcX, NeedleSrcY, NeedleSrcW, NeedleSrcH);
        float rotation = GetNeedleRotation(level);
        Raylib.DrawTexturePro(
            needleTexture,
            needleSrc,
            needleDest,
            new Vector2(originX, originY),
            rotation,
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
