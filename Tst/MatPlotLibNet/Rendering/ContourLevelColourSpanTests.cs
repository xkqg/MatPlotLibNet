// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A contour colours its levels over the levels, whether it laid them or was given them: matplotlib 3.11.2,
/// <c>ContourSet._process_colors</c> ends with <c>self.norm.autoscale_None(self.levels)</c>, so <c>vmin</c> and <c>vmax</c> are the
/// lowest and the highest level (<c>contour(X, Y, Z, levels=[2, 4])</c> has <c>norm.vmin, vmax = 2.0, 4.0</c>), not the range of the
/// values, and a level the grid never reaches still counts. The library mapped the levels a caller named over the values from the
/// lowest to the highest, and the levels it laid over the levels. A filled contour's bands take the same span as its lines.</summary>
public sealed class ContourLevelColourSpanTests
{
    private static readonly double[] X = [0, 1, 2, 3, 4, 5];
    private static readonly double[] Y = [0, 1, 2, 3];

    /// <summary>z = 2x + 0.5: values 0.5 to 10.5.</summary>
    private static ContourSeries Ramp(double[] levels)
    {
        var z = new double[Y.Length, X.Length];
        for (int r = 0; r < Y.Length; r++)
            for (int c = 0; c < X.Length; c++)
                z[r, c] = 2.0 * c + 0.5;
        return new ContourSeries(X, Y, z) { LevelValues = levels, ColorMap = ColorMaps.Viridis };
    }

    private static string[] Strokes(Theme theme, double[] levels, Action<ContourSeries>? configure = null)
    {
        string svg = Plt.Create().WithTheme(theme).AddSubPlot(1, 1, 1, ax => ax.Contour(X, Y, Ramp(levels).ZData, s =>
        {
            s.LevelValues = levels;
            s.ColorMap = ColorMaps.Viridis;
            configure?.Invoke(s);
        })).ToSvg();
        return [.. svg.Elements("polyline").Select(polyline => polyline["stroke"]).OfType<string>().Where(stroke => stroke.StartsWith('#')).Select(stroke => stroke.ToUpperInvariant())];
    }

    private static string Hex(double fraction) =>
        ColorMaps.Viridis.ColorOf(fraction, LinearNormalizer.Instance, 0, 1).ToHex().ToUpperInvariant();

    /// <summary>The first named level is the map's first colour and the last its last, whatever the values span.</summary>
    [Theory]
    [MemberData(nameof(ThemeCases.MatplotlibAndDefault), MemberType = typeof(ThemeCases))]
    public void NamedLevels_AreColouredOverTheLevels(string name) =>
        Assert.Equal([Hex(0), Hex(1)], Strokes(name.AsTheme(), [2, 4]));

    /// <summary>A level outside the grid draws no line but still bounds the span: 2 of the levels -5, 2 and 20 stands at 7 / 25.</summary>
    [Fact]
    public void ALevelTheGridNeverReaches_StillBoundsTheSpan() =>
        Assert.Equal([Hex(7.0 / 25)], Strokes(Theme.MatplotlibV2, [-5, 2, 20]));

    /// <summary>One named level has no span; matplotlib's norm then returns 0 (<c>vmin == vmax</c>), the map's first colour.</summary>
    [Fact]
    public void OneNamedLevel_IsTheFirstColourOfTheMap() =>
        Assert.Equal([Hex(0)], Strokes(Theme.MatplotlibV2, [4]));

    /// <summary>A filled contour's bands take the span of its lines: the band below the lowest level and the one above the highest
    /// are the map's ends, the band between the two named levels its middle.</summary>
    [Fact]
    public void TheBandsOfAFilledContour_TakeTheSpanOfTheLevels()
    {
        var series = Ramp([2, 4]);
        series.Filled = true;
        var bounds = new Rect(0, 0, 400, 300);
        var ctx = new RecordingRenderContext();
        new ContourSeriesRenderer(new SeriesRenderContext(new DataTransform(0, 5, 0, 3, bounds), ctx, Colors.Tab10Blue, new RenderArea(bounds, ctx))
        {
            Theme = Theme.MatplotlibV2,
        }).Render(series);

        var fills = ctx.Of<ShapeCall>().OfType<ShapeCall>().Where(c => c is DrawRectangleCall or DrawPathCall)
            .Select(c => c.Fill!.Value.ToHex().ToUpperInvariant()).ToArray();

        Assert.Equal([Hex(0), Hex(0.5), Hex(1)], fills);
    }
}
