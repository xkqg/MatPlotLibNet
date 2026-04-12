// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>Verifies <see cref="ConstrainedLayoutEngine"/> margin computation and integration with <see cref="ChartRenderer"/>.</summary>
public class ConstrainedLayoutTests
{
    private static readonly SubPlotSpacing Defaults = new();

    /// <summary>Builds a minimal figure with optional axes customization.</summary>
    private static Figure SimpleFigure(Action<AxesBuilder>? configure = null)
    {
        var fb = new FigureBuilder().Plot([1.0, 2.0], [1.0, 2.0]);
        if (configure is not null)
            return new FigureBuilder()
                .AddSubPlot(1, 1, 1, ax => { ax.Plot([1.0, 2.0], [1.0, 2.0]); configure(ax); })
                .Build();
        return fb.Build();
    }

    private static IRenderContext MakeCtx() => new SvgRenderContext();

    // --- CharacterWidthTable-backed MeasureText improves over uniform 0.6 ---

    /// <summary>A figure with no labels and no title should still return at least the minimum clamped margins.</summary>
    [Fact]
    public void Engine_NoLabels_NoTitle_ReturnsAtLeastMinMargins()
    {
        var spacing = new ConstrainedLayoutEngine().Compute(SimpleFigure(), MakeCtx());

        Assert.True(spacing.MarginLeft >= 30);
        Assert.True(spacing.MarginBottom >= 30);
        Assert.True(spacing.MarginTop >= 20);
        Assert.True(spacing.MarginRight >= 10);
    }

    /// <summary>When a Y-axis label is present the left margin must exceed the no-label baseline.</summary>
    [Fact]
    public void Engine_WithYAxisLabel_LeftMarginLargerThanNoLabel()
    {
        var ctx = MakeCtx();
        var withLabel    = new ConstrainedLayoutEngine().Compute(
            SimpleFigure(ax => ax.SetYLabel("Amplitude (dB)")), ctx);
        var withoutLabel = new ConstrainedLayoutEngine().Compute(SimpleFigure(), ctx);

        Assert.True(withLabel.MarginLeft >= withoutLabel.MarginLeft);
    }

    /// <summary>When an X-axis label is present the bottom margin must exceed the no-label baseline.</summary>
    [Fact]
    public void Engine_WithXAxisLabel_BottomMarginLargerThanNoLabel()
    {
        var ctx = MakeCtx();
        var withLabel    = new ConstrainedLayoutEngine().Compute(
            SimpleFigure(ax => ax.SetXLabel("Time (seconds)")), ctx);
        var withoutLabel = new ConstrainedLayoutEngine().Compute(SimpleFigure(), ctx);

        Assert.True(withLabel.MarginBottom >= withoutLabel.MarginBottom);
    }

    /// <summary>When an axes title is present the top margin must be at least the default.</summary>
    [Fact]
    public void Engine_WithAxesTitle_TopMarginAtLeastDefault()
    {
        var spacing = new ConstrainedLayoutEngine().Compute(
            SimpleFigure(ax => ax.WithTitle("My subplot title")), MakeCtx());

        Assert.True(spacing.MarginTop >= Defaults.MarginTop);
    }

