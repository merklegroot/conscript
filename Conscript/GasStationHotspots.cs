namespace Conscript;

/// <summary>Clickable regions on gas-station.png.</summary>
internal static class GasStationHotspots
{
    public const int PumpCount = 2;

    public const float Pump1X1 = 0.361f;
    public const float Pump1Y1 = 0.476f;
    public const float Pump1X2 = 0.435f;
    public const float Pump1Y2 = 0.648f;

    public const float Pump2X1 = 0.589f;
    public const float Pump2Y1 = 0.465f;
    public const float Pump2X2 = 0.657f;
    public const float Pump2Y2 = 0.636f;

    public const float KioskX1 = 0.747f;
    public const float KioskY1 = 0.377f;
    public const float KioskX2 = 0.986f;
    public const float KioskY2 = 0.630f;

    public static void GetPumpRegion(int pumpIndex, out float x1, out float y1, out float x2, out float y2)
    {
        if (pumpIndex == 0)
        {
            x1 = Pump1X1;
            y1 = Pump1Y1;
            x2 = Pump1X2;
            y2 = Pump1Y2;
            return;
        }

        x1 = Pump2X1;
        y1 = Pump2Y1;
        x2 = Pump2X2;
        y2 = Pump2Y2;
    }
}
