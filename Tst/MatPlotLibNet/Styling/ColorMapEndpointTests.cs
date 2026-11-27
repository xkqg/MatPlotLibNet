// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>What a colour map looks like at the points its name promises: <c>hot</c> starts near-black and ends near-white, <c>jet</c> starts
/// dark blue, goes through green and ends dark red. One row of <see cref="Endpoints"/> for each map and position, with the look it must have; the
/// exact colours of every map are held by the tables read from matplotlib (<c>MatplotlibColorTablesTests</c>), so a row names the look only.</summary>
public class ColorMapEndpointTests
{
    /// <summary>A map read at a position, the look it must have there, and the test of that look.</summary>
    public readonly record struct ColorMapEndpoint(IColorMap Map, double At, string Look, Func<Color, bool> Holds);

    private static ColorMapEndpoint Is(IColorMap map, double at, string look, Func<Color, bool> holds) => new(map, at, look, holds);

    private static int Brightness(Color c) => c.R + c.G + c.B;

    private static Func<Color, bool> Below(int limit) => c => c.R < limit && c.G < limit && c.B < limit;

    private static Func<Color, bool> Above(int limit) => c => c.R > limit && c.G > limit && c.B > limit;

    private static readonly Func<Color, bool> Light = c => Brightness(c) > 700;
    private static readonly Func<Color, bool> Blue = c => c.B > c.R;
    private static readonly Func<Color, bool> Red = c => c.R > c.B;
    private static readonly Func<Color, bool> Purple = c => c.B > c.G;
    private static readonly Func<Color, bool> Green = c => c.G > c.R && c.G > c.B;
    private static readonly Func<Color, bool> PureRed = c => c.R > 200 && c.G < 50 && c.B < 50;
    private static readonly Func<Color, bool> Yellow = c => c.R > 200 && c.G > 200;

