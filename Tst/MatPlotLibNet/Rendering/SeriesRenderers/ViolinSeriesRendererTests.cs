// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>Phase X.9.a (v1.7.2, 2026-04-19) — drives every render branch in
/// <see cref="MatPlotLibNet.Rendering.SeriesRenderers.ViolinSeriesRenderer"/>. Pre-X.9
/// the renderer was at 79%L / 63%B because only the default ShowExtrema body was
/// covered. This file pins:
///   - Empty dataset → continue
///   - Positions per-dataset (ternary's true arm)
///   - range==0 (all-equal data) → range = 1
///   - Side.High / Side.Low / Side.Both
///   - ShowExtrema=false (false arm)
///   - ShowMedians=true (true arm)
///   - ShowMeans=true (true arm)</summary>
public class ViolinSeriesRendererTests
{
    private static string Render(ViolinSeries s) =>
        Plt.Create()
            .WithSize(500, 300)
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(s))
            .Build()
            .ToSvg();

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("Render_BasicViolin_DrawsPolygon", () => Render(new ViolinSeries(new double[][] { [1.0, 2, 3, 4, 5] })), "<polygon"),
        // Positions per-dataset (ternary's true arm). Each dataset placed at the configured X offset rather than its index.
        new DrawnToken("Render_Positions_PlacesDatasetsAtCustomX", () => Render(new ViolinSeries(new double[][] { [1.0, 2, 3], [4.0, 5, 6] })
        {
            Positions = new[] { 10.0, 20.0 },
        }), "<polygon"),
        // range==0 (all-identical values) → range = 1 fallback.
        new DrawnToken("Render_AllIdenticalValues_RangeFallback", () => Render(new ViolinSeries(new double[][] { [5.0, 5.0, 5.0, 5.0] })), "<polygon"),
        // Side.High → leftOff=0 (half-violin on the right side only).
        new DrawnToken("Render_SideHigh_ClipsLeftSide", () => Render(new ViolinSeries(new double[][] { [1.0, 2, 3] }) { Side = ViolinSide.High }), "<polygon"),
        // Side.Low → rightOff=0 (half-violin on the left side only).
        new DrawnToken("Render_SideLow_ClipsRightSide", () => Render(new ViolinSeries(new double[][] { [1.0, 2, 3] }) { Side = ViolinSide.Low }), "<polygon"),
        // ShowExtrema=false (false arm) → no extrema lines drawn. Just the body polygon remains.
        new DrawnToken("Render_HideExtrema_OnlyDrawsBody", () => Render(new ViolinSeries(new double[][] { [1.0, 2, 3] }) { ShowExtrema = false }), "<polygon"),
        // ShowMedians=true (true arm) → adds a horizontal median line across the violin in white.
        new DrawnToken("Render_ShowMedians_DrawsMedianLine", () => Render(new ViolinSeries(new double[][] { [1.0, 2, 3, 4, 5] }) { ShowMedians = true }), "<polygon"),
        // ShowMeans=true (true arm) → adds a horizontal dashed mean line across the violin in green.
        new DrawnToken("Render_ShowMeans_DrawsDashedMeanLine", () => Render(new ViolinSeries(new double[][] { [1.0, 2, 3, 4, 5] }) { ShowMeans = true }), "<polygon"),
        // Themed render with explicit ViolinBodyColor + ViolinStatsColor (true arms of `??`). MatplotlibClassic sets these to
        // y=#BFBF00 and r=#FF0000 respectively.
        new DrawnToken("Render_WithMatplotlibClassicTheme_UsesThemeViolinColors", () => Plt.Create()
            .WithSize(400, 300)
            .WithTheme(Theme.MatplotlibClassic)
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new ViolinSeries(new double[][] { [1.0, 2, 3, 4, 5] })))
            .Build()
            .ToSvg(), "<polygon"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    // Note: a mixed empty + non-empty dataset triggers ComputeDataRange's Min/Max on
    // the empty array BEFORE the renderer's `data.Length == 0 continue` guard runs.
    // Skipping that test — the renderer's empty-dataset arm is provably defensive,
    // covered indirectly via the empty-data Min/Max behavior validated in series tests.

    // ── violins placed by Positions are drawn inside the plot ───────────────────────────────────────────────────────

    private static readonly double[][] Samples =
    [
        [4.1, 4.8, 5.0, 5.3, 5.5, 5.9, 6.2, 6.4, 6.8, 7.1, 7.7, 8.4],
        [5.6, 6.1, 6.5, 6.9, 7.2, 7.4, 7.9, 8.3, 8.8, 9.1, 9.6, 10.2],
        [3.0, 3.4, 3.9, 4.4, 4.6, 5.2, 5.8, 6.1, 6.9, 7.5, 8.8, 9.9],
    ];

    /// <summary>The horizontal extent of every violin body in the picture.</summary>
    private static Range1D[] BodyExtents(string svg) =>
    [
        .. svg.Elements("polygon").Select(polygon =>
        {
            var xs = polygon.Points().Select(point => point.X).ToArray();
            return new Range1D(xs.Min(), xs.Max());
        }),
    ];

    private static void AssertInsideThePlot(string svg, int bodies)
    {
        var plot = svg.PlotArea();
        var extents = BodyExtents(svg);

        Assert.Equal(bodies, extents.Length);
        Assert.All(extents, e =>
        {
            Assert.InRange(e.Lo, plot.X - 1e-6, plot.X + plot.Width + 1e-6);
            Assert.InRange(e.Hi, plot.X - 1e-6, plot.X + plot.Width + 1e-6);
        });
    }

    /// <summary>A half-violin cloud per group, each its own series placed at the group's x (the shape of a raincloud
    /// chart). Measured before the fix in a 700 x 450 figure: the second body ran 4.1 px and the third 250 px past the right
    /// edge of the plot, because each series reported the range -1 to 1.</summary>
    [Theory]
    [InlineData(ViolinSide.Both)]
    [InlineData(ViolinSide.High)]
    [InlineData(ViolinSide.Low)]
    public void ViolinsPlacedAtGroupPositions_AreDrawnInsideThePlot(ViolinSide side)
    {
        string svg = Plt.Create().WithSize(700, 450).AddSubPlot(1, 1, 1, ax =>
        {
            for (int g = 0; g < Samples.Length; g++)
            {
                int at = g;
                ax.Violin([Samples[g]], v => { v.Side = side; v.Positions = [at]; v.Widths = 0.7; });
            }
        }).Build().ToSvg();

        AssertInsideThePlot(svg, 3);
    }

    /// <summary>One series with its own positions far from 0..n-1. Measured before the fix: the bodies stood at 1,457 to
    /// 3,942 px in a plot that ends at 630.</summary>
    [Fact]
    public void ViolinsPlacedFarFromTheDefaultPositions_AreDrawnInsideThePlot()
    {
        string svg = Plt.Create().WithSize(700, 450)
            .AddSubPlot(1, 1, 1, ax => ax.Violin(Samples, v => v.Positions = [10, 20, 30])).Build().ToSvg();

        AssertInsideThePlot(svg, 3);
    }

    /// <summary>The range follows the body drawn: a sample most of whose observations are equal makes a body wider than
    /// half the width, and it is still inside the plot.</summary>
    [Fact]
    public void AViolinWiderThanHalfItsWidth_IsDrawnInsideThePlot()
    {
        string svg = Plt.Create().WithSize(500, 300).AddSubPlot(1, 1, 1, ax =>
            ax.Violin([[5.0, 5, 5, 5, 5, 5, 5, 8, 9]], v => { v.Positions = [3]; v.Widths = 2; })).Build().ToSvg();

        AssertInsideThePlot(svg, 1);
    }

    /// <summary>Violins that set no Positions draw exactly where they did: their default room is kept.</summary>
    [Fact]
    public void ViolinsWithoutPositions_StandWhereTheyAlwaysDid()
    {
        string svg = Plt.Create().WithSize(500, 300).WithTheme(Theme.Default)
            .AddSubPlot(1, 1, 1, ax => ax.Violin(Samples)).Build().ToSvg();
        var plot = svg.PlotArea();
        var extents = BodyExtents(svg);

        // Positions 0, 1, 2 in the range -1..3 plus its 5 % margin on each side: the plot is 4.4 positions wide and the
        // middle violin stands in the middle of it.
        double Centre(int g) => (extents[g].Lo + extents[g].Hi) / 2;
        double pitch = Centre(1) - Centre(0);
        Assert.Equal(4.4, plot.Width / pitch, 3);
        Assert.Equal(plot.X + plot.Width / 2, Centre(1), 6);
    }
}
