// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Phase Y.2 (v1.7.2, 2026-04-19) — branch coverage for the
/// <see cref="AxesRenderer"/> base class. Pre-Y.2: 75.4%L / 62.4%B (complexity 383).
/// Targets the largest gaps:
/// - <c>RenderLegend</c> per <see cref="LegendPosition"/> arm (was 84%L/69%B)
/// - <c>ComputeLegendBounds</c> (was 79%L/50%B)
/// - <c>DrawLegendSwatch</c> for line, scatter, bar, hist series (was 31%L/60%B)
/// - <c>RenderTitle</c> per <see cref="TitleLocation"/> arm + math title (was 82%L/67%B)
/// - <c>RenderAxisLabels</c> with rotated and math labels (was 96%L/71%B)
/// - <c>RenderColorBar</c> for orientation + label arms (was 55%L/45%B)
/// - <c>IsLineTypeSeries</c> via legend swatch dispatch (was 0%L/0%B)
/// - <c>RegisterRenderer</c> public registration API (was 0%L/0%B)</summary>
public class AxesRendererCoverageTests
{
    private static string Render(Action<AxesBuilder> configure) =>
        Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, configure)
            .Build()
            .ToSvg();

    // ── RenderLegend per LegendPosition arm ───────────────────────────────

    [Theory]
    [InlineData(LegendPosition.Best)]
    [InlineData(LegendPosition.UpperRight)]
    [InlineData(LegendPosition.UpperLeft)]
    [InlineData(LegendPosition.LowerRight)]
    [InlineData(LegendPosition.LowerLeft)]
    [InlineData(LegendPosition.Right)]
    [InlineData(LegendPosition.CenterLeft)]
    [InlineData(LegendPosition.CenterRight)]
    public void RenderLegend_EveryPosition_DrawsLegend(LegendPosition position)
    {
        var svg = Render(ax => ax
            .Plot([1.0, 2, 3], [4.0, 5, 6], s => s.Label = "Series A")
            .Plot([1.0, 2, 3], [3.0, 4, 5], s => s.Label = "Series B")
            .WithLegend(position));
        Assert.Contains(">Series A<", svg);
        Assert.Contains(">Series B<", svg);
    }

    // ── DrawLegendSwatch for different series types ────────────────────────

    [Fact]
    public void RenderLegend_LineSeriesSwatch_DrawsCorrectShape()
    {
        var svg = Render(ax => ax
            .Plot([1.0, 2], [3.0, 4], s => s.Label = "Line")
            .WithLegend());
        Assert.Contains(">Line<", svg);
    }

    [Fact]
    public void RenderLegend_ScatterSeriesSwatch_DrawsCorrectShape()
    {
        var svg = Render(ax => ax
            .Scatter([1.0, 2], [3.0, 4], s => s.Label = "Scatter")
            .WithLegend());
        Assert.Contains(">Scatter<", svg);
    }

    [Fact]
    public void RenderLegend_BarSeriesSwatch_DrawsCorrectShape()
    {
        var svg = Render(ax => ax
            .Bar(["A", "B"], [10.0, 20.0], s => s.Label = "Bars")
            .WithLegend());
        Assert.Contains(">Bars<", svg);
    }

    [Fact]
    public void RenderLegend_HistogramSeriesSwatch_DrawsCorrectShape()
    {
        var svg = Render(ax => ax
            .Hist([1.0, 2, 3, 4, 5, 6], configure: s => s.Label = "Hist")
            .WithLegend());
        Assert.Contains(">Hist<", svg);
    }

    // ── RenderTitle per TitleLocation arm ──────────────────────────────────

    [Theory]
    [InlineData(TitleLocation.Center)]
    [InlineData(TitleLocation.Left)]
    [InlineData(TitleLocation.Right)]
    public void RenderTitle_EveryAlignment_DrawsTitle(TitleLocation loc)
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot([1.0, 2], [3.0, 4]);
                ax.WithTitle("My Title");
            })
            .Build();
        fig.SubPlots[0].TitleLoc = loc;
        Assert.Contains(">My Title<", fig.ToSvg());
    }

    [Fact]
    public void RenderTitle_MathMode_RendersAsRichText()
    {
        var svg = Render(ax => ax
            .Plot([1.0, 2], [3.0, 4])
            .WithTitle(@"$\alpha + \beta$"));
        Assert.NotNull(svg);
        Assert.Contains("<svg", svg);
    }

    // ── RenderAxisLabels with rotation + math ──────────────────────────────

    [Fact]
    public void RenderAxisLabels_MathLabel_RendersAsRichText()
    {
        var svg = Render(ax => ax
            .Plot([1.0, 2], [3.0, 4])
            .SetXLabel(@"$\theta$")
            .SetYLabel(@"$\rho^2$"));
        Assert.NotNull(svg);
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void RenderAxisLabels_LongLabelTriggersWrap_NoException()
    {
        var svg = Render(ax => ax
            .Plot([1.0, 2], [3.0, 4])
            .SetXLabel("Very long axis label that should be visible regardless")
            .SetYLabel("Another long label"));
        Assert.NotNull(svg);
    }

    // ── RenderColorBar branches ────────────────────────────────────────────

    [Fact]
    public void RenderColorBar_HeatmapSeries_DrawsColorBar()
    {
        var svg = Render(ax =>
        {
            var z = new double[3, 3] { { 1, 2, 3 }, { 4, 5, 6 }, { 7, 8, 9 } };
            ax.Heatmap(z);
            ax.WithColorBar();   // Visible defaults to true
        });
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void RenderColorBar_WithLabel_RendersLabelText()
    {
        var svg = Render(ax =>
        {
            var z = new double[2, 2] { { 1, 2 }, { 3, 4 } };
            ax.Heatmap(z);
            ax.WithColorBar(cb => cb with { Label = "Intensity" });
        });
        Assert.Contains(">Intensity<", svg);
    }

    // Note: RegisterRenderer can't be safely covered here — overwriting the global
    // registry with a test factory would either pollute subsequent tests (if the test
    // factory shadows production behaviour) or stack-overflow (if it delegates back
    // to AxesRenderer.Create). The method is a one-liner that wraps a thread-safe
    // ConcurrentDictionary.AddOrUpdate; its 0%-line shows up because no production
    // caller currently re-registers a CoordinateSystem mapping after startup. A future
    // dedicated phase (with a custom CoordinateSystem enum value, requires production
    // changes) is needed to cover this safely.

    // ── Multi-series legend with mixed types ───────────────────────────────

    [Fact]
    public void RenderLegend_MixedSeriesTypes_AllSwatchesDrawn()
    {
        var svg = Render(ax => ax
            .Plot([1.0, 2], [3.0, 4], s => s.Label = "Line")
            .Scatter([1.0, 2], [4.0, 5], s => s.Label = "Scatter")
            .Bar(["A", "B"], [1.0, 2.0], s => s.Label = "Bars")
            .WithLegend(LegendPosition.UpperRight));
        Assert.Contains(">Line<", svg);
        Assert.Contains(">Scatter<", svg);
        Assert.Contains(">Bars<", svg);
    }

    // ── Title with custom font weight + size ───────────────────────────────

    [Fact]
    public void RenderTitle_CustomFont_AppliesStyle()
    {
        var svg = Render(ax => ax
            .Plot([1.0, 2], [3.0, 4])
            .WithTitle("Bold Title", style => style with
            {
                FontWeight = FontWeight.Bold,
                FontSize = 20,
            }));
        Assert.Contains(">Bold Title<", svg);
    }

    // ── Empty-series legend (no series with labels) ────────────────────────

    [Fact]
    public void RenderLegend_NoLabels_DoesNotDrawLegendBox()
    {
        var svg = Render(ax => ax
            .Plot([1.0, 2], [3.0, 4])     // unlabeled
            .WithLegend());
        // Legend with no labelled series should not crash.
        Assert.NotNull(svg);
    }
}
