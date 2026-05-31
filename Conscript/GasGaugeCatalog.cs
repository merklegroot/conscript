using System.Numerics;
using Raylib_cs;

namespace Conscript;

/// <summary>Fuel gauge assets and needle rotation for the warehouse truck cab.</summary>
internal static class GasGaugeCatalog
{
    public const string FaceFile = "truck-gas-gauge-face.png";
    public const string NeedleFile = "truck-gas-gauge-needle.png";

    public const int LevelCount = 5;
    public const int DefaultLevel = 2;

    /// <summary>Needle points to E (lower-left) when level is empty.</summary>
    public const float NeedleRotationEmpty = 135f;

    /// <summary>Needle points to F (lower-right) when level is full.</summary>
    public const float NeedleRotationFull = -135f;

    /// <summary>Needle sprite crop in the needle texture (pixels).</summary>
    public const float NeedleSrcX = 707f;
    public const float NeedleSrcY = 19f;
    public const float NeedleSrcW = 120f;
    public const float NeedleSrcH = 976f;
    public const float NeedlePivotSrcX = 59.5f;
    public const float NeedlePivotSrcY = 975f;

    /// <summary>Needle tip position on the gauge face when pointing straight up, normalized 0–1.</summary>
    public const float FaceNeedleTipYRatio = 0.020f;

    /// <summary>Needle hub position on the gauge face, normalized 0–1.</summary>
    public const float FacePivotXRatio = 0.500f;
    public const float FacePivotYRatio = 0.622f;

    public static void DrawNeedle(
        Texture2D needleTexture,
        Rectangle faceRect,
        int level,
        float faceHubXRatio,
        float faceHubYRatio)
    {
        if (needleTexture.Id == 0)
            return;

        float hubX = faceRect.X + faceRect.Width * faceHubXRatio;
        float hubY = faceRect.Y + faceRect.Height * faceHubYRatio;

        float needleDestH = faceRect.Height * (faceHubYRatio - FaceNeedleTipYRatio);
        float needleDestW = needleDestH * (NeedleSrcW / NeedleSrcH);
        float originX = needleDestW * (NeedlePivotSrcX / NeedleSrcW);
        float originY = needleDestH * (NeedlePivotSrcY / NeedleSrcH);

        var needleDest = new Rectangle(
            hubX - originX,
            hubY - originY,
            needleDestW,
            needleDestH);
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
