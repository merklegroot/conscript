using System.IO;
using Raylib_cs;

namespace Conscript;

/// <summary>Loads the UI TTF from Fonts/ with Cyrillic and symbol coverage.</summary>
internal static class UiFontLoader
{
    // LoadFontEx requires an explicit glyph list; null/0 only loads ~95 ASCII chars.
    private static readonly int[] Codepoints = CreateCodepoints(
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789" +
        " !\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~°©®™…–—•·‘’“”«»₽" +
        "абвгдеёжзийклмнопрстуфхцчшщъыьэюяАБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ" +
        "\u25B2\u25BC"); // ▲▼ (stat trend arrows)

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

        return LoadFirstExisting(candidates) ?? Raylib.GetFontDefault();
    }

    public static Font LoadItalic()
    {
        string baseDir = AppContext.BaseDirectory;
        string[] candidates =
        {
            Path.Combine(baseDir, "Fonts", "OpenSans-Italic.ttf"),
        };

        return LoadFirstExisting(candidates) ?? Load();
    }

    private static Font? LoadFirstExisting(string[] candidates)
    {
        foreach (string path in candidates)
        {
            if (File.Exists(path))
                return Raylib.LoadFontEx(path, 40, Codepoints, Codepoints.Length);
        }

        return null;
    }

    private static int[] CreateCodepoints(string chars)
    {
        int[] codepoints = new int[chars.Length];
        for (int i = 0; i < chars.Length; i++)
            codepoints[i] = chars[i];

        return codepoints;
    }
}
