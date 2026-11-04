using System;
using System.Collections.Generic;

namespace CrystalM2.Rendering.Core;

// A library must be explicitly declared free of baked ground shadows before
// its actor silhouettes may generate an additional projected shadow.
public sealed class ShadowSourcePolicy
{
    private readonly HashSet<string> _libraries = new(StringComparer.OrdinalIgnoreCase);
    public ShadowSourcePolicy(string libraries)
    {
        foreach (string library in (libraries ?? "").Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            _libraries.Add(Normalize(library));
    }
    public bool Allows(string library) => library != null && _libraries.Contains(Normalize(library));
    private static string Normalize(string path)
    {
        path = path.Replace('\\', '/');
        while (path.StartsWith("./", StringComparison.Ordinal)) path = path[2..];
        return path;
    }
}
