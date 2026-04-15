// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>
/// Covers the Feature C deliverable: outside legend reservation in
/// <see cref="ConstrainedLayoutEngine"/> via the shared <see cref="LegendMeasurer"/>.
/// </summary>
public class OutsideLegendLayoutTests
{
    // ──────────────────────────────────────────────────────────────────────────
    // LegendMeasurer invariants
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void LegendMeasurer_EmptyLabels_ReturnsZeroSize()
    {
        var axes = new Axes();
        axes.Plot([1.0, 2.0], [1.0, 2.0]);  // series with no label
        var size = LegendMeasurer.MeasureBox(axes, new SvgRenderContext(),
            new Font { Family = "sans-serif", Size = 12 });
        Assert.Equal(Size.Empty, size);
    }

    [Fact]
    public void LegendMeasurer_LongLabels_ProduceLargerBoxThanShortLabels()
    {
        var shortAxes = new Axes();
        shortAxes.Plot([1.0, 2.0], [1.0, 2.0]).Label = "A";
        var longAxes = new Axes();
        longAxes.Plot([1.0, 2.0], [1.0, 2.0]).Label = "A very long descriptive label indeed";

        var ctx = new SvgRenderContext();
        var font = new Font { Family = "sans-serif", Size = 12 };
        var shortBox = LegendMeasurer.MeasureBox(shortAxes, ctx, font);
        var longBox = LegendMeasurer.MeasureBox(longAxes, ctx, font);

        Assert.True(longBox.Width > shortBox.Width,
            $"Expected long label to widen box; got short={shortBox.Width}, long={longBox.Width}");
    }

    [Fact]
    public void LegendMeasurer_IsOutsidePosition_ClassifiesCorrectly()
    {
        Assert.True(LegendMeasurer.IsOutsidePosition(LegendPosition.OutsideRight));
        Assert.True(LegendMeasurer.IsOutsidePosition(LegendPosition.OutsideLeft));
        Assert.True(LegendMeasurer.IsOutsidePosition(LegendPosition.OutsideTop));
        Assert.True(LegendMeasurer.IsOutsidePosition(LegendPosition.OutsideBottom));
        Assert.False(LegendMeasurer.IsOutsidePosition(LegendPosition.UpperRight));
        Assert.False(LegendMeasurer.IsOutsidePosition(LegendPosition.Best));
        Assert.False(LegendMeasurer.IsOutsidePosition(LegendPosition.Center));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // ConstrainedLayoutEngine — outside legend reservation
    // ──────────────────────────────────────────────────────────────────────────

    private static Figure BuildFigure(LegendPosition position, params string[] labels)
    {
        var fb = new FigureBuilder()
            .WithSize(800, 600)
            .TightLayout()
            .AddSubPlot(1, 1, 1, ax =>
            {
                foreach (var label in labels)
                    ax.Plot([1.0, 2.0], [1.0, 2.0], s => s.Label = label);
                ax.WithLegend(l => l with { Position = position });
            });
        return fb.Build();
    }

    [Fact]
    public void OutsideRight_InflatesRightMargin_AboveDefault()
    {
        var insideFig = BuildFigure(LegendPosition.UpperRight,
            "Alpha", "Beta", "Gamma", "Delta — a longer label");
        var outsideFig = BuildFigure(LegendPosition.OutsideRight,
            "Alpha", "Beta", "Gamma", "Delta — a longer label");

        var engine = new ConstrainedLayoutEngine();
        var ctx = new SvgRenderContext();
        var inside = engine.Compute(insideFig, ctx);
        var outside = engine.Compute(outsideFig, ctx);

        Assert.True(outside.MarginRight > inside.MarginRight,
            $"OutsideRight should widen right margin; got inside={inside.MarginRight}, outside={outside.MarginRight}");
    }

    [Fact]
    public void OutsideLeft_InflatesLeftMargin_AboveDefault()
    {
        var insideFig = BuildFigure(LegendPosition.UpperLeft,
            "Alpha", "Beta", "Gamma");
        var outsideFig = BuildFigure(LegendPosition.OutsideLeft,
            "Alpha", "Beta", "Gamma");

        var engine = new ConstrainedLayoutEngine();
        var ctx = new SvgRenderContext();
        var inside = engine.Compute(insideFig, ctx);
        var outside = engine.Compute(outsideFig, ctx);

        Assert.True(outside.MarginLeft > inside.MarginLeft,
            $"OutsideLeft should widen left margin; got inside={inside.MarginLeft}, outside={outside.MarginLeft}");
    }

    [Fact]
    public void OutsideBottom_InflatesBottomMargin_AboveDefault()
    {
        var insideFig = BuildFigure(LegendPosition.LowerCenter, "A", "B");
        var outsideFig = BuildFigure(LegendPosition.OutsideBottom, "A", "B");

        var engine = new ConstrainedLayoutEngine();
        var ctx = new SvgRenderContext();
        var inside = engine.Compute(insideFig, ctx);
        var outside = engine.Compute(outsideFig, ctx);

        Assert.True(outside.MarginBottom > inside.MarginBottom,
            $"OutsideBottom should widen bottom margin; got inside={inside.MarginBottom}, outside={outside.MarginBottom}");
    }

    [Fact]
    public void OutsideRight_WiderLabels_ProduceWiderRightMargin()
    {
        var narrowFig = BuildFigure(LegendPosition.OutsideRight, "A");
        var wideFig = BuildFigure(LegendPosition.OutsideRight,
            "A very long descriptive legend label that definitely needs more space");

        var engine = new ConstrainedLayoutEngine();
        var ctx = new SvgRenderContext();
        var narrow = engine.Compute(narrowFig, ctx);
        var wide = engine.Compute(wideFig, ctx);

        Assert.True(wide.MarginRight > narrow.MarginRight,
            $"Wider label should produce wider right margin; got narrow={narrow.MarginRight}, wide={wide.MarginRight}");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // AxesRenderer — outside legend SVG placement
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void OutsideRight_Renders_WithoutErrorAndEmitsLegendElements()
    {
        string svg = Plt.Create()
            .WithSize(900, 500)
            .TightLayout()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot([1.0, 2.0, 3.0], [1.0, 4.0, 2.0], s => s.Label = "SeriesA");
                ax.Plot([1.0, 2.0, 3.0], [2.0, 1.0, 3.0], s => s.Label = "SeriesB");
                ax.WithLegend(l => l with { Position = LegendPosition.OutsideRight });
            })
            .ToSvg();

        // Legend frame is a rectangle and the labels are rendered as glyph paths — the most
        // reliable invariant is that the SVG was produced without throwing and contains the
        // "Chart legend" accessibility group that RenderLegend always emits.
        Assert.Contains("Chart legend", svg);
        Assert.Contains("<svg", svg);
    }
}
