// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;
using MatPlotLibNet.Tests.TestFixtures;
using MatPlotLibNet.Rendering.SeriesRenderers;

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

    private static string Render(BoxSeries s, Theme? theme = null) =>
        Plt.Create()
            .WithSize(400, 300)
            .WithTheme(theme ?? Theme.MatplotlibV2)
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(s))
            .Build()
            .ToSvg();

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("Render_VerticalDefault_DrawsBoxAndWhiskers", () => Render(new BoxSeries(SampleSets)), "<rect"),
        // Horizontal orientation (Vert=false branch).
        new DrawnToken("Render_Horizontal_SwapsAxes", () => Render(new BoxSeries(SampleSets) { Vert = false }), "<rect"),
        // ShowMeans=true exercises the green-circle mean marker the library's own themes draw (the matplotlib themes draw matplotlib's:
        // BoxLookTests); the vertical and the horizontal box each have a fact below.
        new DrawnToken("Render_ShowMeans_Vertical_DrawsCircleAtMean", () => Render(new BoxSeries(SampleSets) { ShowMeans = true }, Theme.Default), "<circle"),
        new DrawnToken("Render_ShowMeans_Horizontal_DrawsCircleAtMean", () => Render(new BoxSeries(SampleSets) { Vert = false, ShowMeans = true }, Theme.Default), "<circle"),
        // Explicit Positions array (`Positions is not null && i < Positions.Length` true branch). Default is i=0,1,...; explicit
        // positions place boxes at custom x-coords.
        new DrawnToken("Render_ExplicitPositions_PlacesBoxesAtCustomCoords", () => Render(new BoxSeries(SampleSets) { Positions = [10.0, 20.0] }), "<rect"),
        // Positions array shorter than dataset count — i >= Positions.Length false arm of the ternary; falls back to integer index.
        // only 1 position for 2 datasets
        new DrawnToken("Render_ShortPositions_FallsBackToIndex", () => Render(new BoxSeries(SampleSets) { Positions = [10.0] }), "<rect"),
        // Explicit MedianColor (`series.MedianColor ?? Colors.Red`).
        new DrawnToken("Render_ExplicitMedianColor_UsesItForMedianLine", () => Render(new BoxSeries(SampleSets) { MedianColor = new Color(0, 255, 0, 255) }), "<rect"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    // ── boxes placed by Positions, or lying down, are drawn inside the plot ─────────────────────────────────────────
    /// <summary>The boxes of the picture: every unfilled rectangle (a box is its outline), as written.</summary>
    private static Rect[] Boxes(string svg) =>
    [
        .. svg.RectsFilled("none"),
    ];

    private static void AssertInsideThePlot(string svg, int boxes)
    {
        var plot = svg.PlotArea();
        var drawn = Boxes(svg);

        Assert.Equal(boxes, drawn.Length);
        Assert.All(drawn, b =>
        {
            Assert.InRange(b.X, plot.X - 1e-6, plot.X + plot.Width + 1e-6);
            Assert.InRange(b.Right, plot.X - 1e-6, plot.X + plot.Width + 1e-6);
            Assert.InRange(b.Y, plot.Y - 1e-6, plot.Y + plot.Height + 1e-6);
            Assert.InRange(b.Bottom, plot.Y - 1e-6, plot.Y + plot.Height + 1e-6);
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

    /// <summary>Boxes that set no Positions draw exactly where they did under the library's own themes: their default room is kept (the
    /// matplotlib themes stand them at 1, 2, ..., <see cref="BoxPlacementTests"/>).</summary>
    [Fact]
    public void BoxesWithoutPositions_StandWhereTheyAlwaysDid()
    {
        string svg = Plt.Create().WithSize(500, 300).WithTheme(Theme.Default)
            .AddSubPlot(1, 1, 1, ax => ax.BoxPlot([.. SampleSets, [1.0, 2, 3, 4]])).Build().ToSvg();
        var plot = svg.PlotArea();
        var boxes = Boxes(svg);

        // Positions 0, 1, 2 in the range -0.5..2.5 plus its 5 % margin on each side: the plot is 3.3 positions wide and
        // the middle box stands in the middle of it.
        double Centre(int g) => boxes[g].Center.X;
        double pitch = Centre(1) - Centre(0);
        Assert.Equal(3.3, plot.Width / pitch, 3);
        Assert.Equal(plot.X + plot.Width / 2, Centre(1), SvgMarkupExtensions.WrittenPrecision);
    }

    /// <summary>A dataset with nothing to draw (no values, or only missing ones) leaves its place empty, and the range does not
    /// throw on it: the other boxes are drawn.</summary>
    [Fact]
    public void ADatasetWithNothingToDraw_LeavesItsPlaceEmpty_TheOthersAreDrawn()
    {
        string svg = Render(new BoxSeries([.. SampleSets, [], [double.NaN]]));

        Assert.Equal(SampleSets.Length, Boxes(svg).Length);
    }

    /// <summary>The marks a box leaves for a legend placed where it is clearest hold each outlier it draws (under a theme that marks them).</summary>
    [Fact]
    public void TheMarksOfABox_HoldItsOutliers()
    {
        var bounds = new Rect(0, 0, 100, 100);
        var ctx = new MatPlotLibNet.Rendering.Svg.SvgRenderContext();
        var context = new MatPlotLibNet.Rendering.SeriesRenderers.SeriesRenderContext(new DataTransform(0, 2, 0, 110, bounds), ctx, Colors.Tab10Blue, new RenderArea(bounds, ctx))
        {
            Theme = Theme.MatplotlibV2,
        };
        var marks = new MarkObstacles.Builder();

        new MatPlotLibNet.Rendering.SeriesRenderers.BoxSeriesRenderer(context).AddMarks(new BoxSeries([[1.0, 2, 2, 3, 3, 3, 4, 4, 5, 100]]), marks);

        Assert.Single(marks.Build().Points);
    }

    // ---- the statistics of a box (StatisticsOf) ----
    // The statistics of one box, as matplotlib's <c>cbook.boxplot_stats</c> works them out (matplotlib 3.11.2, each case
    // printed): the quartiles are <c>np.percentile</c>'s; a whisker reaches to the most extreme observation inside <c>whis</c>
    // times the interquartile range past its box, or stops at the box where no observation lies between the two; the observations
    // outside the whiskers are the fliers. The library's box ended each whisker at the fence itself (<c>q1 - whis * iqr</c>),
    // which is no observation, where <see cref="BoxSeries.Whis"/> says it stops at the most extreme observation inside that
    // reach.

    private static BoxSeriesRenderer.BoxStatistics Of(double whis, params double[] values)
    {
        Array.Sort(values);
        return BoxSeriesRenderer.StatisticsOf(values, whis);
    }

    [Fact]
    public void AnOutlierBeyondTheFence_IsAFlier_AndTheWhiskerStopsAtTheLastObservationInside()
    {
        var s = Of(1.5, 1, 2, 3, 4, 5, 100);   // q1 2.25, median 3.5, q3 4.75, fence 8.5

        Assert.Equal(2.25, s.Q1);
        Assert.Equal(3.5, s.Median);
        Assert.Equal(4.75, s.Q3);
        Assert.Equal(1, s.WhiskerLow);
        Assert.Equal(5, s.WhiskerHigh);
        Assert.Equal([100.0], s.Fliers);
    }

    [Fact]
    public void AnOutlierBelowTheFence_IsAFlier()
    {
        var s = Of(1.5, -50, 1, 2, 3, 4, 5);   // q1 1.25, q3 3.75, fence -1.25 .. 6.25

        Assert.Equal(1, s.WhiskerLow);
        Assert.Equal(5, s.WhiskerHigh);
        Assert.Equal([-50.0], s.Fliers);
    }

    [Fact]
    public void NoObservationBeyondTheFence_TheWhiskersReachTheExtremes_AndThereAreNoFliers()
    {
        var s = Of(1.5, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10);

        Assert.Equal(1, s.WhiskerLow);
        Assert.Equal(10, s.WhiskerHigh);
        Assert.Empty(s.Fliers);
    }

    [Fact]
    public void AReachOfNothing_StopsTheWhiskersAtTheBox_AndEveryObservationOutsideItIsAFlier()
    {
        var s = Of(0, 1, 2, 3, 4, 5, 100);   // q1 2.25, q3 4.75: nothing lies between the box and a fence at the box's edge

        Assert.Equal(2.25, s.WhiskerLow);
        Assert.Equal(4.75, s.WhiskerHigh);
        Assert.Equal([1.0, 2.0, 5.0, 100.0], s.Fliers);
    }

    [Fact]
    public void AReachWideEnoughForEverything_HasNoFliers()
    {
        var s = Of(100, 1, 2, 3, 4, 5, 100);

        Assert.Equal(1, s.WhiskerLow);
        Assert.Equal(100, s.WhiskerHigh);
        Assert.Empty(s.Fliers);
    }

    [Fact]
    public void AnOutlierOnEachSide_AtAReachOfOne()
    {
        var s = Of(1.0, 1, 2, 3, 4, 5, 100, -40, 7.5);   // q1 1.75, q3 5.625, fence -1.875 .. 9.5

        Assert.Equal(1.75, s.Q1);
        Assert.Equal(5.625, s.Q3);
        Assert.Equal(1, s.WhiskerLow);
        Assert.Equal(7.5, s.WhiskerHigh);
        Assert.Equal([-40.0, 100.0], s.Fliers);
    }

    [Fact]
    public void ConstantValues_HaveABoxOfNoHeight_AndNoFliers()
    {
        var s = Of(1.5, 2, 2, 2, 2);

        Assert.Equal(2, s.Q1);
        Assert.Equal(2, s.Q3);
        Assert.Equal(2, s.WhiskerLow);
        Assert.Equal(2, s.WhiskerHigh);
        Assert.Empty(s.Fliers);
    }

    [Fact]
    public void OneValue_IsEveryStatistic()
    {
        var s = Of(1.5, 5);

        Assert.Equal([5.0, 5.0, 5.0, 5.0, 5.0], new[] { s.Q1, s.Median, s.Q3, s.WhiskerLow, s.WhiskerHigh });
        Assert.Empty(s.Fliers);
    }

    [Fact]
    public void AReachThatIsNotANumber_HasNoWhiskersAndNoFliers()
    {
        var s = Of(double.NaN, 1, 2, 3, 4, 5, 100);

        Assert.True(double.IsNaN(s.WhiskerLow));
        Assert.True(double.IsNaN(s.WhiskerHigh));
        Assert.Empty(s.Fliers);
    }

    [Fact]
    public void AnInfiniteReach_ReachesTheExtremes()
    {
        var s = Of(double.PositiveInfinity, 1, 2, 3, 4, 5, 100);

        Assert.Equal(1, s.WhiskerLow);
        Assert.Equal(100, s.WhiskerHigh);
        Assert.Empty(s.Fliers);
    }
}
