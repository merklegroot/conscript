using System.Linq;
using System.Reflection;

namespace Conscript;

/// <summary>
/// Build timestamp injected at compile time via AssemblyMetadata in the project file.
/// </summary>
internal static class BuildInfo
{
    public static string Timestamp { get; } =
        typeof(BuildInfo).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "BuildTimestamp")?.Value
        ?? "dev build";
}
