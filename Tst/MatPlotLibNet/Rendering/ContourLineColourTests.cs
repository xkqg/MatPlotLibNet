// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The lines of a line contour that names no colour map are coloured through the theme's map under the matplotlib themes,
/// as matplotlib's <c>contour</c> colours them: 3.11.2, <c>ContourSet._process_colors</c> (no <c>colors</c> given, so
/// <c>self.cvalues = self.layers</c> and <c>self.norm.autoscale_None(self.levels)</c>) maps every level through the contour's
/// <c>cmap</c>, which a <c>ScalarMappable</c> made with none takes from <c>rcParams['image.cmap']</c> (viridis in the default style,
/// jet in the classic one). The library's own themes draw such a line in their neutral grey, as they always did.</summary>
public sealed class ContourLineColourTests
{
    private static readonly double[] X = [0, 1, 2, 3, 4, 5];
    private static readonly double[] Y = [0, 1, 2, 3];

    /// <summary>z = 2x + 0.5: values 0.5 to 10.5, so a count of 4 lays the levels 0, 2, ..., 12 and the lines of 2 to 10 cross the grid.</summary>
    private static double[,] Ramp()
    {
        var z = new double[Y.Length, X.Length];
        for (int r = 0; r < Y.Length; r++)
            for (int c = 0; c < X.Length; c++)
                z[r, c] = 2.0 * c + 0.5;
        return z;
    }

    private static string[] Strokes(Theme theme, Action<ContourSeries>? configure = null)
    {
        string svg = Plt.Create().WithTheme(theme).AddSubPlot(1, 1, 1, ax => ax.Contour(X, Y, Ramp(), s => { s.Levels = 4; configure?.Invoke(s); })).ToSvg();
        return [.. svg.Elements("polyline").Select(polyline => polyline["stroke"]).OfType<string>().Where(stroke => stroke.StartsWith('#')).Select(stroke => stroke.ToUpperInvariant())];
    }

    /// <summary>The colour of each line that crosses the grid: the map at the level, over the first level laid to the last.</summary>
    private static string[] ThroughTheMap(IColorMap map, Theme theme)
    {
        double[] levels = new ContourSeries(X, Y, Ramp()) { Levels = 4 }.LevelsUnder(theme)!;
        return [.. levels.Where(level => level >= 0.5 && level <= 10.5)
            .Select(level => map.ColorOf(level, LinearNormalizer.Instance, levels[0], levels[^1]).ToHex().ToUpperInvariant())];
    }

    public static IEnumerable<object[]> MapsOfTheMatplotlibStyles() => [["v2", ColorMaps.Viridis], ["classic", ColorMaps.Jet]];

    /// <summary>The lines are viridis under the default matplotlib style and jet under the classic one, over the levels.</summary>
    [Theory]
    [MemberData(nameof(MapsOfTheMatplotlibStyles))]
    public void UnderAMatplotlibStyle_TheLinesAreItsMap_OverTheLevels(string name, IColorMap map)
    {
        var theme = name.AsTheme();

        Assert.Equal(ThroughTheMap(map, theme), Strokes(theme));
    }

    [Fact]
    public void UnderTheLibrarysOwnTheme_TheLinesStayNeutralGrey() =>
        Assert.All(Strokes(Theme.Default), stroke => Assert.Equal("#808080", stroke));

    [Theory]
    [InlineData("matplotlib-v2")]
    [InlineData("matplotlib-classic")]
    [InlineData("default")]
    public void AMapTheSeriesNames_WinsUnderEveryTheme(string name)
    {
        var theme = name.AsTheme();

        Assert.Equal(ThroughTheMap(ColorMaps.Plasma, theme), Strokes(theme, s => s.ColorMap = ColorMaps.Plasma));
    }
}
