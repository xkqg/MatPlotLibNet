// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A quiver key is drawn in the units matplotlib draws it in. matplotlib 3.11.2, read with inspect and printed:
/// <c>QuiverKey.__init__(Q, X, Y, U, label, ...)</c>: "U: The length of the key", drawn by the quiver's own arrow
/// (<c>self.Q._make_verts([[0., 0.]], u, v, 'uv')</c>), so it is as long as an arrow of the vector (U, 0) is in that quiver, not
/// U units of the X axis; the label is <c>mtext.Text(text=label, fontproperties=fontproperties or {})</c>, which takes
/// <c>rcParams['font.size']</c> (printed: 10.0 in the default style, 12.0 in the classic one), in points. The library draws an arrow
/// by the vector times the quiver's <c>Scale</c> in data units, so a key of U is U times the <c>Scale</c> of the quiver it stands
/// for: the last quiver drawn before it on its axes. The label took 12 as pixels where its documentation says points.</summary>
public sealed class QuiverKeyUnitsTests
{
    private static readonly Rect Bounds = new(0, 0, 400, 300);   // 0 to 10 over 400 pixels: a unit is 40 pixels

    private static QuiverSeries Quiver(double scale) => new([1.0], [1.0], [1.0], [0.0]) { Scale = scale, AutoScale = false };   // data units under every theme

    private static RecordingRenderContext Render(QuiverKeySeries key, Theme? theme = null, params ISeries[] before)
    {
        var ctx = new RecordingRenderContext();
        var context = new SeriesRenderContext(new DataTransform(0, 10, 0, 10, Bounds), ctx, Colors.Tab10Blue, new RenderArea(Bounds, ctx))
        {
            Theme = theme ?? Theme.Default,
            AxesSeries = [.. before, key],
        };
        new QuiverKeySeriesRenderer(context).Render(key);
        return ctx;
    }

    private static double ShaftLength(RecordingRenderContext ctx)
    {
        var line = ctx.Of<DrawLineCall>()[0];
        Point p1 = line.P1, p2 = line.P2;
        return Math.Abs(p2.X - p1.X);
    }

    private static double LabelSize(RecordingRenderContext ctx) => (ctx.Of<DrawTextCall>().Single().Font).Size;

    // ── the length ─────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AKey_IsAsLongAsAVectorOfItsLengthIsInTheQuiverItStandsFor()
    {
        var key = new QuiverKeySeries(0.5, 0.5, 2.0, "key");

        Assert.Equal(2.0 * 0.25 * 40, ShaftLength(Render(key, null, Quiver(0.25))), 9);
    }

    [Fact]
    public void AKey_FollowsTheLastQuiverBeforeIt_NotAnEarlierOneNorALaterOne()
    {
        var key = new QuiverKeySeries(0.5, 0.5, 2.0, "key");
        var later = new List<ISeries> { Quiver(0.5), Quiver(0.25), key, Quiver(0.125) };
        var ctx = new RecordingRenderContext();
        var context = new SeriesRenderContext(new DataTransform(0, 10, 0, 10, Bounds), ctx, Colors.Tab10Blue, new RenderArea(Bounds, ctx))
        {
            AxesSeries = later,
        };

        new QuiverKeySeriesRenderer(context).Render(key);

        Assert.Equal(2.0 * 0.25 * 40, ShaftLength(ctx), 9);
    }

    [Fact]
    public void AKeyThatIsNotAmongTheSeriesOfItsAxes_FollowsTheLastQuiverOfThem()
    {
        var key = new QuiverKeySeries(0.5, 0.5, 2.0, "key");
        var ctx = new RecordingRenderContext();
        var context = new SeriesRenderContext(new DataTransform(0, 10, 0, 10, Bounds), ctx, Colors.Tab10Blue, new RenderArea(Bounds, ctx))
        {
            AxesSeries = [Quiver(0.5), Quiver(0.25)],
        };

        new QuiverKeySeriesRenderer(context).Render(key);

        Assert.Equal(2.0 * 0.25 * 40, ShaftLength(ctx), 9);
    }

    [Fact]
    public void AKeyWithNoQuiverBeforeIt_IsDrawnInDataUnits_AsItAlwaysWas()
    {
        var key = new QuiverKeySeries(0.5, 0.5, 2.0, "key");

        Assert.Equal(2.0 * 40, ShaftLength(Render(key)), 9);
        Assert.Equal(2.0 * 40, ShaftLength(Render(key, null, new LineSeries([1.0], [1.0]))), 9);
    }

    [Fact]
    public void AKeyRenderedOnItsOwn_NoContextOfItsAxes_IsDrawnInDataUnits()
    {
        var key = new QuiverKeySeries(0.5, 0.5, 2.0, "key");
        var ctx = new RecordingRenderContext();

        new QuiverKeySeriesRenderer(new SeriesRenderContext(new DataTransform(0, 10, 0, 10, Bounds), ctx, Colors.Tab10Blue, new RenderArea(Bounds, ctx)))
            .Render(key);

        Assert.Equal(2.0 * 40, ShaftLength(ctx), 9);
    }

