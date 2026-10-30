// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A spine is an axis line, and an axis line can span only the data's range (a range frame). Its bounds are data
/// units on its own axis, matplotlib's <c>set_bounds</c>; one owner resolves a spine's line, and the tick marks stand on the
/// line it resolves, so a spine moved to a data value or a fraction of the axes takes its ticks with it.</summary>
public class SpineBoundsTests
{
    private static Figure Of(Func<SpinesConfig, SpinesConfig> spines, Action<AxesBuilder>? more = null) => Plt.Create().WithSize(600, 400)
        .AddSubPlot(1, 1, 1, ax =>
        {
            ax.Plot([0.0, 10.0], [0.0, 10.0]).SetXLim(0, 10).SetYLim(0, 10).WithSpines(spines);
            more?.Invoke(ax);
        })
        .Build();

    private static Rect Plot(Figure figure) => new ChartRenderer().ComputeLayout(figure, new MatPlotLibNet.Rendering.Svg.SvgRenderContext()).PlotAreas[0];

    /// <summary>The lines the spines drew, in the order they were drawn.</summary>
    private static List<(Point From, Point To)> SpineLines(Figure figure)
    {
        var recording = new RecordingRenderContext();
        new ChartRenderer().Render(figure, recording);
        var lines = new List<(Point, Point)>();
        for (int i = 0; i < recording.Calls.Count - 1; i++)
        {
            if (recording.Calls[i].Kind != "BeginGroup" || recording.Calls[i + 1].Kind != "DrawLine") continue;
            string cssClass = ((dynamic)recording.Calls[i].Args!).cssClass;
            if (cssClass != "spine") continue;
            dynamic args = recording.Calls[i + 1].Args!;
            lines.Add(((Point)args.p1, (Point)args.p2));
        }

        return lines;
    }

    private static double PxX(Rect plot, double x) => plot.X + x / 10 * plot.Width;

    private static double PxY(Rect plot, double y) => plot.Y + plot.Height - y / 10 * plot.Height;

    [Fact]
    public void ASpineWithoutBounds_SpansThePlot()
    {
        var figure = Of(s => s);
        var plot = Plot(figure);

        var bottom = SpineLines(figure).First(line => line.From.Y == line.To.Y && line.From.Y > plot.Y + 1);

        Assert.Equal(plot.X, bottom.From.X, 6);
        Assert.Equal(plot.X + plot.Width, bottom.To.X, 6);
    }

    [Fact]
    public void ABottomSpineWithBounds_SpansOnlyThoseDataValues()
    {
        var figure = Of(s => s with { Bottom = s.Bottom with { BoundsFrom = 2, BoundsTo = 8 } });
        var plot = Plot(figure);

        var bottom = SpineLines(figure).First(line => line.From.Y == line.To.Y && line.From.Y > plot.Y + 1);

        Assert.Equal(PxX(plot, 2), bottom.From.X, 6);
        Assert.Equal(PxX(plot, 8), bottom.To.X, 6);
        Assert.Equal(plot.Y + plot.Height, bottom.From.Y, 6);
    }

    [Fact]
    public void ALeftSpineWithBounds_SpansThoseValuesOfTheYAxis()
    {
        var figure = Of(s => s with { Left = s.Left with { BoundsFrom = 3, BoundsTo = 7 } });
        var plot = Plot(figure);

        var left = SpineLines(figure).First(line => line.From.X == line.To.X && line.From.X < plot.X + 1);

        Assert.Equal(Math.Min(PxY(plot, 3), PxY(plot, 7)), Math.Min(left.From.Y, left.To.Y), 6);
        Assert.Equal(Math.Max(PxY(plot, 3), PxY(plot, 7)), Math.Max(left.From.Y, left.To.Y), 6);
    }

    [Fact]
    public void OneBound_LeavesTheOtherAtTheEdge()
    {
        var figure = Of(s => s with { Bottom = s.Bottom with { BoundsFrom = 4 } });
        var plot = Plot(figure);

        var bottom = SpineLines(figure).First(line => line.From.Y == line.To.Y && line.From.Y > plot.Y + 1);

        Assert.Equal(PxX(plot, 4), bottom.From.X, 6);
        Assert.Equal(plot.X + plot.Width, bottom.To.X, 6);
    }

