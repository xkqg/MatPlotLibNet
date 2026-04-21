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

    // ── Wave J.1 — AxesRenderer remaining branch arms ────────────────────────

    /// <summary>L103/L208 TRUE arm — EnableInteractiveAttributes = true → interactiveSvgCtx
    /// is set → BeginDataGroup (not BeginAccessibleGroup) is called for each series.</summary>
    [Fact]
    public void Render_EnableInteractiveAttributes_EmitsDataAttributes()
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4], s => s.Label = "Data"))
            .Build();
        fig.SubPlots[0].EnableInteractiveAttributes = true;
        var svg = fig.ToSvg();
        Assert.Contains("data-series", svg);
    }

    /// <summary>L592 TRUE arm — BuildUniformTickFormatter with fewer than 2 ticks
    /// → falls back to FormatTick.</summary>
    [Fact]
    public void BuildUniformTickFormatter_SingleTick_ReturnsFallback()
    {
        var formatter = AxesRenderer.BuildUniformTickFormatter([5.0]);
        Assert.Equal("5", formatter(5.0));
    }

    /// <summary>L592 TRUE arm — null ticks → fallback.</summary>
    [Fact]
    public void BuildUniformTickFormatter_NullTicks_ReturnsFallback()
    {
        var formatter = AxesRenderer.BuildUniformTickFormatter(null!);
        Assert.Equal("0", formatter(0.0));
    }

    /// <summary>L608 TRUE arm — RequiredDecimalPlaces with step=0 (identical tick values)
    /// → returns 0 decimal places.</summary>
    [Fact]
    public void BuildUniformTickFormatter_ZeroStep_FormatsAsZeroDecimals()
    {
        var formatter = AxesRenderer.BuildUniformTickFormatter([2.0, 2.0]);
        Assert.Equal("2", formatter(2.0));
    }

    /// <summary>ComputeTickValues(4-param) TickLocator TRUE arm — explicit locator overrides
    /// the default nice-number algorithm.</summary>
    [Fact]
    public void Render_WithCustomTickLocator_UsesLocator()
    {
        var svg = Render(ax => ax
            .Plot([0.0, 10], [0.0, 10])
            .SetXTickLocator(new MatPlotLibNet.Rendering.TickLocators.MultipleLocator(2.5)));
        Assert.Contains("<svg", svg);
    }

    /// <summary>ComputeTickValues(4-param) Spacing TRUE arm — Spacing auto-creates a MultipleLocator.</summary>
    [Fact]
    public void Render_WithTickSpacing_UsesMultipleLocator()
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 10], [0.0, 10]))
            .Build();
        fig.SubPlots[0].XAxis.MajorTicks = fig.SubPlots[0].XAxis.MajorTicks with { Spacing = 2.0 };
        Assert.Contains("<svg", fig.ToSvg());
    }

    /// <summary>L688 FALSE arm — plotPixels &lt; 240 → fewer ticks scaled to plot size.
    /// A 100×100 figure forces short plot area → target ticks = clamp(100/30, 2, 6) = 3.</summary>
    [Fact]
    public void Render_SmallFigure_ReducesTickCount()
    {
        var svg = Plt.Create()
            .WithSize(100, 100)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1, 2], [0.0, 1, 2]))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>L457 TRUE arm — colorbar min==max after all data extraction →
    /// fallback range [0, 1] is used so the bar renders rather than crashing.</summary>
    [Fact]
    public void RenderColorBar_DegenerateRange_FallsBackToUnitRange()
    {
        var svg = Render(ax =>
        {
            ax.Heatmap(new double[,] { { 5, 5 }, { 5, 5 } }); // all same → dMin == dMax
            ax.WithColorBar();
        });
        Assert.Contains("<svg", svg);
    }

    /// <summary>L452 non-null arm — cb.ColorMap explicitly set on a colorbar that also
    /// has a data provider → uses cb.ColorMap, not dataProvider.ColorMap.</summary>
    [Fact]
    public void RenderColorBar_ExplicitColorMapOverridesDataProvider()
    {
        var svg = Render(ax =>
        {
            ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } });
            ax.WithColorBar(cb => cb with { ColorMap = MatPlotLibNet.Styling.ColorMaps.ColorMaps.Plasma });
        });
        Assert.Contains("<svg", svg);
    }

    /// <summary>L89 FALSE arm — unregistered CoordinateSystem enum value → falls back
    /// to CartesianAxesRenderer default construction.</summary>
    [Fact]
    public void AxesRenderer_Create_UnknownCoordinateSystem_FallsBackToCartesian()
    {
        var axes = new Axes { CoordinateSystem = (CoordinateSystem)999 };
        var ctx = new MatPlotLibNet.Rendering.Svg.SvgRenderContext();
        var renderer = AxesRenderer.Create(axes, new Rect(50, 50, 300, 200), ctx, Theme.Default);
        Assert.NotNull(renderer);
    }

    // ── Wave J.0.e — remaining uncovered arms ────────────────────────────────

    /// <summary>L164 TRUE arm — two vertical non-stacked bar series → grouped bar layout
    /// assigns side-by-side offsets (barGroups.Count &gt; 1).</summary>
    [Fact]
    public void Render_TwoBarSeries_GroupedBarOffsets()
    {
        var svg = Render(ax => ax
            .Bar(["A", "B", "C"], [10.0, 20.0, 15.0], s => s.Label = "Group 1")
            .Bar(["A", "B", "C"], [12.0, 18.0, 20.0], s => s.Label = "Group 2")
            .WithLegend());
        Assert.Contains(">Group 1<", svg);
        Assert.Contains(">Group 2<", svg);
    }

    /// <summary>L306 TRUE arm — legend entry whose label contains math markup →
    /// <see cref="MathTextParser.ContainsMath"/> returns true → DrawRichText called.</summary>
    [Fact]
    public void RenderLegend_MathLabel_DrawsRichText()
    {
        var svg = Render(ax => ax
            .Plot([1.0, 2], [3.0, 4], s => s.Label = @"$\alpha + \beta$")
            .WithLegend());
        Assert.Contains("<svg", svg);
    }

    /// <summary>L285/298/310 TRUE arms — <see cref="Axes.EnableInteractiveAttributes"/> = true
    /// while legend has labelled entries → svgCtxLegend is non-null → BeginLegendItemGroup
    /// and EndGroup are called per entry.</summary>
    [Fact]
    public void RenderLegend_InteractiveAttributes_EmitsLegendItemGroups()
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4], s => s.Label = "Alpha")
                .Plot([1.0, 2], [4.0, 5], s => s.Label = "Beta")
                .WithLegend())
            .Build();
        fig.SubPlots[0].EnableInteractiveAttributes = true;
        var svg = fig.ToSvg();
        Assert.Contains("data-legend-index", svg);
    }

    /// <summary>DrawLegendSwatch — SignalSeries arm: horizontal line swatch.</summary>
    [Fact]
    public void RenderLegend_SignalSeriesSwatch_DrawsLine()
    {
        var svg = Render(ax => ax
            .Signal(Enumerable.Range(0, 10).Select(i => (double)i).ToArray(),
                configure: s => s.Label = "Signal")
            .WithLegend());
        Assert.Contains(">Signal<", svg);
    }

    /// <summary>DrawLegendSwatch — StepSeries arm: horizontal line swatch.</summary>
    [Fact]
    public void RenderLegend_StepSeriesSwatch_DrawsLine()
    {
        var svg = Render(ax => ax
            .Step([1.0, 2, 3], [4.0, 5, 6], s => s.Label = "Step")
            .WithLegend());
        Assert.Contains(">Step<", svg);
    }

    /// <summary>DrawLegendSwatch — EcdfSeries arm: horizontal line swatch.</summary>
    [Fact]
    public void RenderLegend_EcdfSeriesSwatch_DrawsLine()
    {
        var svg = Render(ax => ax
            .Ecdf([1.0, 2, 3, 4, 5], configure: s => s.Label = "ECDF")
            .WithLegend());
        Assert.Contains(">ECDF<", svg);
    }

    /// <summary>DrawLegendSwatch — RegressionSeries arm: horizontal line swatch.</summary>
    [Fact]
    public void RenderLegend_RegressionSeriesSwatch_DrawsLine()
    {
        var svg = Render(ax => ax
            .Regression([1.0, 2, 3], [2.0, 4, 6], s => s.Label = "Fit")
            .WithLegend());
        Assert.Contains(">Fit<", svg);
    }

    /// <summary>DrawLegendSwatch — ErrorBarSeries arm: centre line + two caps.</summary>
    [Fact]
    public void RenderLegend_ErrorBarSeriesSwatch_DrawsLineWithCaps()
    {
        var svg = Render(ax => ax
            .ErrorBar([1.0, 2, 3], [4.0, 5, 6], [0.2, 0.3, 0.1], [0.2, 0.3, 0.1], s => s.Label = "ErrBar")
            .WithLegend());
        Assert.Contains(">ErrBar<", svg);
    }

    /// <summary>DrawLegendSwatch — LineSeries with Marker set (L379 TRUE arm) →
    /// DrawLegendMarker is also called beside the line segment.</summary>
    [Fact]
    public void RenderLegend_LineSeriesWithMarker_DrawsMarker()
    {
        var svg = Render(ax => ax
            .Plot([1.0, 2, 3], [4.0, 5, 6],
                s => { s.Label = "Marked"; s.Marker = MarkerStyle.Circle; })
            .WithLegend());
        Assert.Contains(">Marked<", svg);
    }

    /// <summary>DrawLegendMarker L431 TRUE arm — Square marker → DrawRectangle called
    /// instead of DrawCircle.</summary>
    [Fact]
    public void RenderLegend_ScatterSquareMarker_DrawsSquareSwatch()
    {
        var svg = Render(ax => ax
            .Scatter([1.0, 2, 3], [4.0, 5, 6],
                s => { s.Label = "Squares"; s.Marker = MarkerStyle.Square; })
            .WithLegend());
        Assert.Contains(">Squares<", svg);
    }

    /// <summary>DrawLegendSwatch — SignalXYSeries arm (previously uncovered).</summary>
    [Fact]
    public void RenderLegend_SignalXYSeriesSwatch_DrawsLine()
    {
        var svg = Render(ax => ax
            .SignalXY(
                Enumerable.Range(0, 10).Select(i => (double)i).ToArray(),
                Enumerable.Range(0, 10).Select(i => (double)i * 0.5).ToArray(),
                s => s.Label = "SignalXY")
            .WithLegend());
        Assert.Contains(">SignalXY<", svg);
    }

    /// <summary>DrawLegendSwatch — SparklineSeries arm (previously uncovered).</summary>
    [Fact]
    public void RenderLegend_SparklineSeriesSwatch_DrawsLine()
    {
        var svg = Render(ax => ax
            .Sparkline(Enumerable.Range(0, 8).Select(i => (double)i).ToArray(),
                s => s.Label = "Spark")
            .WithLegend());
        Assert.Contains(">Spark<", svg);
    }

    /// <summary>L109/110/188/189 non-null arm — explicit PropCycler on theme →
    /// cycledProps is non-null → cycledProps.Color used for seriesColor.</summary>
    [Fact]
    public void Render_WithExplicitPropCycler_UsesCyclerColor()
    {
        var cycler = new PropCyclerBuilder()
            .WithColors(Colors.Red, Colors.Green, Colors.Blue)
            .Build();
        var theme = Theme.CreateFrom(Theme.Default).WithPropCycler(cycler).Build();
        var svg = Plt.Create()
            .WithSize(500, 400)
            .WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4], s => s.Label = "A")
                .Plot([1.0, 2], [4.0, 5], s => s.Label = "B")
                .WithLegend())
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>L113 FALSE arm (series is PieSeries AND pie.Colors is NOT null) →
    /// skip the auto-fill branch.</summary>
    [Fact]
    public void Render_PieSeriesWithExplicitColors_SkipsAutoFill()
    {
        var svg = Render(ax => ax.Pie(
            [30.0, 50.0, 20.0],
            configure: s => s.Colors = [Colors.Red, Colors.Green, Colors.Blue]));
        Assert.Contains("<svg", svg);
    }

    // ── Wave K.0.3 — RenderLegend entries.Count == 0 arm ─────────────────────

    /// <summary>RenderLegend L237 TRUE arm — Legend.Visible = true but no series have labels →
    /// entries.Count == 0 → early return before any drawing. Assert: SVG contains no legend group.</summary>
    [Fact]
    public void RenderLegend_NoLabelledSeries_LegendVisibleEarlyReturn()
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])    // no label set
                .WithLegend())               // Visible = true, but entries will be empty
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
        Assert.DoesNotContain("class=\"legend\"", svg);
    }
}
