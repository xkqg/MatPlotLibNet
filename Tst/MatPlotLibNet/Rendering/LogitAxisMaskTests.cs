// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A logit axis masks a value it cannot place (0 or below, 1 or above), as matplotlib 3.11.2's logit scale does
/// (<c>LogitScale(nonpositive='mask')</c>; printed: <c>ax.yaxis.get_transform()._nonpositive</c> is <c>'mask'</c>). Only the log
/// scale clips (<c>nonpositive='clip'</c>). Printed with python: <c>ax.bar([1, 2], [0.5, 0.9], bottom=[0, 0.3])</c> on a logit Y
/// axis gives the first bar the vertices <c>[[nan, -inf], [nan, -inf], [189.34, 131.71], [64.09, 131.71], [nan, -inf]]</c> and
/// draws no fill for it (no pixel of its colour). The library pinned such a base to the bottom edge, as a log axis clips it, and
/// drew the bar from there.</summary>
public class LogitAxisMaskTests
{
    private static readonly Rect Bounds = new(0, 0, 400, 300);

    private static RecordingRenderContext Render(DataTransform transform, Action<SeriesRenderContext> draw)
    {
        var ctx = new RecordingRenderContext();
        draw(new SeriesRenderContext(transform, ctx, Colors.Tab10Blue, new RenderArea(Bounds, ctx)));
        return ctx;
    }

    [Fact]
    public void ALogitAxis_HasNoPlaceBeyond_AndClipsNoBase()
    {
        var logit = new PixelDirection(100, 0, 50, null, ScaleRule.Logit, new Range1D(-1, 1));

        Assert.False(logit.ClipsBase(0));
        Assert.False(logit.ClipsBase(1.5));
        Assert.True(double.IsNaN(Bounds.LogitY().DataToPixelClippedAlong(false, 1, 0).Y));
    }

    [Fact]
    public void ABarStandingOnZero_IsNotDrawnOnALogitAxis()
    {
        var ctx = Render(Bounds.LogitY(), c => new BarSeriesRenderer(c).Render(new BarSeries(["a"], [0.5])));

        Assert.Equal(0, ctx.Count<DrawRectangleCall>());
    }

    /// <summary>A bar standing on a base the logit axis can place is drawn as before.</summary>
    [Fact]
    public void ABarStandingOnAPlacedBase_IsDrawnOnALogitAxis()
    {
        var ctx = Render(Bounds.LogitY(), c => new BarSeriesRenderer(c).Render(new BarSeries(["a"], [0.5]) { StackBaseline = [0.2] }));

        Assert.Equal(1, ctx.Count<DrawRectangleCall>());
    }

    public static TheoryData<string> Kinds { get; } = new(
        "bar", "hist", "hist step", "hist stepfilled", "area", "area lower edge", "area where", "stackplot", "box", "violin", "kde",
        "ecdf", "stem", "errorbar", "waterfall");

    /// <summary>Every mark that stands on 0 on a logit axis: whatever it leaves out, no coordinate it cannot draw reaches the
    /// SVG.</summary>
    [Theory]
    [MemberData(nameof(Kinds))]
    public void AFigureOnALogitAxis_WritesNoNaN_ForAMarkThatStandsOnZero(string kind)
    {
        double[] x = [0.0, 1, 2];
        double[] sample = [0.2, 0.3, 0.35, 0.5, 0.6, 0.7];
        string svg = Plt.Create().WithSize(600, 400).AddSubPlot(1, 1, 1, ax =>
        {
            ax.SetYScale(AxisScale.Logit);
            switch (kind)
            {
                case "bar": ax.Bar(["a", "b"], [0.3, 0.6]); break;
                case "hist": ax.Hist(sample, 3); break;
                case "hist step": ax.Hist(sample, 3, s => s.HistType = HistType.Step); break;
                case "hist stepfilled": ax.Hist(sample, 3, s => s.HistType = HistType.StepFilled); break;
                case "area": ax.FillBetween(x, [0.3, 0.6, 0.4]); break;
                case "area lower edge": ax.FillBetween(x, [0.3, 0.6, 0.4], [0.2, 0.0, 0.2]); break;
                case "area where": ax.FillBetween(x, [0.3, 0.6, 0.4], null, s => s.Where = (_, v) => v > 0.35); break;
                case "stackplot": ax.StackPlot(x, [[0.2, 0.3, 0.2], [0.1, 0.2, 0.1]]); break;
                case "box": ax.BoxPlot([sample, [-0.5, 0.2, 0.4]]); break;
                case "violin": ax.Violin([sample, [-0.5, 0.2, 0.4]]); break;
                case "kde": ax.Kde(sample); break;
                case "ecdf": ax.Ecdf(sample); break;
                case "stem": ax.Stem([1.0, 2, 3], [0.2, 0.5, 0.7]); break;
                case "errorbar": ax.ErrorBar([1.0, 2, 3], [0.2, 0.5, 0.7], [0.3, 0.1, 0.1], [0.1, 0.1, 0.4]); break;
                default: ax.Waterfall(["a", "b"], [0.3, 0.2]); break;
            }
        }).ToSvg();

        svg.AssertNoNonFiniteNumber();
    }
}
