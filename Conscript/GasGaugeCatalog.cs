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

    /// <summary>Rotation pivot on the needle texture, measured from the top edge.</summary>
    public const float NeedlePivotYRatio = 0.92f;

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
