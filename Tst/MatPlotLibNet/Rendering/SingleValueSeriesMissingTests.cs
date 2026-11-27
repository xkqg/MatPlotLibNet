// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using MatPlotLibNet.Builders;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>
/// A series that shows one value (a gauge, a progress bar, a bullet graph, a stat tile) draws nothing for a value it cannot
/// place: a missing value (NaN) or an infinity. matplotlib draws nothing for NaN, and no render context is handed a
/// coordinate it cannot draw (<see cref="MissingValueGeometryTests"/>). Before, <c>Gauge(double.NaN)</c> wrote a needle
/// <c>x2="NaN" y2="NaN"</c> and the text <c>NaN</c>, a progress bar a bar of width NaN and the text <c>NaN%</c>, a bullet graph a
/// bar of NaN and an axis that spans NaN.
/// </summary>
public class SingleValueSeriesMissingTests
{
    private static readonly Rect Bounds = new(0, 0, 400, 300);

    private static RecordingRenderContext Render(Action<SeriesRenderContext> draw)
    {
        var ctx = new RecordingRenderContext();
        draw(new SeriesRenderContext(new DataTransform(0, 10, 0, 10, Bounds), ctx, Colors.Tab10Blue, new RenderArea(Bounds, ctx)));
        return ctx;
    }

    private static RecordingRenderContext Gauge(GaugeSeries series) =>
        Render(c => new GaugeSeriesRenderer(c).Render(series));

    private static string Svg(Action<AxesBuilder> draw) =>
        Plt.Create().WithSize(600, 450).AddSubPlot(1, 1, 1, draw).ToSvg();

    // ── gauge ───────────────────────────────────────────────────────────────────────────────────────────────

    public static TheoryData<double> Unplaceable => [double.NaN, double.PositiveInfinity, double.NegativeInfinity];

    [Theory]
    [MemberData(nameof(Unplaceable))]
    public void AGaugeWithAValueItCannotPlace_DrawsItsBands_ButNoNeedleHubOrNumber(double value)
    {
        var withValue = Gauge(new GaugeSeries(37));
        var ctx = Gauge(new GaugeSeries(value));

        ctx.AssertNothingUndrawableReached();
        Assert.Equal(withValue.Count<DrawPolygonCall>(), ctx.Count<DrawPolygonCall>());
        Assert.Equal(0, ctx.Count<DrawLineCall>());
        Assert.Equal(0, ctx.Count<DrawCircleCall>());
        Assert.Equal(0, ctx.Count<DrawTextCall>());
    }

    [Fact]
    public void AGaugeWithAValueItCanPlace_StillDrawsNeedleHubAndNumber()
    {
        var ctx = Gauge(new GaugeSeries(37));

        Assert.Equal(1, ctx.Count<DrawLineCall>());
        Assert.Equal(1, ctx.Count<DrawCircleCall>());
        var text = Assert.Single(ctx.Of<DrawTextCall>());
        Assert.Equal("37", text.Text);
    }

    [Theory]
    [InlineData(double.NaN, 100)]
    [InlineData(0, double.NaN)]
    [InlineData(double.NegativeInfinity, 100)]
    [InlineData(0, double.PositiveInfinity)]
    public void AGaugeWhoseScaleCannotBePlaced_DrawsNothing(double min, double max)
    {
        var ctx = Gauge(new GaugeSeries(37) { Min = min, Max = max });

        ctx.AssertNothingUndrawableReached();
        Assert.Empty(ctx.Calls);
    }

    [Fact]
    public void AGaugeBandWhoseThresholdCannotBePlaced_IsSkipped_TheOthersAreDrawnAsWithoutIt()
    {
        GaugeBand[] with = [new(30, Colors.Green), new(double.NaN, Colors.Amber), new(100, Colors.Red)];
        GaugeBand[] without = [new(30, Colors.Green), new(100, Colors.Red)];

        var withGap = Gauge(new GaugeSeries(37) { Ranges = with });
        var withoutGap = Gauge(new GaugeSeries(37) { Ranges = without });

        withGap.AssertNothingUndrawableReached();
        Assert.Equal(withoutGap.Count<DrawPolygonCall>(), withGap.Count<DrawPolygonCall>());
    }

    public static TheoryData<string, Action<AxesBuilder>> ValuesItCannotPlace => new()
    {
        { "gauge", ax => ax.Gauge(double.NaN) },
        { "progress bar", ax => ax.ProgressBar(double.NaN) },
        { "bullet, whose axis is a finite one", ax => ax.Bullet(double.NaN) },
        { "bullet target", ax => ax.Bullet(60).Bullet(60, s => s.Target = double.NaN) },
        { "sparkline", ax => ax.Sparkline([1.0, double.NaN, 3.0, 2.0, double.PositiveInfinity, 4.0]) },
    };

