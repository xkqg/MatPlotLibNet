// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Styling.ColorMapNormalizers;

/// <summary>The under, over and bad colours of a map that sets none are null: the default members of <see cref="IColorMap"/> (a map
/// that writes only its colours) and a built-in map whose extremes were not set.</summary>
public class IColorMapDefaultsTests
{
    public static TheoryData<string, IColorMap> MapsThatSetNoExtreme => new()
    {
        { "a map that writes only its colours", new TestColorMap("minimal", _ => new(0, 0, 0)) },
        { "viridis", ColorMaps.Viridis },
    };

    [Theory]
    [MemberData(nameof(MapsThatSetNoExtreme))]
    public void TheExtremeColoursOfAMapThatSetsNone_AreNull(string map, IColorMap colours)
    {
        Assert.Null(colours.GetUnderColor());
        Assert.Null(colours.GetOverColor());
        Assert.Null(colours.GetBadColor());
        _ = map;
    }
}