    [Fact]
    public void BoundsGivenTheOtherWayRound_DrawTheSameLine()
    {
        var plot = Plot(Of(s => s));
        var forward = SpineLines(Of(s => s with { Bottom = s.Bottom with { BoundsFrom = 2, BoundsTo = 8 } })).First(l => l.From.Y > plot.Y + 1 && l.From.Y == l.To.Y);
        var backward = SpineLines(Of(s => s with { Bottom = s.Bottom with { BoundsFrom = 8, BoundsTo = 2 } })).First(l => l.From.Y > plot.Y + 1 && l.From.Y == l.To.Y);

        Assert.Equal(Math.Min(forward.From.X, forward.To.X), Math.Min(backward.From.X, backward.To.X), 6);
        Assert.Equal(Math.Max(forward.From.X, forward.To.X), Math.Max(backward.From.X, backward.To.X), 6);
    }

    [Fact]
    public void OnABackwardsAxis_TheBoundsStillStandAtTheirDataValues()
    {
        var figure = Of(s => s with { Bottom = s.Bottom with { BoundsFrom = 2, BoundsTo = 8 } }, ax => ax.SetXLim(10, 0));
        var plot = Plot(figure);

        var bottom = SpineLines(figure).First(line => line.From.Y == line.To.Y && line.From.Y > plot.Y + 1);

        Assert.Equal(plot.X + plot.Width - 2.0 / 10 * plot.Width, bottom.From.X, 6);
        Assert.Equal(plot.X + plot.Width - 8.0 / 10 * plot.Width, bottom.To.X, 6);
    }

    [Fact]
    public void OnALogAxis_TheBoundsArePlacedOnTheLogScale()
    {
        var figure = Plt.Create().WithSize(600, 400).AddSubPlot(1, 1, 1, ax => ax
            .Plot([1.0, 100.0], [1.0, 100.0]).SetXScale(AxisScale.Log).SetXLim(1, 100).SetYLim(0, 10)
            .WithSpines(s => s with { Bottom = s.Bottom with { BoundsFrom = 10, BoundsTo = 100 } })).Build();
        var plot = Plot(figure);

        var bottom = SpineLines(figure).First(line => line.From.Y == line.To.Y && line.From.Y > plot.Y + 1);

        Assert.Equal(plot.X + plot.Width / 2, bottom.From.X, 6);
        Assert.Equal(plot.X + plot.Width, bottom.To.X, 6);
    }

