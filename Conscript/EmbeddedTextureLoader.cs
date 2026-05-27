using System.IO;
using System.Linq;
using System.Reflection;
using Raylib_cs;

namespace Conscript;

/// <summary>Loads PNGs embedded as assembly resources (see Conscript.csproj img/**/*).</summary>
internal static class EmbeddedTextureLoader
{
    public static Texture2D Load(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        string[] candidates =
        {
            $"Conscript.img.{fileName}",
            $"Conscript.{fileName}",
            fileName,
            $"img.{fileName}"
        };

        foreach (string name in candidates)
        {
            using Stream? stream = assembly.GetManifestResourceStream(name);
            if (stream != null)
            {
                byte[] data = new byte[stream.Length];
                stream.ReadExactly(data);
                string ext = Path.GetExtension(fileName);
                if (string.IsNullOrEmpty(ext)) ext = ".png";

                Image image = Raylib.LoadImageFromMemory(ext, data);
                if (image.Width <= 0 || image.Height <= 0)
                {
                    Raylib.UnloadImage(image);
                    image = Raylib.GenImageColor(1, 1, Color.DARKGRAY);
                }

                Texture2D texture = Raylib.LoadTextureFromImage(image);
                Raylib.UnloadImage(image);
                return texture;
            }
        }

        string available = string.Join(", ", assembly.GetManifestResourceNames().Take(30));
        throw new FileNotFoundException(
            $"Embedded image '{fileName}' not found. Tried names: {string.Join(", ", candidates)}. Available resources: {available}");
    }
}