    public static IEnumerable<object[]> Endpoints() => new ColorMapEndpoint[]
    {
        // sequential
        Is(SequentialColorMaps.Hot, 0.0, "near-black", Below(20)), Is(SequentialColorMaps.Hot, 1.0, "near-white", Above(240)),
        Is(SequentialColorMaps.Copper, 0.0, "near-black", Below(30)), Is(SequentialColorMaps.Copper, 1.0, "a copper tone, R > G >= B", c => c.R > c.G && c.G >= c.B),
        Is(SequentialColorMaps.Bone, 0.0, "near-black", Below(30)), Is(SequentialColorMaps.Bone, 1.0, "near-white", Above(200)),
        Is(SequentialColorMaps.BuPu, 0.0, "light", Light), Is(SequentialColorMaps.BuPu, 1.0, "purple, blue over green", Purple),
        Is(SequentialColorMaps.GnBu, 0.0, "light", Light), Is(SequentialColorMaps.GnBu, 1.0, "blue over red", Blue),
        Is(SequentialColorMaps.PuRd, 0.0, "light", Light), Is(SequentialColorMaps.PuRd, 1.0, "red over blue", Red),
        Is(SequentialColorMaps.RdPu, 0.0, "light", Light), Is(SequentialColorMaps.RdPu, 1.0, "purple, blue over green", Purple),
        Is(SequentialColorMaps.YlGnBu, 0.0, "light", Light), Is(SequentialColorMaps.YlGnBu, 1.0, "blue over red", Blue),
        Is(SequentialColorMaps.PuBuGn, 0.0, "light", Light), Is(SequentialColorMaps.PuBuGn, 1.0, "green at least red", c => c.G >= c.R),
        Is(SequentialColorMaps.Cubehelix, 0.0, "near-black", c => Brightness(c) < 60), Is(SequentialColorMaps.Cubehelix, 1.0, "near-white", c => Brightness(c) > 650),
        // diverging (matplotlib 3.11.2: PuOr runs from (127, 59, 8), a dark orange-brown, to (45, 0, 75), a dark purple)
        Is(DivergingColorMaps.PuOr, 0.0, "orange, red over blue", Red), Is(DivergingColorMaps.PuOr, 1.0, "purple, blue over red", Blue),
        Is(DivergingColorMaps.Seismic, 0.0, "blue", Blue), Is(DivergingColorMaps.Seismic, 1.0, "red", Red),
        Is(DivergingColorMaps.Bwr, 0.0, "blue", c => c.B > c.R && c.B > c.G), Is(DivergingColorMaps.Bwr, 0.5, "near-white", Above(240)),
        Is(DivergingColorMaps.Bwr, 1.0, "red", c => c.R > c.B && c.R > c.G),
        // qualitative: the first colour of the set
        Is(QualitativeColorMaps.Pastel2, 0.0, "greenish (#B3E2CD)", c => c.G >= c.R), Is(QualitativeColorMaps.Dark2, 0.0, "teal (#1B9E77), green over red", c => c.G > c.R),
        Is(QualitativeColorMaps.Accent, 0.0, "green (#7FC97F)", Green), Is(QualitativeColorMaps.Paired, 0.0, "light blue (#A6CEE3)", c => c.B >= c.R),
        // special
        Is(ColorMaps.Turbo, 0.0, "dark blue", Blue), Is(ColorMaps.Turbo, 1.0, "dark red", Red),
        Is(ColorMaps.Jet, 0.0, "dark blue", Blue), Is(ColorMaps.Jet, 0.5, "greenish", c => c.G > c.B), Is(ColorMaps.Jet, 1.0, "dark red", Red),
        Is(CyclicColorMaps.Hsv, 0.0, "red", PureRed), Is(CyclicColorMaps.Hsv, 1.0, "red again (cyclic)", PureRed),
        Is(ColorMaps.Viridis, 0.0, "dark", c => c.R < 128), Is(ColorMaps.Viridis, 1.0, "light", c => c.R > 128),
        Is(ColorMaps.Blues, 0.0, "light", c => c.R > 200), Is(ColorMaps.Blues, 1.0, "blue", c => c.B > c.R && c.B > c.G),
        Is(ColorMaps.Reds, 0.0, "light", c => c.R > 200),
        // the additional maps (matplotlib 3.11.2: ocean starts at (0, 127, 0), its green at a half and its blue at 0)
        Is(AdditionalColorMaps.Gray, 0.0, "near-black", Below(20)), Is(AdditionalColorMaps.Gray, 1.0, "near-white", Above(240)),
        Is(AdditionalColorMaps.Spring, 0.0, "magenta, R high and G 0", c => c.R > 200 && c.G < 10), Is(AdditionalColorMaps.Spring, 1.0, "yellow", Yellow),
        Is(AdditionalColorMaps.Summer, 0.0, "greenish, G over R", c => c.G > c.R),
        Is(AdditionalColorMaps.Autumn, 0.0, "red", c => c.R > 200 && c.G < 10), Is(AdditionalColorMaps.Autumn, 1.0, "yellow", Yellow),
        Is(AdditionalColorMaps.Winter, 0.0, "bluish, B over R", Blue),
        Is(AdditionalColorMaps.Cool, 0.0, "cyan", c => c.G > 200 && c.B > 200 && c.R < 10), Is(AdditionalColorMaps.Cool, 1.0, "magenta", c => c.R > 200 && c.B > 200),
        Is(AdditionalColorMaps.AfmHot, 0.0, "dark", c => Brightness(c) < 30), Is(AdditionalColorMaps.CMRmap, 0.0, "dark", c => Brightness(c) < 30),
        Is(AdditionalColorMaps.PRGn, 0.0, "purplish", c => c.R > 50 && c.B > 50), Is(AdditionalColorMaps.PRGn, 1.0, "green", Green),
        Is(AdditionalColorMaps.RdGy, 0.0, "reddish", Red),
        Is(AdditionalColorMaps.Rainbow, 0.0, "blue", Blue), Is(AdditionalColorMaps.Rainbow, 1.0, "red", Red),
        Is(AdditionalColorMaps.Ocean, 0.0, "green", Green),
    }.Select(endpoint => new object[] { endpoint });

    [Theory]
    [MemberData(nameof(Endpoints))]
    public void AColorMap_AtAnEndpoint_HasTheLookItsNamePromises(ColorMapEndpoint endpoint)
    {
        var c = endpoint.Map.GetColor(endpoint.At);

        Assert.True(endpoint.Holds(c), $"{endpoint.Map.Name} at {endpoint.At} should be {endpoint.Look}, got ({c.R},{c.G},{c.B})");
    }
}
