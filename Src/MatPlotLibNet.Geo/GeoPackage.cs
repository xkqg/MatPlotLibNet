// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using MatPlotLibNet.Geo.Series;

namespace MatPlotLibNet.Geo;

/// <summary>What this package does when it is loaded: it registers the reader that rebuilds a map from the figure's JSON.</summary>
internal static class GeoPackage
{
    /// <summary>Runs once when the assembly loads, as <c>MatPlotLibNet.Skia</c> registers its exporters. A process that reads a
    /// map's JSON without having used any type of this package calls <see cref="GeoPolygonSeries.RegisterReader"/> once.
    /// CA2255 (the module initializer is meant for application code) is suppressed on purpose: this is the package's
    /// advertised entry point.</summary>
#pragma warning disable CA2255
    [ModuleInitializer]
    internal static void Initialize() => GeoPolygonSeries.RegisterReader();
#pragma warning restore CA2255
}
