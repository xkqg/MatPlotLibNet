// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.LegendRendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.TestFixtures;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies legend rendering behavior.</summary>
public class LegendRenderingTests
{
    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // Verifies that labeled series produce a legend box in SVG output.
        new DrawnToken("Legend_WithLabeledSeries_ContainsLegendText", () => Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Temperature")
            .Plot([1.0, 2.0], [5.0, 6.0], s => s.Label = "Humidity")
            .ToSvg(), "Temperature", "Humidity"),
        // Verifies that series without labels do not produce legend entries.
        // No labeled series → no legend rectangle
        // The SVG should not contain a legend group
        new DrawnToken("Legend_NoLabels_SkipsLegend", () => Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg()) { Absent = ["class=\"legend\""] },
        // Verifies that a hidden legend produces no legend output.
        new DrawnToken("Legend_NotVisible_SkipsLegend", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Data")
                .WithLegend(visible: false))
            .ToSvg()) { Absent = ["class=\"legend\""] },
        // Verifies that the legend contains color swatches as rect elements.
        new DrawnToken("Legend_ContainsColorSwatches", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Series A")
                .WithLegend())
            .ToSvg(), "class=\"legend\"", "Series A"),
        // Verifies that only labeled series appear in the legend.
        new DrawnToken("Legend_MixedLabeledAndUnlabeled_OnlyShowsLabeled", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Visible")
                .Plot([1.0, 2.0], [5.0, 6.0])
                .WithLegend())
            .ToSvg(), "Visible", "class=\"legend\""),
        // Verifies that multiple labeled series all appear in the legend.
        new DrawnToken("Legend_MultipleSeries_AllLabelsPresent", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Alpha")
                .Plot([1.0, 2.0], [5.0, 6.0], s => s.Label = "Beta")
                .Scatter([1.0, 2.0], [7.0, 8.0], s => s.Label = "Gamma")
                .WithLegend())
            .ToSvg(), "Alpha", "Beta", "Gamma"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>Verifies that legend position can be set via AxesBuilder.</summary>
    [Fact]
    public void WithLegend_SetsPosition()
    {
        var builder = new FigureBuilder();
        var figure = builder
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Test")
                .WithLegend(LegendPosition.LowerLeft))
            .Build();

        Assert.Equal(LegendPosition.LowerLeft, figure.SubPlots[0].Legend.Position);
    }

    /// <summary>Verifies that WithLegend defaults to Best position when called without arguments.</summary>
    [Fact]
    public void WithLegend_DefaultPosition_IsBest()
    {
        var builder = new FigureBuilder();
        var figure = builder
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Test")
                .WithLegend())
            .Build();

        Assert.Equal(LegendPosition.Best, figure.SubPlots[0].Legend.Position);
    }

    // ---- Best: where the marks are -----------------------------------------------------------------------------

    private static readonly double[] Ramp = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

    /// <summary>How far a legend stands inside the corner of the axes under the default style, the figure's own theme:
    /// <c>legend.borderaxespad</c>, 0.5 of the legend's 10 pt font.</summary>
    private static readonly double Inset = 0.5 * 10.0.PointsInPixels;

    private static FigureBuilder BestCase(string name) => name switch
    {
        "a rising line" => Plt.Create().WithSize(500, 350).AddSubPlot(1, 1, 1, ax => ax
            .Plot(Ramp, Ramp, s => s.Label = "signal").WithLegend()),
        "a falling line" => Plt.Create().WithSize(500, 350).AddSubPlot(1, 1, 1, ax => ax
            .Plot(Ramp, Ramp.Select(v => 10 - v).ToArray(), s => s.Label = "signal").WithLegend()),
        // Both ends stand on the plot's edges: no vertex of the line is inside any place, the segment crosses two.
        "a threshold line near the top" => Plt.Create().WithSize(500, 350).AddSubPlot(1, 1, 1, ax => ax
            .Plot([0.0, 10.0], [10.0, 10.0], s => s.Label = "limit").SetYLim(0, 11).WithLegend()),
        "a tall bar on the right" => Plt.Create().WithSize(500, 350).AddSubPlot(1, 1, 1, ax => ax
            .Bar(["a", "b", "c"], [1.0, 2.0, 3.0], s => s.Label = "sales").WithLegend()),
        "points in the upper right corner" => Plt.Create().WithSize(500, 350).AddSubPlot(1, 1, 1, ax => ax
            .Scatter([9.0, 9.5, 10.0, 9.2], [9.0, 9.6, 10.0, 9.8], s => s.Label = "cluster").SetXLim(0, 10).SetYLim(0, 10).WithLegend()),
        "a line on a right axis" => Plt.Create().WithSize(500, 350).AddSubPlot(1, 1, 1, ax => ax
            .Plot(Ramp, Ramp.Select(v => 10 - v).ToArray(), s => s.Label = "left")
            .WithAddRightYAxis(y2 => y2.Plot(Ramp, Ramp, s => s.Label = "right")).WithLegend()),
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, null),
    };

    private static Rect PlotAreaOf(string svg) =>
        svg.Elements("rect").Where(rect => rect["fill"] == "#FFFFFF").Skip(1)
            .Select(rect => new Rect(rect.Number("x"), rect.Number("y"), rect.Number("width"), rect.Number("height"))).First();

    /// <summary>A Best legend stands where the marks the chart drew are fewest: the first of matplotlib's places that
    /// nothing is under, the upper right when it is free.</summary>
    [Theory]
    [InlineData("a rising line", LegendPosition.UpperLeft)]
    [InlineData("a falling line", LegendPosition.UpperRight)]
    [InlineData("a threshold line near the top", LegendPosition.LowerLeft)]
    [InlineData("a tall bar on the right", LegendPosition.UpperLeft)]
    [InlineData("points in the upper right corner", LegendPosition.UpperLeft)]
    [InlineData("a line on a right axis", LegendPosition.Right)]   // the right axis' line fills the upper right, the primary's the other three corners
    public void ABestLegend_StandsWhereTheFewestMarksAreCovered(string name, LegendPosition expected)
    {
        string svg = BestCase(name).ToSvg();
        var frame = svg.LegendFrame()!.Value;

        var place = LegendPositionStrategyFactory.Create(expected).Place(PlotAreaOf(svg), new Size(frame.Width, frame.Height), Inset);

        Assert.Equal(place.X, frame.X, SvgMarkupExtensions.WrittenPrecision);
        Assert.Equal(place.Y, frame.Y, SvgMarkupExtensions.WrittenPrecision);
    }

    /// <summary>The first place, in the order a Best legend tries them, that <paramref name="svg"/>'s legend stands at (Right and
    /// CenterRight are the same box).</summary>
    private static LegendPosition PlaceOf(string svg, double inset)
    {
        var frame = svg.LegendFrame()!.Value;
        var plot = PlotAreaOf(svg);
        LegendPosition[] tried =
        [
            LegendPosition.UpperRight, LegendPosition.UpperLeft, LegendPosition.LowerLeft, LegendPosition.LowerRight,
            LegendPosition.Right, LegendPosition.CenterLeft, LegendPosition.CenterRight, LegendPosition.LowerCenter,
            LegendPosition.UpperCenter, LegendPosition.Center,
        ];
        return tried.First(p =>
            {
                var place = LegendPositionStrategyFactory.Create(p).Place(plot, new Size(frame.Width, frame.Height), inset);
                return Math.Abs(place.X - frame.X) <= SvgMarkupExtensions.WrittenPrecision && Math.Abs(place.Y - frame.Y) <= SvgMarkupExtensions.WrittenPrecision;
            });
    }

    /// <summary>One panel of a live wall: three seeded noisy lines of 360 points between <paramref name="low"/> and
    /// <paramref name="high"/> on an axis from 0 to 5000, under a wide legend of four columns, at Best. The X axis is held
    /// at -17.95 to 400, the range the places below were pinned on (the margin, widened to the nice bound 400), so the pins
    /// stay about the scoring and not about where the default theme ends an axis.</summary>
    private static FigureBuilder NoisyPanel(int seed, double low, double high) => Plt.Create().WithSize(1000, 190)
        .AddSubPlot(1, 1, 1, ax =>
        {
            var random = new Random(seed);
            double[] x = [.. Enumerable.Range(0, 360).Select(i => (double)i)];
            for (int line = 0; line < 3; line++)
                ax.Plot(x, [.. x.Select(_ => low + random.NextDouble() * (high - low))], s => s.Label = $"queue {line + 1}");
            ax.SetXLim(-17.95, 400).SetYLim(0, 5000).WithLegend(l => l with { Position = LegendPosition.Best, FontSize = 9, MarkerScale = 0.55, NCols = 4 });
        });

    /// <summary>Where a Best legend stands over noisy lines, pinned as it stood before the scoring was made cheaper: lines over
    /// the whole height cover every place, so every candidate is scored and the least covered wins; lines over a band leave
    /// places free, and the first free one wins. Seeds 2, 3, 9, 13 and 14 were pinned again when the box took matplotlib's padding and
    /// inset under the default style: a box of another size and place covers other marks, and over lines that fill the height the
    /// least covered of the right-hand corners is a near tie.</summary>
    [Theory]
    [InlineData(1, 0, 5000, LegendPosition.UpperRight)]
    [InlineData(2, 0, 5000, LegendPosition.UpperRight)]
    [InlineData(3, 0, 5000, LegendPosition.LowerRight)]
    [InlineData(4, 0, 5000, LegendPosition.UpperRight)]
    [InlineData(8, 0, 5000, LegendPosition.UpperRight)]
    [InlineData(9, 0, 5000, LegendPosition.UpperRight)]
    [InlineData(10, 0, 5000, LegendPosition.UpperRight)]
    [InlineData(11, 0, 5000, LegendPosition.LowerRight)]
    [InlineData(12, 0, 5000, LegendPosition.UpperRight)]
    [InlineData(13, 0, 5000, LegendPosition.UpperRight)]
    [InlineData(14, 0, 5000, LegendPosition.UpperRight)]
    [InlineData(5, 2500, 5000, LegendPosition.LowerLeft)]
    [InlineData(15, 4200, 5000, LegendPosition.LowerLeft)]
    [InlineData(16, 0, 4600, LegendPosition.UpperRight)]
    [InlineData(6, 0, 2500, LegendPosition.UpperRight)]
    [InlineData(7, 1500, 3500, LegendPosition.UpperRight)]
    public void ABestLegend_OverNoisyLines_StandsWhereItAlwaysStood(int seed, double low, double high, LegendPosition expected)
    {
        Assert.Equal(expected, PlaceOf(NoisyPanel(seed, low, high).ToSvg(), inset: 0.5 * 9));   // borderaxespad: half the 9 px legend font
    }

    /// <summary>The picture the roadmap's own example draws: a rising line y = x in a 500 x 350 figure. The legend that stood
    /// over a stretch of the line in the upper right now stands in the upper left, clear of it.</summary>
    [Fact]
    public void ABestLegend_OverARisingLine_StandsInTheUpperLeft_HalfItsFontInsideTheCorner()
    {
        string svg = BestCase("a rising line").ToSvg();
        var frame = svg.LegendFrame()!.Value;
        var plot = PlotAreaOf(svg);

        Assert.Equal(plot.X + Inset, frame.X, SvgMarkupExtensions.WrittenPrecision);
        Assert.Equal(plot.Y + Inset, frame.Y, SvgMarkupExtensions.WrittenPrecision);
        Assert.Equal(88.6, frame.Width, 1);    // 'signal' and 3.6 of the 13.89 px font (2 x borderpad, handlelength, handletextpad)
        Assert.Equal(27.8, frame.Height, 1);   // 2 x borderpad and a row as tall as the text's line
    }

    /// <summary>Hiding a series (the legend's own toggle) does not move the legend from under the pointer: a hidden series
    /// still counts among the marks.</summary>
    [Fact]
    public void ALegendToggle_DoesNotMoveABestLegend()
    {
        var figure = Plt.Create().WithSize(500, 350).AddSubPlot(1, 1, 1, ax => ax
            .Plot(Ramp, Ramp, s => s.Label = "up")
            .Plot(Ramp, Ramp.Select(v => 10 - v).ToArray(), s => s.Label = "down").WithLegend()).Build();
        var shown = figure.ToSvg().LegendFrame()!.Value;

        figure.SubPlots[0].Series[0].Visible = false;
        var hidden = figure.ToSvg().LegendFrame()!.Value;

        Assert.Equal(shown.X, hidden.X, 6);
        Assert.Equal(shown.Y, hidden.Y, 6);
    }

    /// <summary>The chart without a mark under any place keeps the upper right, exactly as before.</summary>
    [Fact]
    public void ABestLegend_OverNothingInTheWay_StaysInTheUpperRight()
    {
        string svg = Plt.Create().WithSize(500, 350).AddSubPlot(1, 1, 1, ax => ax
            .Plot([0.0, 1.0], [5.0, 5.0], s => s.Label = "flat").SetXLim(0, 1).SetYLim(0, 10).WithLegend()).ToSvg();
        var frame = svg.LegendFrame()!.Value;
        var place = LegendPositionStrategyFactory.Create(LegendPosition.UpperRight).Place(PlotAreaOf(svg), new Size(frame.Width, frame.Height), Inset);

        Assert.Equal(place.X, frame.X, SvgMarkupExtensions.WrittenPrecision);
        Assert.Equal(place.Y, frame.Y, SvgMarkupExtensions.WrittenPrecision);
    }

    /// <summary>An annotation's text stands among the marks a Best legend keeps clear of, where it is drawn: a flat line
    /// leaves every place free, so the legend stays in the upper right, until a text stands there.</summary>
    [Fact]
    public void ALegend_AvoidsAnAnnotationAtItsAnchor()
    {
        FigureBuilder Chart(bool annotated) => Plt.Create().WithSize(500, 350).AddSubPlot(1, 1, 1, ax =>
        {
            ax.Plot([0.0, 1.0], [1.0, 1.0], s => s.Label = "flat").SetXLim(0, 1).SetYLim(0, 10).WithLegend();
            if (annotated) ax.Annotate("a long note standing where the legend would", 0.55, 9.3);
        });

        var free = Chart(annotated: false).ToSvg().LegendFrame()!.Value;
        var noted = Chart(annotated: true).ToSvg().LegendFrame()!.Value;

        Assert.True(noted.X < free.X, $"the legend stood at x {free.X} with nothing in its way and at x {noted.X} with a note there");
    }

    /// <summary>The polar marks a Best legend keeps clear of: a line along the top of the circle, under a legend wide enough to
    /// reach it from the upper right, sends the legend to a place the line does not cross.</summary>
    [Fact]
    public void ABestLegend_OnPolarAxes_StandsClearOfAPolarLine()
    {
        string svg = Plt.Create().WithSize(500, 400).AddSubPlot(1, 1, 1, ax => ax
            .PolarPlot([1.0, 1.0, 1.0, 1.0, 1.0], [0.8, 1.2, 1.57, 1.9, 2.3], s => s.Label = "an arc along the top of the circle and a long name")
            .WithLegend()).ToSvg();
        var frame = svg.LegendFrame()!.Value;

        var under = svg.PolylinePoints()
            .Where(p => p.X > frame.X && p.X < frame.Right && p.Y > frame.Y && p.Y < frame.Bottom).ToList();

        Assert.Empty(under);
    }
}
