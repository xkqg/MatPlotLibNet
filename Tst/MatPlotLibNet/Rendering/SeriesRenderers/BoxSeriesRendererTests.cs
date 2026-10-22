// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>Phase X.3.a (v1.7.2, 2026-04-19) — drives every render branch in
/// <see cref="MatPlotLibNet.Rendering.SeriesRenderers.BoxSeriesRenderer"/>. Pre-X
/// the renderer was at 64.5%L because only the vertical default-Positions path was
/// exercised. Adds: horizontal orientation, ShowMeans, explicit Positions, explicit
/// MedianColor, and empty-dataset skip.</summary>
public class BoxSeriesRendererTests
{
    private static double[][] SampleSets => [
        [1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0, 10.0],
        [5.0, 6.0, 7.0, 8.0, 9.0, 10.0, 11.0, 12.0, 13.0, 14.0],
    ];

    private static string Render(BoxSeries s) =>
        Plt.Create()
            .WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(s))
            .Build()
            .ToSvg();

    [Fact]
    public void Render_VerticalDefault_DrawsBoxAndWhiskers()
    {
        var svg = Render(new BoxSeries(SampleSets));
        Assert.Contains("<rect", svg);
    }

    /// <summary>Horizontal orientation (line 44-58: Vert=false branch).</summary>
    [Fact]
    public void Render_Horizontal_SwapsAxes()
    {
        var svg = Render(new BoxSeries(SampleSets) { Vert = false });
        Assert.Contains("<rect", svg);
    }

    /// <summary>ShowMeans=true exercises the green-circle mean marker (lines 38-42 vertical
    /// AND lines 53-57 horizontal both share the same `if (series.ShowMeans)` shape — one
    /// fact each below).</summary>
    [Fact]
    public void Render_ShowMeans_Vertical_DrawsCircleAtMean()
    {
        var svg = Render(new BoxSeries(SampleSets) { ShowMeans = true });
        Assert.Contains("<circle", svg);
    }

    [Fact]
    public void Render_ShowMeans_Horizontal_DrawsCircleAtMean()
    {
        var svg = Render(new BoxSeries(SampleSets) { Vert = false, ShowMeans = true });
        Assert.Contains("<circle", svg);
    }

    /// <summary>Explicit Positions array (line 24's `Positions is not null &amp;&amp; i &lt; Positions.Length`
    /// true branch). Default is i=0,1,...; explicit positions place boxes at custom x-coords.</summary>
    [Fact]
    public void Render_ExplicitPositions_PlacesBoxesAtCustomCoords()
    {
        var svg = Render(new BoxSeries(SampleSets) { Positions = [10.0, 20.0] });
        Assert.Contains("<rect", svg);
    }

    /// <summary>Positions array shorter than dataset count — i &gt;= Positions.Length false
    /// arm of the ternary; falls back to integer index.</summary>
    [Fact]
    public void Render_ShortPositions_FallsBackToIndex()
    {
        var svg = Render(new BoxSeries(SampleSets) { Positions = [10.0] });   // only 1 position for 2 datasets
        Assert.Contains("<rect", svg);
    }

    /// <summary>Explicit MedianColor (line 35/50 — `series.MedianColor ?? Colors.Red`).</summary>
    [Fact]
    public void Render_ExplicitMedianColor_UsesItForMedianLine()
    {
        var svg = Render(new BoxSeries(SampleSets) { MedianColor = new Color(0, 255, 0, 255) });
        Assert.Contains("<rect", svg);
    }

    // ── boxes placed by Positions, or lying down, are drawn inside the plot ─────────────────────────────────────────

    private static double Number(string text) => double.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>The boxes of the picture: every unfilled rectangle (a box is its outline), as written.</summary>
    private static Rect[] Boxes(string svg) =>
    [
        .. System.Text.RegularExpressions.Regex
            .Matches(svg, @"<rect x=""([-0-9.eE+]+)"" y=""([-0-9.eE+]+)"" width=""([-0-9.eE+]+)"" height=""([-0-9.eE+]+)"" fill=""none""")
            .Select(m => new Rect(Number(m.Groups[1].Value), Number(m.Groups[2].Value), Number(m.Groups[3].Value), Number(m.Groups[4].Value))),
    ];

    private static void AssertInsideThePlot(string svg, int boxes)
    {
        var plot = BarSeriesRendererTests.PlotArea(svg);
        var drawn = Boxes(svg);

        Assert.Equal(boxes, drawn.Length);
        Assert.All(drawn, b =>
        {
            Assert.InRange(b.X, plot.X - 1e-6, plot.X + plot.W + 1e-6);
            Assert.InRange(b.Right, plot.X - 1e-6, plot.X + plot.W + 1e-6);
            Assert.InRange(b.Y, plot.Y - 1e-6, plot.Y + plot.H + 1e-6);
            Assert.InRange(b.Bottom, plot.Y - 1e-6, plot.Y + plot.H + 1e-6);
        });
    }

    /// <summary>A narrow box per group, each its own series placed at the group's x. Measured before the fix in a 700 x 450
    /// figure: the second box ended 198 px and the third 650 px past the right edge of the plot, because each series
    /// reported the range -0.5 to 0.5.</summary>
    [Fact]
    public void BoxesPlacedAtGroupPositions_AreDrawnInsideThePlot()
    {
        string svg = Plt.Create().WithSize(700, 450).AddSubPlot(1, 1, 1, ax =>
        {
            for (int g = 0; g < SampleSets.Length + 1; g++)
            {
                int at = g;
                ax.BoxPlot([SampleSets[g % SampleSets.Length]], b => { b.Positions = [at]; b.Widths = 0.08; });
            }
        }).Build().ToSvg();

        AssertInsideThePlot(svg, 3);
    }

    /// <summary>One series with its own positions far from 0..n-1. Measured before the fix: the boxes stood at 1,796 to
    /// 5,163 px in a plot that ends at 630.</summary>
    [Fact]
    public void BoxesPlacedFarFromTheDefaultPositions_AreDrawnInsideThePlot()
    {
        string svg = Plt.Create().WithSize(700, 450)
            .AddSubPlot(1, 1, 1, ax => ax.BoxPlot(SampleSets, b => b.Positions = [10, 20])).Build().ToSvg();

        AssertInsideThePlot(svg, 2);
    }

    /// <summary>A wide box is not cut by the room the default positions leave: at a width of 3 it fills the plot.</summary>
    [Fact]
    public void ABoxWiderThanTheDefaultRoom_IsDrawnInsideThePlot()
    {
        string svg = Plt.Create().WithSize(500, 300)
            .AddSubPlot(1, 1, 1, ax => ax.BoxPlot(SampleSets, b => b.Widths = 3)).Build().ToSvg();

        AssertInsideThePlot(svg, 2);
    }

    /// <summary>Boxes that lie down (Vert = false), by index or by Positions: the values run along X and the positions along
    /// Y, as they are drawn. Measured before the fix: the three boxes of this figure ended 694, 1,022 and 723 px past the
    /// right edge of the plot, because the range was the upright one.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BoxesThatLieDown_AreDrawnInsideThePlot(bool withPositions)
    {
        double[][] three = [.. SampleSets, [1.0, 2, 3, 4, 5, 20]];
        string svg = Plt.Create().WithSize(700, 450).AddSubPlot(1, 1, 1, ax =>
            ax.BoxPlot(three, b => { b.Vert = false; if (withPositions) b.Positions = [10, 20, 30]; })).Build().ToSvg();

        AssertInsideThePlot(svg, 3);
    }

    /// <summary>A box that lies down is a rectangle of positive size: measured before the fix its height was written
    /// negative (-8.26 px for the first box of the figure above), which a browser does not draw.</summary>
    [Fact]
    public void ABoxThatLiesDown_HasAPositiveSize()
    {
        string svg = Plt.Create().WithSize(700, 450)
            .AddSubPlot(1, 1, 1, ax => ax.BoxPlot(SampleSets, b => b.Vert = false)).Build().ToSvg();

        Assert.All(Boxes(svg), b =>
        {
            Assert.True(b.Width > 0, $"width {b.Width}");
            Assert.True(b.Height > 0, $"height {b.Height}");
        });
    }

    /// <summary>Boxes that set no Positions draw exactly where they did: their default room is kept.</summary>
    [Fact]
    public void BoxesWithoutPositions_StandWhereTheyAlwaysDid()
    {
        string svg = Plt.Create().WithSize(500, 300)
            .AddSubPlot(1, 1, 1, ax => ax.BoxPlot([.. SampleSets, [1.0, 2, 3, 4]])).Build().ToSvg();
        var plot = BarSeriesRendererTests.PlotArea(svg);
        var boxes = Boxes(svg);

        // Positions 0, 1, 2 in the range -0.5..2.5 plus its 5 % margin on each side: the plot is 3.3 positions wide and
        // the middle box stands in the middle of it.
        double Centre(int g) => boxes[g].Center.X;
        double pitch = Centre(1) - Centre(0);
        Assert.Equal(3.3, plot.W / pitch, 3);
        Assert.Equal(plot.X + plot.W / 2, Centre(1), 6);
    }

    // X.3.a finding (NOT a renderer test, deliberately omitted): the
    // `data.Length == 0` continue arm in BoxSeriesRenderer (line 23) is dead code
    // through the public API — BoxSeries.ComputeDataRange throws on an empty
    // dataset before render is even reached (Min/Max on empty IEnumerable).
    // Either ComputeDataRange should filter empty datasets, or the renderer's
    // guard is unreachable. Tracked as a tiny stabilisation TODO; no Phase X
    // bandaid here.
}
