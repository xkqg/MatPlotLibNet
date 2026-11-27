// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Frames;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A draw of a figure is laid out once: the spacing, the height of the figure's title and the plot area of each
/// subplot are one <see cref="Rendering.ChartRenderer.FigureFrame"/>, which the drawing and the layout a native control hit-tests with both read. A
/// paint that draws and lays out (what a control does) measures nothing twice, and its layout is the one
/// <see cref="ChartRenderer.ComputeLayout"/> gives.</summary>
public sealed class FigureFrameTests
{
    /// <summary>Counts how often a text is measured; draws nothing.</summary>
    private sealed class MeasureCounter : SilentRenderContext
    {
        public Dictionary<string, int> Measured { get; } = new(StringComparer.Ordinal);

        public override Size MeasureText(string text, Font font)
        {
            Measured[text] = Measured.GetValueOrDefault(text) + 1;
            return base.MeasureText(text, font);
        }
    }

    private static Figure Titled(bool tight = false)
    {
        var builder = Plt.Create().WithSize(500, 400).WithTitle("The figure's title")
            .AddSubPlot(1, 2, 1, ax => ax.Plot([0.0, 1, 2], [1.0, 3, 2], s => s.Label = "a").WithLegend())
            .AddSubPlot(1, 2, 2, ax => ax.Plot([0.0, 1, 2], [2.0, 1, 3]));
        if (tight) builder = builder.TightLayout();
        return builder.Build();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void APaint_MeasuresTheTitleNoMoreOftenThanADrawAlone(bool tight)
    {
        var drawn = new MeasureCounter();
        new ChartRenderer().Render(Titled(tight), drawn);
        var painted = new MeasureCounter();

        _ = new ChartRenderer().RenderAndLayOut(Titled(tight), new DirectFrameTarget(painted));

        Assert.Equal(drawn.Measured["The figure's title"], painted.Measured["The figure's title"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void APaintsLayout_IsTheLayoutComputeLayoutGives(bool tight)
    {
        var figure = Titled(tight);

        var painted = new ChartRenderer().RenderAndLayOut(figure, new DirectFrameTarget(new MeasureCounter()));
        var computed = new ChartRenderer().ComputeLayout(figure, new MeasureCounter());

        Assert.Equal(computed.PlotAreas, painted.PlotAreas);
        Assert.Equal(computed.LegendItems.Count, painted.LegendItems.Count);
        for (int i = 0; i < computed.LegendItems.Count; i++) Assert.Equal(computed.LegendItems[i], painted.LegendItems[i]);
    }

    [Fact]
    public void TheFrame_StandsTheFirstRowBelowATallTitle()
    {
        var figure = Titled();
        var ctx = new MeasureCounter();
        var renderer = new ChartRenderer();

        var frame = renderer.FrameOf(figure, ctx);

        Assert.True(frame.TitleHeight > 0);
        Assert.True(frame.PlotAreaTop >= frame.TitleHeight);
        Assert.Equal(frame.PlotAreas, renderer.ComputeLayout(figure, ctx).PlotAreas);
    }

    [Fact]
    public void AFigureWithoutATitle_HasATitleHeightOfNothing_AndItsTopMargin()
    {
        var figure = Plt.Create().Plot([0.0, 1], [1.0, 2]).Build();

        var frame = new ChartRenderer().FrameOf(figure, new MeasureCounter());

        Assert.Equal(0, frame.TitleHeight);
        Assert.Equal(frame.Spacing.MarginTop, frame.PlotAreaTop);
    }

    [Fact]
    public void AMathTitle_IsMeasuredAsTheRichTextItIsDrawnAs()
    {
        var figure = Plt.Create().WithTitle("$x^2$").Plot([0.0, 1], [1.0, 2]).Build();
        var ctx = new MeasureCounter();

        var frame = new ChartRenderer().FrameOf(figure, ctx);

        var font = figure.Theme.SupTitleFont();
        double expected = ((IRenderContext)new MeasureCounter()).MeasureRichText(MathTextParser.Parse("$x^2$"), font).Height;
        Assert.Equal(expected, frame.TitleHeight);
    }
}
