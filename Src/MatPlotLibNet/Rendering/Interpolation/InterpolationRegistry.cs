// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace MatPlotLibNet.Rendering.Interpolation;

/// <summary>Thread-safe registry for <see cref="IInterpolationEngine"/> lookup by name.
/// The three built-in engines (nearest, bilinear, bicubic) are pre-registered.</summary>
public static class InterpolationRegistry
{
    private static readonly ConcurrentDictionary<string, IInterpolationEngine> Engines
        = new(StringComparer.OrdinalIgnoreCase);

    static InterpolationRegistry()
    {
        Register(NearestInterpolation.Instance.Name,  NearestInterpolation.Instance);
        Register(BilinearInterpolation.Instance.Name, BilinearInterpolation.Instance);
        Register(BicubicInterpolation.Instance.Name,  BicubicInterpolation.Instance);
    }

    /// <summary>Registers a custom interpolation engine under the given name.</summary>
    public static void Register(string name, IInterpolationEngine engine)
        => Engines[name] = engine;

    /// <summary>Gets an engine by name (case-insensitive), or <see langword="null"/> if not found.</summary>
    public static IInterpolationEngine? Get(string name)
        => Engines.TryGetValue(name, out var engine) ? engine : null;

    /// <summary>Gets all registered engine names.</summary>
    public static IEnumerable<string> Names => Engines.Keys;
}
