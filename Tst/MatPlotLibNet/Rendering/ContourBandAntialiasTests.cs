// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The bands of a filled contour are drawn with no antialiasing under the matplotlib themes: matplotlib 3.11.2,
/// <c>ContourSet.__init__</c>: <c>if antialiased is None and filled: antialiased = False</c> ("Eliminate artifacts; we are not
/// stroking the boundaries"). A band is a fill of a path with no edge of its own, so its boundary falls inside a pixel and the
/// antialiasing blends the two bands there: the peaks surface's picture held 214 colours in the plot where matplotlib's holds 10.
/// The fill says so on its <see cref="ShapeStyle"/> (<c>Aliased</c>), which every backend reads: the SVG as
/// <c>shape-rendering="crispEdges"</c>, the raster export in its paint. The library's own themes keep the antialiased edge.</summary>
public sealed class ContourBandAntialiasTests
{
    private static readonly double[] Axis = [0.0, 1.0, 2.0, 3.0, 4.0];

    private static double[,] Diagonal()
    {
        var z = new double[5, 5];
        for (int r = 0; r < 5; r++)
            for (int c = 0; c < 5; c++)
                z[r, c] = r + c;
        return z;
    }

    private static readonly Rect Bounds = new(0, 0, 400, 300);

    private static bool[] FillsAliased(Theme theme, Action<SeriesRenderContext> draw)
    {
        var ctx = new RecordingRenderContext();
        draw(new SeriesRenderContext(new DataTransform(0, 4, 0, 4, Bounds), ctx, Colors.Tab10Blue, new RenderArea(Bounds, ctx)) { Theme = theme });
        return [.. ctx.Of<ShapeCall>().Where(c => c is DrawRectangleCall or DrawPathCall)
            .Select(c => c.Shape.Aliased)];
    }

    private static void Contourf(SeriesRenderContext c) =>
        new ContourfSeriesRenderer(c).Render(new ContourfSeries(Axis, Axis, Diagonal()) { Levels = 4, ShowLines = false });

    private static void FilledContour(SeriesRenderContext c) =>
        new ContourSeriesRenderer(c).Render(new ContourSeries(Axis, Axis, Diagonal()) { Levels = 4, Filled = true });

    [Theory]
    [InlineData("matplotlib-v2")]
    [InlineData("matplotlib-classic")]
    public void EveryBand_OfAFilledContour_IsAliased_UnderTheMatplotlibThemes(string name)
    {
        var theme = name.AsTheme();

        var fills = FillsAliased(theme, Contourf);

        Assert.True(fills.Length > 1);
        Assert.All(fills, aliased => Assert.True(aliased));
    }

    [Fact]
    public void EveryBand_OfAFilledLineContour_IsAliased_UnderTheMatplotlibThemes()
    {
        var fills = FillsAliased(Theme.MatplotlibV2, FilledContour);

        Assert.True(fills.Length > 1);
        Assert.All(fills, aliased => Assert.True(aliased));
    }

    [Fact]
    public void EveryBand_KeepsItsAntialiasedEdge_UnderTheLibrarysOwnTheme()
    {
        Assert.All(FillsAliased(Theme.Default, Contourf), aliased => Assert.False(aliased));
        Assert.All(FillsAliased(Theme.Default, FilledContour), aliased => Assert.False(aliased));
    }

    /// <summary>A shape built the ordinary way is antialiased, and the flag is part of the style's identity.</summary>
    [Fact]
    public void AShapeStyle_IsAntialiased_UntilItSaysOtherwise()
    {
        var plain = new ShapeStyle(Colors.Red, null, 0);

        Assert.False(plain.Aliased);
        Assert.NotEqual(plain, plain with { Aliased = true });
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("TheSvg_WritesCrispEdges_ForAnAliasedFill_AndOnlyThen (#1)", () =>
        {
            string Svg(Theme theme) => Plt.Create().WithTheme(theme)
                .AddSubPlot(1, 1, 1, ax => ax.Contourf(Axis, Axis, Diagonal(), s => { s.Levels = 4; s.ShowLines = false; })).ToSvg();
            return Svg(Theme.MatplotlibV2);
        }, "shape-rendering=\"crispEdges\""),
        new DrawnToken("TheSvg_WritesCrispEdges_ForAnAliasedFill_AndOnlyThen (#2)", () =>
        {
            string Svg(Theme theme) => Plt.Create().WithTheme(theme)
                .AddSubPlot(1, 1, 1, ax => ax.Contourf(Axis, Axis, Diagonal(), s => { s.Levels = 4; s.ShowLines = false; })).ToSvg();
            return Svg(Theme.Default);
        }) { Absent = ["shape-rendering"] },
        // The control: a chart that is not a filled contour writes no such attribute under the same theme.
        new DrawnToken("ABarChart_WritesNoCrispEdges_UnderV2",
            () => Plt.Create().WithTheme(Theme.MatplotlibV2).AddSubPlot(1, 1, 1, ax => ax.Bar(["a", "b"], [1.0, 2.0])).ToSvg()) { Absent = ["shape-rendering"] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