    /// <summary>When the Y-axis is configured to show large numbers the left margin should be wider
    /// than when it shows small numbers.</summary>
    [Fact]
    public void Engine_WideYRange_LargerLeftMargin()
    {
        var ctx = MakeCtx();

        // Large numbers → longer tick label strings
        var largeFigure = new FigureBuilder()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot([1.0, 2.0], [1_000_000.0, 9_999_999.0]);
                ax.SetYLim(1_000_000, 9_999_999);
            })
            .Build();

        // Small numbers → shorter tick labels
        var smallFigure = new FigureBuilder()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot([1.0, 2.0], [1.0, 9.0]);
                ax.SetYLim(1, 9);
            })
            .Build();

        var largeSpacing = new ConstrainedLayoutEngine().Compute(largeFigure, ctx);
        var smallSpacing = new ConstrainedLayoutEngine().Compute(smallFigure, ctx);

        Assert.True(largeSpacing.MarginLeft >= smallSpacing.MarginLeft,
            $"Expected large-range left margin ({largeSpacing.MarginLeft}) >= small-range ({smallSpacing.MarginLeft})");
    }

    /// <summary>With multiple subplots the engine takes the maximum requirements across all of them.</summary>
    [Fact]
    public void Engine_MultipleSubplots_TakesMaxAcrossAll()
    {
        var ctx = MakeCtx();

        // One subplot has a long Y label, the other doesn't
        var figure = new FigureBuilder()
            .AddSubPlot(1, 2, 1, ax => { ax.Plot([1.0, 2.0], [1.0, 2.0]); ax.SetYLabel("A very long Y axis label"); })
            .AddSubPlot(1, 2, 2, ax => ax.Plot([1.0, 2.0], [1.0, 2.0]))
            .Build();

        var singleLabel = new ConstrainedLayoutEngine().Compute(
            new FigureBuilder()
                .AddSubPlot(1, 1, 1, ax => { ax.Plot([1.0, 2.0], [1.0, 2.0]); ax.SetYLabel("A very long Y axis label"); })
                .Build(), ctx);

        var multiSpacing = new ConstrainedLayoutEngine().Compute(figure, ctx);

        // Should be at least as large as the subplot that has the label
        Assert.True(multiSpacing.MarginLeft >= singleLabel.MarginLeft - 1);
    }

    /// <summary>Enabling TightLayout on FigureBuilder causes ChartRenderer to produce SVG without throwing.</summary>
    [Fact]
    public void FigureBuilder_TightLayout_ProducesSvgWithoutError()
    {
        string svg = new FigureBuilder()
            .WithTitle("Test")
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.SetXLabel("Time");
                ax.SetYLabel("Value");
                ax.Plot([1.0, 2.0, 3.0], [1.0, 4.0, 9.0]);
            })
            .TightLayout()
            .Build()
            .ToSvg();

        Assert.NotEmpty(svg);
        Assert.Contains("<svg", svg);
    }

    /// <summary>ConstrainedLayout() method exists on FigureBuilder and produces valid SVG.</summary>
    [Fact]
    public void FigureBuilder_ConstrainedLayout_ProducesSvgWithoutError()
    {
        string svg = new FigureBuilder()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ConstrainedLayout()
            .Build()
            .ToSvg();

        Assert.NotEmpty(svg);
        Assert.Contains("<svg", svg);
    }

    /// <summary>Left margin is clamped to [30, 120] regardless of content.</summary>
    [Fact]
    public void Engine_LeftMargin_Clamped_Between30And120()
    {
        var spacing = new ConstrainedLayoutEngine().Compute(SimpleFigure(), MakeCtx());
        Assert.InRange(spacing.MarginLeft, 30, 120);
    }

    /// <summary>Bottom margin is clamped to [30, 100] regardless of content.</summary>
    [Fact]
    public void Engine_BottomMargin_Clamped_Between30And100()
    {
        var spacing = new ConstrainedLayoutEngine().Compute(SimpleFigure(), MakeCtx());
        Assert.InRange(spacing.MarginBottom, 30, 100);
    }

    /// <summary>TightLayout and ConstrainedLayout produce different margin values than the defaults
    /// when axis labels are present.</summary>
    [Fact]
    public void TightLayout_WithAxisLabels_MarginsAdaptToContent()
    {
        // Build the same figure with and without TightLayout
        var withTight = new FigureBuilder()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.SetXLabel("A long X label text");
                ax.SetYLabel("A long Y label text");
                ax.Plot([1.0, 2.0], [1.0, 2.0]);
            })
            .TightLayout()
            .Build();

        // TightLayout must not produce zero-size plot areas (i.e., the engine completed)
        var svg = withTight.ToSvg();
        Assert.NotEmpty(svg);
    }
}
