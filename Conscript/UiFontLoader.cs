using System.IO;
using Raylib_cs;

namespace Conscript;

/// <summary>Loads the UI TTF from Fonts/ with Cyrillic and symbol coverage.</summary>
internal static class UiFontLoader
{
    public static Font Load()
    {
        string baseDir = AppContext.BaseDirectory;
        string[] candidates =
        {
            Path.Combine(baseDir, "Fonts", "OpenSans.ttf"),
            Path.Combine(baseDir, "Fonts", "OpenSans-Regular.ttf"),
            Path.Combine(baseDir, "Fonts", "Inter.ttf"),
            Path.Combine(baseDir, "Fonts", "Roboto-Regular.ttf"),
        };

        // LoadFontEx requires an explicit glyph list; null/0 only loads ~95 ASCII chars.
        const string chars =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789" +
            " !\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~°©®™…–—•·‘’“”«»₽" +
            "абвгдеёжзийклмнопрстуфхцчшщъыьэюяАБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ" +
            "\u25B2\u25BC"; // ▲▼ (stat trend arrows)

        int[] codepoints = new int[chars.Length];
        for (int i = 0; i < chars.Length; i++)
            codepoints[i] = chars[i];

        foreach (string path in candidates)
        {
            if (File.Exists(path))
                return Raylib.LoadFontEx(path, 40, codepoints, codepoints.Length);
        }

        return Raylib.GetFontDefault();
    }
}
