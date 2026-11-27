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

    /// <summary>The lines the spines drew, in the order they were drawn.</summary>
    private static List<(Point From, Point To)> SpineLines(Figure figure)
    {
        var recording = new RecordingRenderContext();
        new ChartRenderer().Render(figure, recording);
        var lines = new List<(Point, Point)>();
        for (int i = 0; i < recording.Calls.Count - 1; i++)
        {
            if (recording.Calls[i] is BeginGroupCall { CssClass: "spine" } && recording.Calls[i + 1] is DrawLineCall line)
                lines.Add((line.P1, line.P2));
        }

        return lines;
    }

    private static double PxX(Rect plot, double x) => plot.X + x / 10 * plot.Width;

    private static double PxY(Rect plot, double y) => plot.Y + plot.Height - y / 10 * plot.Height;

    [Fact]
    public void ASpineWithoutBounds_SpansThePlot()
    {
        var figure = Of(s => s);
        var plot = figure.FirstPlotArea();

        var bottom = SpineLines(figure).First(line => line.From.Y == line.To.Y && line.From.Y > plot.Y + 1);

        Assert.Equal(plot.X, bottom.From.X, 6);
        Assert.Equal(plot.X + plot.Width, bottom.To.X, 6);
    }

    [Fact]
    public void ABottomSpineWithBounds_SpansOnlyThoseDataValues()
    {
        var figure = Of(s => s with { Bottom = s.Bottom with { BoundsFrom = 2, BoundsTo = 8 } });
        var plot = figure.FirstPlotArea();

        var bottom = SpineLines(figure).First(line => line.From.Y == line.To.Y && line.From.Y > plot.Y + 1);

        Assert.Equal(PxX(plot, 2), bottom.From.X, 6);
        Assert.Equal(PxX(plot, 8), bottom.To.X, 6);
        Assert.Equal(plot.Y + plot.Height, bottom.From.Y, 6);
    }

    [Fact]
    public void ALeftSpineWithBounds_SpansThoseValuesOfTheYAxis()
    {
        var figure = Of(s => s with { Left = s.Left with { BoundsFrom = 3, BoundsTo = 7 } });
        var plot = figure.FirstPlotArea();

        var left = SpineLines(figure).First(line => line.From.X == line.To.X && line.From.X < plot.X + 1);

        Assert.Equal(Math.Min(PxY(plot, 3), PxY(plot, 7)), Math.Min(left.From.Y, left.To.Y), 6);
        Assert.Equal(Math.Max(PxY(plot, 3), PxY(plot, 7)), Math.Max(left.From.Y, left.To.Y), 6);
    }

    [Fact]
    public void OneBound_LeavesTheOtherAtTheEdge()
    {
        var figure = Of(s => s with { Bottom = s.Bottom with { BoundsFrom = 4 } });
        var plot = figure.FirstPlotArea();

        var bottom = SpineLines(figure).First(line => line.From.Y == line.To.Y && line.From.Y > plot.Y + 1);

        Assert.Equal(PxX(plot, 4), bottom.From.X, 6);
        Assert.Equal(plot.X + plot.Width, bottom.To.X, 6);
    }

    [Fact]
    public void BoundsGivenTheOtherWayRound_DrawTheSameLine()
    {
        var plot = Of(s => s).FirstPlotArea();
        var forward = SpineLines(Of(s => s with { Bottom = s.Bottom with { BoundsFrom = 2, BoundsTo = 8 } })).First(l => l.From.Y > plot.Y + 1 && l.From.Y == l.To.Y);
        var backward = SpineLines(Of(s => s with { Bottom = s.Bottom with { BoundsFrom = 8, BoundsTo = 2 } })).First(l => l.From.Y > plot.Y + 1 && l.From.Y == l.To.Y);

        Assert.Equal(Math.Min(forward.From.X, forward.To.X), Math.Min(backward.From.X, backward.To.X), 6);
        Assert.Equal(Math.Max(forward.From.X, forward.To.X), Math.Max(backward.From.X, backward.To.X), 6);
    }

    [Fact]
    public void OnABackwardsAxis_TheBoundsStillStandAtTheirDataValues()
    {
        var figure = Of(s => s with { Bottom = s.Bottom with { BoundsFrom = 2, BoundsTo = 8 } }, ax => ax.SetXLim(10, 0));
        var plot = figure.FirstPlotArea();

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
        var plot = figure.FirstPlotArea();

        var bottom = SpineLines(figure).First(line => line.From.Y == line.To.Y && line.From.Y > plot.Y + 1);

        Assert.Equal(plot.X + plot.Width / 2, bottom.From.X, 6);
        Assert.Equal(plot.X + plot.Width, bottom.To.X, 6);
    }

    [Fact]
    public void ASpineMovedToAData_StillSpansItsBounds()
    {
        var figure = Of(s => s with { Bottom = s.Bottom with { Position = SpinePosition.Data, PositionValue = 5, BoundsFrom = 2, BoundsTo = 8 } });
        var plot = figure.FirstPlotArea();

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

        var back = figure.RoundTripped();

        Assert.Equal(2, back.SubPlots[0].Spines.Bottom.BoundsFrom);
        Assert.Equal(8, back.SubPlots[0].Spines.Bottom.BoundsTo);
        Assert.Equal(figure.ToSvg(), back.ToSvg());
    }

    // ── RenderSpines, straight on a context ───────────────────────────────────────────────────────────────────────

    private static readonly Rect Area = new(100, 50, 400, 300);

    private static List<(Point From, Point To, MatPlotLibNet.Styling.Color Color)> Drawn(SpinesConfig spines)
    {
        var ctx = new RecordingRenderContext();
        ctx.RenderSpines(spines, new DataTransform(0, 10, 0, 10, Area), MatPlotLibNet.Styling.Theme.Default);
        return [.. ctx.Of<DrawLineCall>()
            .Select(a => (a.P1, a.P2, (MatPlotLibNet.Styling.Color)a.Color))];
    }

    [Fact]
    public void RenderSpines_DrawsTheFourEdges_BottomTopLeftRight_EachInItsOwnGroup()
    {
        var recording = new RecordingRenderContext();
        recording.RenderSpines(new SpinesConfig(), new DataTransform(0, 10, 0, 10, Area), MatPlotLibNet.Styling.Theme.Default);
        var lines = Drawn(new SpinesConfig());

        Assert.Equal(4, recording.Count<BeginGroupCall>());
        Assert.Equal(4, recording.Count<EndGroupCall>());
        Assert.Equal(
            [(new Point(100, 350), new Point(500, 350)), (new Point(100, 50), new Point(500, 50)),
             (new Point(100, 50), new Point(100, 350)), (new Point(500, 50), new Point(500, 350))],
            lines.Select(l => (l.From, l.To)));
    }

    [Theory]
    [InlineData("bottom")]
    [InlineData("top")]
    [InlineData("left")]
    [InlineData("right")]
    public void RenderSpines_LeavesOutASpineThatIsNotVisible(string hidden)
    {
        var off = new SpineConfig { Visible = false };
        var spines = hidden switch
        {
            "bottom" => new SpinesConfig { Bottom = off },
            "top" => new SpinesConfig { Top = off },
            "left" => new SpinesConfig { Left = off },
            _ => new SpinesConfig { Right = off },
        };

        Assert.Equal(3, Drawn(spines).Count);
    }

    [Fact]
    public void RenderSpines_DrawsASpineInItsOwnColour_ElseTheForeground()
    {
        var lines = Drawn(new SpinesConfig { Left = new SpineConfig { Color = MatPlotLibNet.Styling.Colors.Red } });

        Assert.Equal(MatPlotLibNet.Styling.Colors.Red, lines[2].Color);
        Assert.All(lines.Where((_, i) => i != 2), l => Assert.Equal(MatPlotLibNet.Styling.Theme.Default.ForegroundText, l.Color));
    }

    /// <summary>A left spine moved to the data value 5 of [0, 10], or to a quarter of the axes' width, over a plot from 100 to
    /// 500 px.</summary>
    [Theory]
    [InlineData(SpinePosition.Data, 5.0, 300.0)]
    [InlineData(SpinePosition.Axes, 0.25, 200.0)]
    public void RenderSpines_StandsAMovedLeftSpine_WhereItWasMoved(SpinePosition position, double value, double x)
    {
        var lines = Drawn(new SpinesConfig { Left = new SpineConfig { Position = position, PositionValue = value } });

        Assert.Equal(x, lines[2].From.X);
        Assert.Equal(x, lines[2].To.X);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("BoundsAtRest_AreLeftOutOfTheJson", () => Of(s => s).ToJson()) { Absent = ["bounds"], Comparison = StringComparison.OrdinalIgnoreCase },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    [Fact]
    public void ASpineWrittenWithoutAWidth_ReadsBackAsWideAsTheModelsSpine()
    {
        var json = """{"width":300,"height":200,"subPlots":[{"spines":{"bottom":{}},"series":[]}]}""";

        var figure = ChartSerializerDefault.Current.FromJson(json);

        Assert.Equal(new SpineConfig().LineWidth, figure.SubPlots[0].Spines.Bottom.LineWidth);
    }
}

