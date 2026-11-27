// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests;

/// <summary>A colour map an application wrote, as a test hands one to a series or a colour bar: no registry knows it, and it declares
/// exactly the members of <see cref="IColorMap"/> that have no default body, so the interface's own under, over and bad colours run
/// through it. A test that gives those derives a leaf that lists the interface again.</summary>
/// <param name="name">The map's name.</param>
/// <param name="color">The colour of each value from 0 to 1.</param>
internal class TestColorMap(string name, Func<double, Color> color) : IColorMap
{
    public string Name => name;
    public Color GetColor(double value) => color(value);
}
