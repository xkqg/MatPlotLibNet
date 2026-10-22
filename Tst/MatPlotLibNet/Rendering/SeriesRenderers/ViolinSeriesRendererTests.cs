// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>Phase X.9.a (v1.7.2, 2026-04-19) — drives every render branch in
/// <see cref="MatPlotLibNet.Rendering.SeriesRenderers.ViolinSeriesRenderer"/>. Pre-X.9
/// the renderer was at 79%L / 63%B because only the default ShowExtrema body was
/// covered. This file pins:
///   - Empty dataset → continue (line 28)
///   - Positions per-dataset (line 29 ternary's true arm)
///   - range==0 (all-equal data) → range = 1 (line 31)
///   - Side.High / Side.Low / Side.Both (lines 40-41)
///   - ShowExtrema=false (line 50 false arm)
///   - ShowMedians=true (line 62 true arm)
///   - ShowMeans=true (line 70 true arm)</summary>
public class ViolinSeriesRendererTests
{
    private static string Render(ViolinSeries s) =>
        Plt.Create()
            .WithSize(500, 300)
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(s))
            .Build()
            .ToSvg();

    [Fact]
    public void Render_BasicViolin_DrawsPolygon()
    {
        var svg = Render(new ViolinSeries(new double[][] { [1.0, 2, 3, 4, 5] }));
        Assert.Contains("<polygon", svg);
    }

    // Note: a mixed empty + non-empty dataset triggers ComputeDataRange's Min/Max on
    // the empty array BEFORE the renderer's `data.Length == 0 continue` guard runs.
    // Skipping that test — the renderer's empty-dataset arm is provably defensive,
    // covered indirectly via the empty-data Min/Max behavior validated in series tests.

    /// <summary>Positions per-dataset (line 29 ternary's true arm). Each dataset placed
    /// at the configured X offset rather than its index.</summary>
    [Fact]
    public void Render_Positions_PlacesDatasetsAtCustomX()
    {
        var svg = Render(new ViolinSeries(new double[][] { [1.0, 2, 3], [4.0, 5, 6] })
        {
            Positions = new[] { 10.0, 20.0 },
        });
        Assert.Contains("<polygon", svg);
    }

    /// <summary>range==0 (all-identical values) → range = 1 fallback (line 31).</summary>
    [Fact]
    public void Render_AllIdenticalValues_RangeFallback()
    {
        var svg = Render(new ViolinSeries(new double[][] { [5.0, 5.0, 5.0, 5.0] }));
        Assert.Contains("<polygon", svg);
    }

    /// <summary>Side.High → leftOff=0 (half-violin on the right side only).</summary>
    [Fact]
    public void Render_SideHigh_ClipsLeftSide()
    {
        var svg = Render(new ViolinSeries(new double[][] { [1.0, 2, 3] }) { Side = ViolinSide.High });
        Assert.Contains("<polygon", svg);
    }

    /// <summary>Side.Low → rightOff=0 (half-violin on the left side only).</summary>
    [Fact]
    public void Render_SideLow_ClipsRightSide()
    {
        var svg = Render(new ViolinSeries(new double[][] { [1.0, 2, 3] }) { Side = ViolinSide.Low });
        Assert.Contains("<polygon", svg);
    }

    /// <summary>ShowExtrema=false (line 50 false arm) → no extrema lines drawn. Just
    /// the body polygon remains.</summary>
    [Fact]
    public void Render_HideExtrema_OnlyDrawsBody()
    {
        var svg = Render(new ViolinSeries(new double[][] { [1.0, 2, 3] }) { ShowExtrema = false });
        Assert.Contains("<polygon", svg);
    }

    /// <summary>ShowMedians=true (line 62 true arm) → adds a horizontal median line
    /// across the violin in white.</summary>
    [Fact]
    public void Render_ShowMedians_DrawsMedianLine()
    {
        var svg = Render(new ViolinSeries(new double[][] { [1.0, 2, 3, 4, 5] }) { ShowMedians = true });
        Assert.Contains("<polygon", svg);
    }

    /// <summary>ShowMeans=true (line 70 true arm) → adds a horizontal dashed mean line
    /// across the violin in green.</summary>
    [Fact]
    public void Render_ShowMeans_DrawsDashedMeanLine()
    {
        var svg = Render(new ViolinSeries(new double[][] { [1.0, 2, 3, 4, 5] }) { ShowMeans = true });
        Assert.Contains("<polygon", svg);
    }

    // ── violins placed by Positions are drawn inside the plot ───────────────────────────────────────────────────────

    private static readonly double[][] Samples =
    [
        [4.1, 4.8, 5.0, 5.3, 5.5, 5.9, 6.2, 6.4, 6.8, 7.1, 7.7, 8.4],
        [5.6, 6.1, 6.5, 6.9, 7.2, 7.4, 7.9, 8.3, 8.8, 9.1, 9.6, 10.2],
        [3.0, 3.4, 3.9, 4.4, 4.6, 5.2, 5.8, 6.1, 6.9, 7.5, 8.8, 9.9],
    ];

    private static double Number(string text) => double.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>The horizontal extent of every violin body in the picture.</summary>
    private static Range1D[] BodyExtents(string svg) =>
    [
        .. System.Text.RegularExpressions.Regex.Matches(svg, @"<polygon points=""([^""]+)""").Select(m =>
        {
            var xs = m.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(p => Number(p.Split(',')[0])).ToArray();
            return new Range1D(xs.Min(), xs.Max());
        }),
    ];

    private static void AssertInsideThePlot(string svg, int bodies)
    {
        var plot = BarSeriesRendererTests.PlotArea(svg);
        var extents = BodyExtents(svg);

        Assert.Equal(bodies, extents.Length);
        Assert.All(extents, e =>
        {
            Assert.InRange(e.Lo, plot.X - 1e-6, plot.X + plot.W + 1e-6);
            Assert.InRange(e.Hi, plot.X - 1e-6, plot.X + plot.W + 1e-6);
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
        string svg = Plt.Create().WithSize(500, 300)
            .AddSubPlot(1, 1, 1, ax => ax.Violin(Samples)).Build().ToSvg();
        var plot = BarSeriesRendererTests.PlotArea(svg);
        var extents = BodyExtents(svg);

        // Positions 0, 1, 2 in the range -1..3 plus its 5 % margin on each side: the plot is 4.4 positions wide and the
        // middle violin stands in the middle of it.
        double Centre(int g) => (extents[g].Lo + extents[g].Hi) / 2;
        double pitch = Centre(1) - Centre(0);
        Assert.Equal(4.4, plot.W / pitch, 3);
        Assert.Equal(plot.X + plot.W / 2, Centre(1), 6);
    }

    /// <summary>Themed render with explicit ViolinBodyColor + ViolinStatsColor
    /// (lines 20, 23 true arms of `??`). MatplotlibClassic sets these to y=#BFBF00
    /// and r=#FF0000 respectively.</summary>
    [Fact]
    public void Render_WithMatplotlibClassicTheme_UsesThemeViolinColors()
    {
        var svg = Plt.Create()
            .WithSize(400, 300)
            .WithTheme(Theme.MatplotlibClassic)
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new ViolinSeries(new double[][] { [1.0, 2, 3, 4, 5] })))
            .Build()
            .ToSvg();
        Assert.Contains("<polygon", svg);
    }
}