    [Fact]
    public void AFigure_DrawsTheKeyBytheQuiversScale()
    {
        double Shaft(double scale)
        {
            var svg = Plt.Create().WithSize(500, 400).AddSubPlot(1, 1, 1, ax => ax
                .AddSeries(Quiver(scale))
                .AddSeries(new QuiverKeySeries(0.5, 0.9, 1.0, "key") { Color = Colors.Red })
                .SetXLim(0, 10).SetYLim(0, 10)).Build().ToSvg().Xml();
            var line = svg.Each("line").First(e => (string?)e.Attribute("stroke") == Colors.Red.ToHex());
            return Math.Abs(line.Number("x2") - line.Number("x1"));
        }

        double wide = Shaft(1.0);
        double narrow = Shaft(0.25);

        Assert.True(wide > 30, $"a key of one data unit is {wide} px long");
        Assert.Equal(wide / 4, narrow, 1);
    }

    /// <summary>The length of the red key's shaft in a figure built by <paramref name="build"/>.</summary>
    private static double KeyShaft(Action<Axes> build)
    {
        var figure = Plt.Create().WithSize(500, 400).Build();
        var axes = figure.AddSubPlot(1, 1, 1);
        axes.XAxis.Min = 0; axes.XAxis.Max = 10; axes.YAxis.Min = 0; axes.YAxis.Max = 10;
        build(axes);
        var svg = figure.ToSvg().Xml();
        var line = svg.Each("line").First(e => (string?)e.Attribute("stroke") == Colors.Red.ToHex());
        return Math.Abs(line.Number("x2") - line.Number("x1"));
    }

    private static QuiverKeySeries RedKey() => new(0.5, 0.9, 1.0, "key") { Color = Colors.Red };

    /// <summary>A key and its quiver on a Y axis past the primary: the key stands for the quiver before it on that axis, as it does
    /// on the primary.</summary>
    [Fact]
    public void AKeyOnAStackedYAxis_FollowsTheQuiverBeforeItOnThatAxis()
    {
        double Shaft(double scale) => KeyShaft(axes =>
        {
            var slot = axes.YAxes.Right.FirstOrAdd();
            slot.AddSeries(Quiver(scale));
            slot.AddSeries(RedKey());
        });

        Assert.Equal(Shaft(1.0) / 4, Shaft(0.25), 1);
    }

    /// <summary>A key on a Y axis past the primary does not stand for a quiver of the primary: the quivers it may stand for are the
    /// ones drawn before it on its own axis.</summary>
    [Fact]
    public void AKeyOnAStackedYAxis_DoesNotFollowAQuiverOfThePrimary()
    {
        double Shaft(double scale) => KeyShaft(axes =>
        {
            axes.AddSeries(Quiver(scale));
            axes.YAxes.Right.FirstOrAdd().AddSeries(RedKey());
        });

        Assert.Equal(Shaft(1.0), Shaft(0.25), 6);
    }

    /// <summary>A key and its quiver on the top X axis: the key stands for the quiver before it there.</summary>
    [Fact]
    public void AKeyOnTheTopAxis_FollowsTheQuiverBeforeItOnThatAxis()
    {
        double Shaft(double scale) => KeyShaft(axes =>
        {
            axes.AddXSecondarySeries(Quiver(scale));
            axes.AddXSecondarySeries(RedKey());
            axes.SecondaryXAxis!.Min = 0;
            axes.SecondaryXAxis.Max = 10;
        });

        Assert.Equal(Shaft(1.0) / 4, Shaft(0.25), 1);
    }

    // ── the label ──────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ALabelThatWasGivenNoSize_IsTheStylesFontSize_AsMatplotlibsIs()
    {
        var key = new QuiverKeySeries(0.5, 0.5, 2.0, "key");

        Assert.Equal(10.0.PointsInPixels, LabelSize(Render(key, Theme.MatplotlibV2)), 9);     // font.size 10 in the default style
        Assert.Equal(12.0.PointsInPixels, LabelSize(Render(key, Theme.MatplotlibClassic)), 9);  // and 12 in the classic one
        Assert.Equal(Theme.Default.DefaultFont.Size, LabelSize(Render(key, Theme.Default)), 9);
    }

    [Fact]
    public void ALabelThatNamesASize_IsThatManyPoints()
    {
        var key = new QuiverKeySeries(0.5, 0.5, 2.0, "key") { FontSize = 14 };

        Assert.Equal(14.0.PointsInPixels, LabelSize(Render(key, Theme.MatplotlibV2)), 9);
        Assert.Equal(14.0.PointsInPixels, LabelSize(Render(key, Theme.Default)), 9);
    }
}
