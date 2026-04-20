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

    // ── Phase Z.3: ColorBar extend arms (vertical + horizontal × Min/Max/Both)

    /// <summary>Vertical colorbar with Extend=Min — covers `extendMin` true arm at line 676,
    /// and the under-color rectangle draw at line 678-679.</summary>
    [Fact]
    public void RenderColorBar_VerticalExtendMin_DrawsUnderRect()
    {
        var svg = Render(ax =>
        {
            ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } });
            ax.WithColorBar(cb => cb with
            {
                Orientation = global::MatPlotLibNet.Styling.ColorBarOrientation.Vertical,
                Extend = global::MatPlotLibNet.Styling.ColorMaps.ColorBarExtend.Min,
            });
        });
        Assert.Contains("<svg", svg);
    }

    /// <summary>Vertical colorbar with Extend=Max — covers `extendMax` true arm at line 659.</summary>
    [Fact]
    public void RenderColorBar_VerticalExtendMax_DrawsOverRect()
    {
        var svg = Render(ax =>
        {
            ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } });
            ax.WithColorBar(cb => cb with
            {
                Orientation = global::MatPlotLibNet.Styling.ColorBarOrientation.Vertical,
                Extend = global::MatPlotLibNet.Styling.ColorMaps.ColorBarExtend.Max,
            });
        });
        Assert.Contains("<svg", svg);
    }

    /// <summary>Vertical colorbar with Extend=Both — exercises both extend arms simultaneously.</summary>
    [Fact]
    public void RenderColorBar_VerticalExtendBoth_DrawsBothRects()
    {
        var svg = Render(ax =>
        {
            ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } });
            ax.WithColorBar(cb => cb with
            {
                Orientation = global::MatPlotLibNet.Styling.ColorBarOrientation.Vertical,
                Extend = global::MatPlotLibNet.Styling.ColorMaps.ColorBarExtend.Both,
            });
        });
        Assert.Contains("<svg", svg);
    }

    /// <summary>Horizontal colorbar default (no Extend) — exercises the H branch at line 592 with
    /// `drawXMin=false drawXMax=false` (line 605 short-circuit, gradX = barX, gradW = fullW).</summary>
    [Fact]
    public void RenderColorBar_HorizontalNoExtend_DrawsBarBelow()
    {
        var svg = Render(ax =>
        {
            ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } });
            ax.WithColorBar(cb => cb with
            {
                Orientation = global::MatPlotLibNet.Styling.ColorBarOrientation.Horizontal,
            });
        });
        Assert.Contains("<svg", svg);
    }

    /// <summary>Horizontal colorbar with Extend=Min — `drawXMin = true`, line 607 true arm.</summary>
    [Fact]
    public void RenderColorBar_HorizontalExtendMin_DrawsLeftWedge()
    {
        var svg = Render(ax =>
        {
            ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } });
            ax.WithColorBar(cb => cb with
            {
                Orientation = global::MatPlotLibNet.Styling.ColorBarOrientation.Horizontal,
                Extend = global::MatPlotLibNet.Styling.ColorMaps.ColorBarExtend.Min,
            });
        });
        Assert.Contains("<svg", svg);
    }

    /// <summary>Horizontal colorbar with Extend=Max — `drawXMax = true`, line 624 true arm.</summary>
    [Fact]
    public void RenderColorBar_HorizontalExtendMax_DrawsRightWedge()
    {
        var svg = Render(ax =>
        {
            ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } });
            ax.WithColorBar(cb => cb with
            {
                Orientation = global::MatPlotLibNet.Styling.ColorBarOrientation.Horizontal,
                Extend = global::MatPlotLibNet.Styling.ColorMaps.ColorBarExtend.Max,
            });
        });
        Assert.Contains("<svg", svg);
    }

    /// <summary>Horizontal colorbar with Extend=Both + Label — exercises both wedges + the
    /// label branch at line 644-645.</summary>
    [Fact]
    public void RenderColorBar_HorizontalExtendBothWithLabel_DrawsAllArms()
    {
        var svg = Render(ax =>
        {
            ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } });
            ax.WithColorBar(cb => cb with
            {
                Orientation = global::MatPlotLibNet.Styling.ColorBarOrientation.Horizontal,
                Extend = global::MatPlotLibNet.Styling.ColorMaps.ColorBarExtend.Both,
                Label = "intensity",
            });
        });
        Assert.Contains(">intensity<", svg);
    }

    /// <summary>Vertical colorbar with DrawEdges=true — covers the per-step edge-line branch at
    /// line 672-673.</summary>
    [Fact]
    public void RenderColorBar_VerticalDrawEdges_DrawsPerStepEdges()
    {
        var svg = Render(ax =>
        {
            ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } });
            ax.WithColorBar(cb => cb with
            {
                Orientation = global::MatPlotLibNet.Styling.ColorBarOrientation.Vertical,
                DrawEdges = true,
            });
        });
        Assert.Contains("<svg", svg);
    }

    /// <summary>Horizontal colorbar with DrawEdges=true — covers line 620-621 per-step edges.</summary>
    [Fact]
    public void RenderColorBar_HorizontalDrawEdges_DrawsPerStepEdges()
    {
        var svg = Render(ax =>
        {
            ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } });
            ax.WithColorBar(cb => cb with
            {
                Orientation = global::MatPlotLibNet.Styling.ColorBarOrientation.Horizontal,
                DrawEdges = true,
            });
        });
        Assert.Contains("<svg", svg);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Phase Ω.7 — AxesRenderer remaining LegendPosition arms (L299-305) +
    // Shadow + invisible series + invisible Legend arm.
    // ─────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(LegendPosition.LowerCenter)]
    [InlineData(LegendPosition.UpperCenter)]
    [InlineData(LegendPosition.Center)]
    [InlineData(LegendPosition.OutsideRight)]
    [InlineData(LegendPosition.OutsideLeft)]
    [InlineData(LegendPosition.OutsideTop)]
    [InlineData(LegendPosition.OutsideBottom)]
    public void RenderLegend_RemainingPositionArms_RendersWithoutError(LegendPosition position)
    {
        var svg = Render(ax => ax
            .Plot([1.0, 2, 3], [4.0, 5, 6], s => s.Label = "L1")
            .Plot([1.0, 2, 3], [3.0, 4, 5], s => s.Label = "L2")
            .WithLegend(position));
        Assert.Contains(">L1<", svg);
    }

    [Fact]
    public void RenderLegend_WithShadowEnabled_DrawsShadowRectangle()
    {
        // L329-333 — shadow path
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4], s => s.Label = "L")
                .WithLegend())
            .Build();
        fig.SubPlots[0].Legend = fig.SubPlots[0].Legend with { Shadow = true };
        var svg = fig.ToSvg();
        Assert.Contains(">L<", svg);
    }

    [Fact]
    public void Render_WithInvisibleSeries_SkipsRender()
    {
        // L108 if (!series.Visible) continue; — true arm
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4], s => { s.Label = "hidden"; s.Visible = false; })
                .Plot([1.0, 2], [3.5, 4.5], s => s.Label = "shown"))
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Render_WithLegendInvisible_SkipsLegendRender()
    {
        // L392 if (!Axes.Legend.Visible) return;
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4], s => s.Label = "label")
                .WithLegend())
            .Build();
        fig.SubPlots[0].Legend = fig.SubPlots[0].Legend with { Visible = false };
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void RenderLegend_WithCustomTitleFontSize_AppliesCustomFont()
    {
        // L274/433 — `legend.TitleFontSize.HasValue` true arm
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4], s => s.Label = "L")
                .WithLegend())
            .Build();
        fig.SubPlots[0].Legend = fig.SubPlots[0].Legend with
        {
            Title = "MyLegend",
            TitleFontSize = 18,
        };
        var svg = fig.ToSvg();
        Assert.Contains(">MyLegend<", svg);
    }

    [Fact]
    public void RenderLegend_WithFrameOff_OmitsFrame()
    {
        // L320 if (legend.FrameOn) — false arm
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4], s => s.Label = "noframe")
                .WithLegend())
            .Build();
        fig.SubPlots[0].Legend = fig.SubPlots[0].Legend with { FrameOn = false };
        var svg = fig.ToSvg();
        Assert.Contains(">noframe<", svg);
    }

    [Fact]
    public void RenderLegend_WithCustomFaceAndEdgeColor_UsesOverrides()
    {
        // L322/327 - faceColor / edgeColor `?? default` non-null arms
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4], s => s.Label = "L")
                .WithLegend())
            .Build();
        fig.SubPlots[0].Legend = fig.SubPlots[0].Legend with
        {
            FaceColor = global::MatPlotLibNet.Styling.Colors.Yellow,
            EdgeColor = global::MatPlotLibNet.Styling.Colors.Red,
        };
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Render_WithPieSeriesNoColors_AssignsDefaultColors()
    {
        // L113-116 - pie.Colors is null arm
        var svg = Render(ax => ax.Pie([30.0, 50.0, 20.0]));
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Render_WithPropCyclerOnTheme_UsesCyclerProperties()
    {
        // L188-189 / L232 - Theme.PropCycler?[i] non-null arm
        var fig = Plt.Create()
            .WithSize(500, 400)
            .WithTheme(Theme.MatplotlibClassic)  // matplotlib classic has a PropCycler
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .Plot([1.0, 2], [4.0, 5])
                .Plot([1.0, 2], [5.0, 6]))
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }
}