    [Fact]
    public void ASpineMovedToAData_StillSpansItsBounds()
    {
        var figure = Of(s => s with { Bottom = s.Bottom with { Position = SpinePosition.Data, PositionValue = 5, BoundsFrom = 2, BoundsTo = 8 } });
        var plot = Plot(figure);

        var moved = SpineLines(figure).First(line => line.From.Y == line.To.Y && Math.Abs(line.From.Y - PxY(plot, 5)) < 1e-6);

        Assert.Equal(PxX(plot, 2), moved.From.X, 6);
        Assert.Equal(PxX(plot, 8), moved.To.X, 6);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void ABoundThatIsNotAFiniteNumber_IsRefusedByName(double bound)
    {
        var from = Assert.Throws<ArgumentOutOfRangeException>(() => new SpineConfig { BoundsFrom = bound });
        var to = Assert.Throws<ArgumentOutOfRangeException>(() => new SpineConfig { BoundsTo = bound });

        Assert.Equal("BoundsFrom", from.ParamName);
        Assert.Equal("BoundsTo", to.ParamName);
    }

    [Fact]
    public void TheBounds_SurviveTheJson_AndDrawTheSamePicture()
    {
        var figure = Of(s => s with { Bottom = s.Bottom with { BoundsFrom = 2, BoundsTo = 8 } });

        var back = ChartSerializerDefault.Current.FromJson(figure.ToJson());

        Assert.Equal(2, back.SubPlots[0].Spines.Bottom.BoundsFrom);
        Assert.Equal(8, back.SubPlots[0].Spines.Bottom.BoundsTo);
        Assert.Equal(figure.ToSvg(), back.ToSvg());
    }

    [Fact]
    public void BoundsAtRest_AreLeftOutOfTheJson()
    {
        Assert.DoesNotContain("bounds", Of(s => s).ToJson(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ASpineWrittenWithoutAWidth_ReadsBackAsWideAsTheModelsSpine()
    {
        var json = """{"width":300,"height":200,"subPlots":[{"spines":{"bottom":{}},"series":[]}]}""";

        var figure = ChartSerializerDefault.Current.FromJson(json);

        Assert.Equal(new SpineConfig().LineWidth, figure.SubPlots[0].Spines.Bottom.LineWidth);
    }
}

/// <summary>Tick marks stand on the line of the spine they belong to: a spine moved to a data value or to a fraction of the
/// axes takes its ticks and their labels with it, as matplotlib's do.</summary>
public class TicksStandOnAMovedSpineTests
{
    private static Figure Moved(SpinePosition position, double value) => Plt.Create().WithSize(600, 400)
        .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 10.0], [0.0, 10.0]).SetXLim(0, 10).SetYLim(0, 10)
            .WithSpines(s => s with { Bottom = s.Bottom with { Position = position, PositionValue = value } }))
        .Build();

    private static List<(Point From, Point To)> VerticalMarks(Figure figure)
    {
        var recording = new RecordingRenderContext();
        new ChartRenderer().Render(figure, recording);
        var marks = new List<(Point From, Point To)>();
        foreach (var call in recording.OfKind("DrawLine"))
        {
            dynamic args = call.Args!;
            Point from = args.p1, to = args.p2;
            if (from.X == to.X && Math.Abs(to.Y - from.Y) is > 2 and < 12) marks.Add((from, to));
        }

        return marks;
    }

    private static Rect Plot(Figure figure) => new ChartRenderer().ComputeLayout(figure, new MatPlotLibNet.Rendering.Svg.SvgRenderContext()).PlotAreas[0];

    [Fact]
    public void WhenTheBottomSpineStandsAtADataValue_TheXTickMarksStandOnIt()
    {
        var figure = Moved(SpinePosition.Data, 5);
        var plot = Plot(figure);
        double spineY = plot.Y + plot.Height / 2;

        var marks = VerticalMarks(figure).Where(mark => mark.From.X > plot.X + 1 && mark.From.X < plot.X + plot.Width - 1).ToArray();

        Assert.NotEmpty(marks);
        Assert.Contains(marks, mark => Math.Abs(Math.Min(mark.From.Y, mark.To.Y) - spineY) < 1.5 || Math.Abs(Math.Max(mark.From.Y, mark.To.Y) - spineY) < 1.5);
        Assert.DoesNotContain(marks, mark => Math.Abs(Math.Max(mark.From.Y, mark.To.Y) - (plot.Y + plot.Height + 3.5)) < 0.6);
    }

    [Fact]
    public void WhenTheBottomSpineStandsAtAFractionOfTheAxes_TheXTickMarksStandOnIt()
    {
        var figure = Moved(SpinePosition.Axes, 0.25);
        var plot = Plot(figure);
        double spineY = plot.Y + 0.75 * plot.Height;

        var marks = VerticalMarks(figure).Where(mark => mark.From.X > plot.X + 1 && mark.From.X < plot.X + plot.Width - 1).ToArray();

        Assert.Contains(marks, mark => Math.Abs(Math.Min(mark.From.Y, mark.To.Y) - spineY) < 1.5 || Math.Abs(Math.Max(mark.From.Y, mark.To.Y) - spineY) < 1.5);
    }

    [Fact]
    public void WithTheSpineAtTheEdge_TheTicksAreWhereTheyAlwaysWere()
    {
        var edge = Moved(SpinePosition.Edge, 0);
        var plain = Plt.Create().WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 10.0], [0.0, 10.0]).SetXLim(0, 10).SetYLim(0, 10)).Build();

        Assert.Equal(plain.ToSvg(), edge.ToSvg());
    }
}