    /// <summary>A value a single-value series cannot place writes no NaN or infinity into the picture.</summary>
    [Theory]
    [MemberData(nameof(ValuesItCannotPlace))]
    public void ASeriesOfAValueItCannotPlace_WritesNoNaNIntoThePicture(string series, Action<AxesBuilder> draw)
    {
        Svg(draw).AssertNoNonFiniteNumber("the picture");
        _ = series;
    }

    // ── progress bar ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A progress bar clamps what it is given to 0..1 when it is made, so an infinity is a full or an empty bar; only a
    /// missing value has no bar.</summary>
    [Fact]
    public void AProgressBarWithAMissingValue_DrawsItsTrack_ButNoFillOrPercentage()
    {
        var ctx = Render(c => new ProgressBarSeriesRenderer(c).Render(new ProgressBarSeries(double.NaN)));

        ctx.AssertNothingUndrawableReached();
        Assert.Equal(1, ctx.Count<DrawRectangleCall>());
        Assert.Equal(0, ctx.Count<DrawTextCall>());
    }

    [Fact]
    public void AProgressBarWithAValue_DrawsTrackFillAndPercentage()
    {
        var ctx = Render(c => new ProgressBarSeriesRenderer(c).Render(new ProgressBarSeries(0.37)));

        Assert.Equal(2, ctx.Count<DrawRectangleCall>());
        Assert.Equal(1, ctx.Count<DrawTextCall>());
    }

    // ── bullet graph ────────────────────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(double.NaN, Orientation.Horizontal)]
    [InlineData(double.NaN, Orientation.Vertical)]
    [InlineData(double.PositiveInfinity, Orientation.Horizontal)]
    [InlineData(double.PositiveInfinity, Orientation.Vertical)]
    public void ABulletWithAValueItCannotPlace_DrawsItsBands_ButNoBar(double value, Orientation orientation)
    {
        GaugeBand[] bands = [new(50, Colors.LightGray), new(100, Colors.Gray)];
        var ctx = Render(c => new BulletGraphSeriesRenderer(c).Render(new BulletGraphSeries(value) { Bands = bands, Orientation = orientation }));

        ctx.AssertNothingUndrawableReached();
        Assert.Equal(2, ctx.Count<DrawRectangleCall>());
    }

    [Theory]
    [InlineData(Orientation.Horizontal)]
    [InlineData(Orientation.Vertical)]
    public void ABulletWithATargetItCannotPlace_DrawsNoTargetTick(Orientation orientation)
    {
        var ctx = Render(c => new BulletGraphSeriesRenderer(c).Render(new BulletGraphSeries(60) { Target = double.NaN, Orientation = orientation }));

        ctx.AssertNothingUndrawableReached();
        Assert.Equal(0, ctx.Count<DrawLineCall>());
        Assert.Equal(1, ctx.Count<DrawRectangleCall>());
    }

    [Theory]
    [InlineData(Orientation.Horizontal)]
    [InlineData(Orientation.Vertical)]
    public void ABulletBandWhoseThresholdCannotBePlaced_IsSkipped(Orientation orientation)
    {
        GaugeBand[] bands = [new(30, Colors.LightGray), new(double.NaN, Colors.Gray), new(100, Colors.DarkGray)];
        var ctx = Render(c => new BulletGraphSeriesRenderer(c).Render(new BulletGraphSeries(60) { Bands = bands, Orientation = orientation }));

        ctx.AssertNothingUndrawableReached();
    }

    // ── stat tile ───────────────────────────────────────────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(Unplaceable))]
    public void AStatTileWithAValueItCannotPlace_WritesNoNumber(double value)
    {
        string svg = Svg(ax => ax.StatTile(value, t => t.Label = "Processes"));

        svg.AssertNoNonFiniteNumber("the picture");
        Assert.Contains(">Processes<", svg);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("AStatTileWithAValue_WritesTheNumber", () => Svg(ax => ax.StatTile(4217, t => t.Label = "Processes")), ">4217<"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    // ── quiver key ──────────────────────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(double.NaN, 0.9, 1.0)]
    [InlineData(0.8, double.NaN, 1.0)]
    [InlineData(0.8, 0.9, double.NaN)]
    [InlineData(0.8, 0.9, double.PositiveInfinity)]
    public void AQuiverKeyThatCannotBePlaced_DrawsNeitherArrowNorLabel(double x, double y, double u)
    {
        var ctx = Render(c => new QuiverKeySeriesRenderer(c).Render(new QuiverKeySeries(x, y, u, "1 m/s")));

        ctx.AssertNothingUndrawableReached();
        Assert.Empty(ctx.Calls);
    }

    [Fact]
    public void AQuiverKeyThatCanBePlaced_DrawsItsArrowAndLabel()
    {
        var ctx = Render(c => new QuiverKeySeriesRenderer(c).Render(new QuiverKeySeries(0.8, 0.9, 1.0, "1 m/s")));

        Assert.Equal(3, ctx.Count<DrawLineCall>());
        Assert.Equal(1, ctx.Count<DrawTextCall>());
    }
}
