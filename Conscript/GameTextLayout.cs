using System.Numerics;
using Raylib_cs;

namespace Conscript;

internal static class GameTextLayout
{
    /// <summary>
    /// Word-wraps text to fit within maxWidth, returning lines and total pixel height.
    /// </summary>
    public static (List<string> lines, int height) WrapForBox(
        string text,
        Font font,
        float fontSize,
        float spacing,
        int maxWidth,
        int lineHeight)
    {
        if (text == null)
            return (new List<string>(), 0);

        int blankLineHeight = lineHeight / 2;
        text = text.Replace("\r\n", "\n");
        var paragraphs = text.Split('\n', StringSplitOptions.None);
        var lines = new List<string>();
        int totalHeight = 0;

        foreach (string paragraph in paragraphs)
        {
            if (string.IsNullOrWhiteSpace(paragraph))
            {
                lines.Add("");
                totalHeight += blankLineHeight;
                continue;
            }

            string[] words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string current = "";

            foreach (string word in words)
            {
                string candidate = current.Length == 0 ? word : current + " " + word;
                Vector2 size = Raylib.MeasureTextEx(font, candidate, fontSize, spacing);

                if (size.X > maxWidth && current.Length > 0)
                {
                    lines.Add(current.Trim());
                    totalHeight += lineHeight;
                    current = word;
                }
                else
                {
                    current = candidate;
                }
            }

            if (current.Length > 0)
            {
                lines.Add(current.Trim());
                totalHeight += lineHeight;
            }
        }

        return (lines, totalHeight);
    }
}
