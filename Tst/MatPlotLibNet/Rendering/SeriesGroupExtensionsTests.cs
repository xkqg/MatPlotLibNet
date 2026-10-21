// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary><see cref="SeriesGroupExtensions.BeginSeriesGroup"/> — the one rule for the group a series is drawn in,
/// shared by the primary series passes and both secondary-axis parts.</summary>
public class SeriesGroupExtensionsTests
{
    private readonly record struct OneLine(Axes Axes, LineSeries Line);

    private static OneLine Line(string? label, bool interactive)
    {
        var axes = new Axes { EnableInteractiveAttributes = interactive };
        var line = axes.Plot([0.0, 1.0], [0.0, 1.0]);
        line.Label = label;
        return new OneLine(axes, line);
    }

    [Fact]
    public void WithInteractiveAttributes_OpensADataGroupWithTheSlotAndTheLabel()
    {
        var (axes, line) = Line("Revenue & cost", interactive: true);
        var ctx = new SvgRenderContext();

        Assert.True(ctx.BeginSeriesGroup(axes, line, 7));

        Assert.Equal("<g class=\"series\" data-series-index=\"7\" aria-label=\"Revenue &amp; cost\">",
                     ctx.GetOutput().Trim());
    }

    [Fact]
    public void WithInteractiveAttributes_AnUnlabelledSeries_StillGetsItsDataGroup()
    {
        var (axes, line) = Line(null, interactive: true);
        var ctx = new SvgRenderContext();

        Assert.True(ctx.BeginSeriesGroup(axes, line, 3));

        Assert.Equal("<g class=\"series\" data-series-index=\"3\">", ctx.GetOutput().Trim());
    }

    [Fact]
    public void WithoutInteractiveAttributes_ALabelledSeries_GetsAnAccessibleGroup()
    {
        var (axes, line) = Line("Revenue", interactive: false);
        var ctx = new SvgRenderContext();

        Assert.True(ctx.BeginSeriesGroup(axes, line, 2));

        Assert.Equal("<g class=\"series\" aria-label=\"Revenue\">", ctx.GetOutput().Trim());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void WithoutInteractiveAttributes_AnUnlabelledSeries_GetsNoGroup(string? label)
    {
        var (axes, line) = Line(label, interactive: false);
        var ctx = new SvgRenderContext();

        Assert.False(ctx.BeginSeriesGroup(axes, line, 0));

        Assert.Equal("", ctx.GetOutput());
    }

    [Fact]
    public void ANonSvgContext_OpensNoGroup()
    {
        var (axes, line) = Line("Revenue", interactive: true);
        var ctx = new RecordingRenderContext();

        Assert.False(ctx.BeginSeriesGroup(axes, line, 0));

        Assert.Empty(ctx.Calls);
    }
}
