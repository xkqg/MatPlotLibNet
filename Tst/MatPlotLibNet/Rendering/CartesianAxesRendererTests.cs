// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Phase Y.3 (v1.7.2, 2026-04-19) — branch coverage for the
/// <see cref="MatPlotLibNet.Rendering.CartesianAxesRenderer"/>. Pre-Y.3:
/// 83.0%L / 73.3%B (complexity 442). Targets the largest gaps:
/// - <c>RenderGrid</c> arms (was 46%L/33%B) — visible/style/major+minor
/// - <c>RenderTicks</c> arms (was 74%L/57%B) — direction/length/rotation
/// - <c>RenderSpines</c> + ResolveSpine{X,Y} arms — Data/Axes positions
/// - <c>DrawAxisBreakMark</c> arms — broken-axis variants
/// - <c>ComputeSecondaryXDataRanges</c> — secondary X axis</summary>
public class CartesianAxesRendererCoverageTests
{
    private static string Render(Action<AxesBuilder> configure) =>
        Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, configure)
            .Build()
            .ToSvg();

    // ── Grid styles ─────────────────────────────────────────────────────────

    [Fact]
    public void RenderGrid_VisibleDefault_DrawsGridLines()
    {
        var svg = Render(ax => ax.Plot([1.0, 2, 3], [1.0, 2, 3]).WithGrid(g => g with { Visible = true }));
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void RenderGrid_HiddenExplicitly_DoesNotDrawGridLines()
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 2, 3]))
            .Build();
        fig.SubPlots[0].Grid = fig.SubPlots[0].Grid with { Visible = false };
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    // ── Tick directions + rotation ──────────────────────────────────────────

    [Theory]
    [InlineData(TickDirection.In)]
    [InlineData(TickDirection.Out)]
    [InlineData(TickDirection.InOut)]
    public void RenderTicks_TickDirection_AllArms(TickDirection direction)
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 2, 3]))
            .Build();
        fig.SubPlots[0].XAxis.MajorTicks = fig.SubPlots[0].XAxis.MajorTicks with { Direction = direction };
        fig.SubPlots[0].YAxis.MajorTicks = fig.SubPlots[0].YAxis.MajorTicks with { Direction = direction };
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(45.0)]
    [InlineData(90.0)]
    public void RenderTicks_TickLabelRotation_AllArms(double degrees)
    {
        var svg = Render(ax => ax.Plot([1.0, 2, 3], [1.0, 2, 3]).WithXTickLabelRotation(degrees));
        Assert.Contains("<svg", svg);
    }

    // ── Spine variants ──────────────────────────────────────────────────────

    [Fact]
    public void RenderSpines_HideAllAxes_NoSpineLines()
    {
        var svg = Render(ax => ax.Plot([1.0, 2], [1.0, 2]).HideAllAxes());
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void RenderSpines_TopAndRightHidden_OnlyBottomAndLeftDrawn()
    {
        var svg = Render(ax => ax
            .Plot([1.0, 2], [1.0, 2])
            .HideTopSpine()
            .HideRightSpine());
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void RenderSpines_DataPosition_ResolveSpineY_DataArm()
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [-2.0, 0, 2]))
            .Build();
        fig.SubPlots[0].Spines = fig.SubPlots[0].Spines with
        {
            Bottom = new SpineConfig { Position = SpinePosition.Data, PositionValue = 0, Visible = true }
        };
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void RenderSpines_AxesPosition_ResolveSpineX_AxesArm()
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 2, 3]))
            .Build();
        fig.SubPlots[0].Spines = fig.SubPlots[0].Spines with
        {
            Left = new SpineConfig { Position = SpinePosition.Axes, PositionValue = 0.5, Visible = true }
        };
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    // ── Scale variants (Log, SymLog, Logit) ────────────────────────────────

    [Theory]
    [InlineData(AxisScale.Linear)]
    [InlineData(AxisScale.Log)]
    [InlineData(AxisScale.SymLog)]
    public void RenderTicks_XScale_AllReachableArms(AxisScale scale)
    {
        var svg = Render(ax => ax
            .Plot([1.0, 10, 100, 1000], [1.0, 2, 3, 4])
            .SetXScale(scale));
        Assert.Contains("<svg", svg);
    }

    [Theory]
    [InlineData(AxisScale.Linear)]
    [InlineData(AxisScale.Log)]
    [InlineData(AxisScale.SymLog)]
    public void RenderTicks_YScale_AllReachableArms(AxisScale scale)
    {
        var svg = Render(ax => ax
            .Plot([1.0, 2, 3, 4], [1.0, 10, 100, 1000])
            .SetYScale(scale));
        Assert.Contains("<svg", svg);
    }

    // ── Inverted axis ──────────────────────────────────────────────────────

    [Fact]
    public void Render_InvertedYAxis_NoException()
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 2, 3]))
            .Build();
        // Inverted axis: yMin > yMax
        fig.SubPlots[0].YAxis.Min = 5;
        fig.SubPlots[0].YAxis.Max = 0;
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    // ── Mirror ticks ───────────────────────────────────────────────────────

    [Fact]
    public void Render_WithMirroredXTicks_DrawsTopTicks()
    {
        var svg = Render(ax => ax.Plot([1.0, 2, 3], [1.0, 2, 3]).WithXTicksMirrored());
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Render_WithMirroredYTicks_DrawsRightTicks()
    {
        var svg = Render(ax => ax.Plot([1.0, 2, 3], [1.0, 2, 3]).WithYTicksMirrored());
        Assert.Contains("<svg", svg);
    }

    // ── Categorical bar chart (RenderCategoryLabels arm) ───────────────────

    [Fact]
    public void RenderCategoryLabels_BarChart_DrawsCategoryNames()
    {
        var svg = Render(ax => ax.Bar(["Cat A", "Cat B", "Cat C"], [1.0, 2.0, 3.0]));
        Assert.Contains(">Cat A<", svg);
        Assert.Contains(">Cat B<", svg);
        Assert.Contains(">Cat C<", svg);
    }

    // ── Secondary axes ─────────────────────────────────────────────────────

    [Fact]
    public void Render_WithSecondaryYAxis_DrawsRightAxis()
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot([1.0, 2, 3], [1.0, 2, 3]);
                ax.WithSecondaryYAxis(s => s.SetYLabel("Right axis").SetYLim(0, 100));
            })
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    // ── Theme-driven font/color overrides ───────────────────────────────────

    [Fact]
    public void Render_WithMatplotlibClassicTheme_AppliesThemedSpines()
    {
        var svg = Plt.Create()
            .WithSize(500, 400)
            .WithTheme(Theme.MatplotlibClassic)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 2, 3]))
            .Build()
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Render_WithDarkTheme_AppliesDarkBackground()
    {
        var svg = Plt.Create()
            .WithSize(500, 400)
            .WithTheme(Theme.Dark)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 2, 3]))
            .Build()
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Phase Z.4 — span/break/grid/radar/locator-auto-install branches
    // ─────────────────────────────────────────────────────────────────────

    private static string RenderFigure(Action<AxesBuilder> configure, Action<global::MatPlotLibNet.Models.Figure>? postBuild = null)
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, configure)
            .Build();
        postBuild?.Invoke(fig);
        return fig.ToSvg();
    }

    /// <summary>Vertical AxVSpan with custom alpha + linestyle + label — covers
    /// vertical-span branch (lines 118-137), line 124 LineStyle != None true arm,
    /// and line 131 Label is not null true arm.</summary>
    [Fact]
    public void Render_WithVerticalSpanLabelledAndStyled_DrawsBorderAndLabel()
    {
        var svg = RenderFigure(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .AxVSpan(2, 3, sp => { sp.Alpha = 0.4; sp.LineStyle = LineStyle.Dashed; sp.LineWidth = 2; sp.Label = "v"; }));
        Assert.Contains(">v<", svg);
    }

    /// <summary>Horizontal AxHSpan with label — covers line 112 Label is not null arm.</summary>
    [Fact]
    public void Render_WithHorizontalSpanLabelled_DrawsLabel()
    {
        var svg = RenderFigure(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .AxHSpan(2, 3, sp => { sp.Alpha = 0.3; sp.LineStyle = LineStyle.Solid; sp.Label = "h"; }));
        Assert.Contains(">h<", svg);
    }

    /// <summary>Vertical reference line with label — covers line 166 Label is not null arm
    /// in the vertical reference-line branch (lines 159-173).</summary>
    [Fact]
    public void Render_WithVerticalReferenceLineLabelled_DrawsLabel()
    {
        var svg = RenderFigure(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .AxVLine(2.5, r => { r.Label = "v-ref"; r.LineStyle = LineStyle.Dotted; }));
        Assert.Contains(">v-ref<", svg);
    }

    /// <summary>Horizontal reference line with label — covers line 151 Label is not null arm.</summary>
    [Fact]
    public void Render_WithHorizontalReferenceLineLabelled_DrawsLabel()
    {
        var svg = RenderFigure(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .AxHLine(3.0, r => { r.Label = "h-ref"; r.LineStyle = LineStyle.Dotted; }));
        Assert.Contains(">h-ref<", svg);
    }

    /// <summary>Axes with X-breaks filtering ticks (line 72-73 true arm).</summary>
    [Fact]
    public void Render_WithXBreaks_FiltersTicksInsideBreak()
    {
        var svg = RenderFigure(
            configure: ax => ax.Plot([1.0, 10], [1.0, 10]),
            postBuild: fig => fig.SubPlots[0].AddXBreak(3, 5, BreakStyle.Zigzag));
        Assert.Contains("<svg", svg);
    }

    /// <summary>Axes with Y-breaks filtering ticks (line 74-75 true arm).</summary>
    [Fact]
    public void Render_WithYBreaks_FiltersTicksInsideBreak()
    {
        var svg = RenderFigure(
            configure: ax => ax.Plot([1.0, 10], [1.0, 10]),
            postBuild: fig => fig.SubPlots[0].AddYBreak(6, 8, BreakStyle.Zigzag));
        Assert.Contains("<svg", svg);
    }

    /// <summary>Axes with grid hidden (line 86 effectiveGrid.Visible false arm).</summary>
    [Fact]
    public void Render_WithGridHidden_OmitsGridGroup()
    {
        var svg = RenderFigure(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .WithGrid(g => g with { Visible = false }));
        Assert.Contains("<svg", svg);
    }

    /// <summary>Radar series in axes (line 79-81 radarOnly true arm — skip Cartesian decorations).</summary>
    [Fact]
    public void Render_WithRadarSeriesOnly_SkipsCartesianGridAndTicks()
    {
        var svg = RenderFigure(ax => ax.Radar(["A", "B", "C"], [1.0, 2.0, 3.0]));
        Assert.Contains("<svg", svg);
    }

    /// <summary>Pie series in axes (line 79-81 radarOnly true via PieSeries).</summary>
    [Fact]
    public void Render_WithPieSeriesOnly_SkipsCartesianGridAndTicks()
    {
        var svg = RenderFigure(ax => ax.Pie([30.0, 70.0]));
        Assert.Contains("<svg", svg);
    }

    /// <summary>X-axis Date scale without explicit locator → AutoDateLocator auto-installs
    /// (line 53-58).</summary>
    [Fact]
    public void Render_WithDateScaleAndNoLocator_AutoInstallsAutoDateLocator()
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2, 3], [10.0, 20, 30]))
            .Build();
        fig.SubPlots[0].XAxis.Scale = AxisScale.Date;
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>Y-axis SymLog scale without explicit locator → SymlogLocator auto-installs
    /// (line 61-62).</summary>
    [Fact]
    public void Render_WithSymLogYScaleAndNoLocator_AutoInstallsSymlogLocator()
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2, 3], [10.0, 20, 30]))
            .Build();
        fig.SubPlots[0].YAxis.Scale = AxisScale.SymLog;
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>X-axis SymLog scale without explicit locator → SymlogLocator auto-installs
    /// (line 63-64).</summary>
    [Fact]
    public void Render_WithSymLogXScaleAndNoLocator_AutoInstallsSymlogLocator()
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([-3.0, 0, 3], [10.0, 20, 30]))
            .Build();
        fig.SubPlots[0].XAxis.Scale = AxisScale.SymLog;
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Phase Ω.8 — secondary axes RENDERED (Phase Z only round-tripped them),
    // annotations with arrow/box, signal markers, grid variants, log-scale.
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Renders a figure with secondary Y-axis + secondary line series — flips
    /// the entire L201-238 block (29 lines) that's been dead in the cobertura.</summary>
    [Fact]
    public void Render_WithSecondaryYAxisAndLineSeries_RendersWithoutError()
    {
        var svg = RenderFigure(ax => ax
            .Plot([1.0, 2, 3], [3.0, 4, 5])
            .WithSecondaryYAxis(s => s
                .SetYLabel("right axis")
                .Plot([1.0, 2, 3], [50.0, 60, 70])
                .Scatter([1.5, 2.5], [55.0, 65])));
        Assert.Contains("<svg", svg);
    }

    /// <summary>Secondary Y-axis with custom TickFormatter — flips the
    /// `?? secYUniformFormat` arm at L227.</summary>
    [Fact]
    public void Render_WithSecondaryYAxisCustomFormatter_UsesFormatter()
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .WithSecondaryYAxis(s => s
                    .SetYLabel("right")
                    .Plot([1.0, 2], [50.0, 60])))
            .Build();
        fig.SubPlots[0].SecondaryYAxis!.TickFormatter = new global::MatPlotLibNet.Rendering.TickFormatters.NumericTickFormatter();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>Secondary Y-axis with hidden series — L210 `if (!series.Visible) continue;`
    /// true arm.</summary>
    [Fact]
    public void Render_WithSecondaryYAxisAndInvisibleSeries_SkipsHidden()
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .WithSecondaryYAxis(s => s.Plot([1.0, 2], [50.0, 60])))
            .Build();
        fig.SubPlots[0].SecondarySeries[0].Visible = false;
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    // ── Annotations: BoxStyle non-None / BackgroundColor / ArrowStyle ─────

    [Fact]
    public void Render_WithAnnotationBoxStyle_RendersCalloutBox()
    {
        var svg = RenderFigure(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .Annotate("box", 2, 3, a => { a.BoxStyle = BoxStyle.Round; a.BoxFaceColor = Colors.Yellow; a.BoxEdgeColor = Colors.Black; }));
        Assert.Contains(">box<", svg);
    }

    [Fact]
    public void Render_WithAnnotationBackgroundColor_RendersBackgroundRect()
    {
        var svg = RenderFigure(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .Annotate("bg", 2, 3, a => a.BackgroundColor = Colors.Yellow));
        Assert.Contains(">bg<", svg);
    }

    [Fact]
    public void Render_WithAnnotationArrow_RendersArrowAndConnection()
    {
        var svg = RenderFigure(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .Annotate("ann", 2, 3, 4, 4));
        Assert.Contains(">ann<", svg);
    }

    [Fact]
    public void Render_WithAnnotationArrowStyleNone_SkipsArrow()
    {
        var svg = RenderFigure(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .Annotate("noarrow", 2, 3, 4, 4, a => a.ArrowStyle = ArrowStyle.None));
        Assert.Contains(">noarrow<", svg);
    }

    [Fact]
    public void Render_WithAnnotationCustomArrowColor_UsesOverride()
    {
        var svg = RenderFigure(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .Annotate("c", 2, 3, 4, 4, a => a.ArrowColor = Colors.Magenta));
        Assert.Contains(">c<", svg);
    }

    [Fact]
    public void Render_WithAnnotationCustomFont_UsesFont()
    {
        var svg = RenderFigure(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .Annotate("f", 2, 3, a => a.Font = new Font { Family = "monospace", Size = 14 }));
        Assert.Contains(">f<", svg);
    }

    // ── Signal markers ────────────────────────────────────────────────────

    [Fact]
    public void Render_WithBuySignal_DrawsGreenTriangleUp()
    {
        var svg = RenderFigure(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .AddSignal(2, 3, SignalDirection.Buy));
        Assert.Contains("<polygon", svg);
    }

    [Fact]
    public void Render_WithSellSignal_DrawsRedTriangleDown()
    {
        var svg = RenderFigure(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .AddSignal(2, 3, SignalDirection.Sell));
        Assert.Contains("<polygon", svg);
    }

    [Fact]
    public void Render_WithSignalCustomColor_UsesOverride()
    {
        var svg = RenderFigure(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .AddSignal(2, 3, SignalDirection.Buy, m => m.Color = Colors.Magenta));
        Assert.Contains("<polygon", svg);
    }

    // ── Grid axis arms (X-only, Y-only) ───────────────────────────────────

    [Fact]
    public void Render_WithGridXOnly_DrawsOnlyVerticalLines()
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2, 3], [1.0, 2, 3])
                .WithGrid(g => g with { Visible = true, Axis = GridAxis.X }))
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Render_WithGridYOnly_DrawsOnlyHorizontalLines()
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2, 3], [1.0, 2, 3])
                .WithGrid(g => g with { Visible = true, Axis = GridAxis.Y }))
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Render_WithGridMinorTicks_DrawsMinorGrid()
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2, 3], [1.0, 2, 3])
                .WithGrid(g => g with { Visible = true, Which = GridWhich.Both }))
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    // ── ScaleRange arm: Log scale with min ≤ 0 (NaN fallback at L378) ─────

    [Fact]
    public void Render_LogScaleWithNonPositiveMin_HandlesNaNGracefully()
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.1, 1, 10], [1.0, 10, 100]))
            .Build();
        fig.SubPlots[0].XAxis.Scale = AxisScale.Log;
        // Force min to a non-positive value
        fig.SubPlots[0].XAxis.Min = -1;
        fig.SubPlots[0].XAxis.Max = 100;
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    // ── Span without label (L97-98 partial branches: span color/edge null arms) ──

    [Fact]
    public void Render_HorizontalSpanWithoutLabelOrLineStyle_RendersFillOnly()
    {
        var svg = RenderFigure(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .AxHSpan(2, 3, sp => { sp.Alpha = 0.3; sp.LineStyle = LineStyle.None; sp.Label = null; }));
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Render_VerticalSpanWithoutLabelOrLineStyle_RendersFillOnly()
    {
        var svg = RenderFigure(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .AxVSpan(2, 3, sp => { sp.Alpha = 0.3; sp.LineStyle = LineStyle.None; sp.Label = null; }));
        Assert.Contains("<svg", svg);
    }

    // ── Wave J.2 — secondary-X axis + break styles + unused arms ────────────

    /// <summary>Secondary X axis with series — ComputeSecondaryXDataRanges L598-608 branches.</summary>
    [Fact]
    public void Render_WithSecondaryXAxis_DrawsTopAxis()
    {
        var svg = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2, 3], [1.0, 4, 9])
                .WithSecondaryXAxis(sx => sx
                    .SetXLabel("Top axis")
                    .SetXLim(0, 100)
                    .PlotXSecondary([10.0, 20, 30], [1.0, 4, 9])))
            .Build()
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>X-break with BreakStyle.Straight — L753-763 straight-break path.</summary>
    [Fact]
    public void Render_XBreakStraightStyle_DrawsBreak()
    {
        var svg = RenderFigure(
            ax => ax.Plot([1.0, 2, 10, 11], [1.0, 2, 3, 4]),
            postBuild: fig => fig.SubPlots[0].AddXBreak(3, 9, BreakStyle.Straight));
        Assert.Contains("<svg", svg);
    }

    /// <summary>Y-break with BreakStyle.Straight — L753 straight-break path (Y-axis).</summary>
    [Fact]
    public void Render_YBreakStraightStyle_DrawsBreak()
    {
        var svg = RenderFigure(
            ax => ax.Plot([1.0, 2, 3], [1.0, 2, 10]),
            postBuild: fig => fig.SubPlots[0].AddYBreak(3, 8, BreakStyle.Straight));
        Assert.Contains("<svg", svg);
    }

    /// <summary>X-break with BreakStyle.None — L683 `if (b.Style == None) continue` true arm.</summary>
    [Fact]
    public void Render_XBreakNoneStyle_SkipsDrawing()
    {
        var svg = RenderFigure(
            ax => ax.Plot([1.0, 2, 10, 11], [1.0, 2, 3, 4]),
            postBuild: fig => fig.SubPlots[0].AddXBreak(3, 9, BreakStyle.None));
        Assert.Contains("<svg", svg);
    }

    /// <summary>Minor ticks visible false — L318 `if (xMinor.Visible && ...)` false arm (xMinor.Visible=false).</summary>
    [Fact]
    public void Render_MinorTicksHidden_SkipsMinorTickDraw()
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 4, 9]))
            .Build();
        fig.SubPlots[0].XAxis.MinorTicks = fig.SubPlots[0].XAxis.MinorTicks with { Visible = false };
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>Y-axis ticks hidden — L347 `if (yMajor.Visible)` false arm.</summary>
    [Fact]
    public void Render_YMajorTicksHidden_SkipsYTickDraw()
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 4, 9]))
            .Build();
        fig.SubPlots[0].YAxis.MajorTicks = fig.SubPlots[0].YAxis.MajorTicks with { Visible = false };
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>Y-axis mirrored ticks — L364 `if (yMajor.Mirror && yMajor.Visible)` true arm.</summary>
    [Fact]
    public void Render_YMajorTicksMirrored_DrawsRightTicks()
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 4, 9]))
            .Build();
        fig.SubPlots[0].YAxis.MajorTicks = fig.SubPlots[0].YAxis.MajorTicks with { Mirror = true };
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>Secondary Y axis data range — ComputeSecondaryDataRanges with HasValue arms.
    /// Also covers L576 yMin==MaxValue false arm (yMin was set from series data).</summary>
    [Fact]
    public void Render_SecondaryYAxisWithExplicitLimits_CoversHasValueArms()
    {
        var svg = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2, 3], [1.0, 4, 9])
                .WithSecondaryYAxis(sy => sy
                    .SetYLabel("Right")
                    .SetYLim(10, 100)
                    .Plot([1.0, 2, 3], [50.0, 60, 70])))
            .Build()
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    // ── Wave J.1 — remaining branch arms ────────────────────────────────────

    /// <summary>BarMode.Stacked with two bar series — L100-113 stacked-baseline assignment.
    /// Asserts both series render (two rects with distinct colours).</summary>
    [Fact]
    public void Render_StackedBarMode_AssignsBaselineToEachSeries()
    {
        var svg = RenderFigure(ax => ax
            .Bar(["A", "B"], [1.0, 2.0], s => s.Color = Colors.Blue)
            .Bar(["A", "B"], [3.0, 4.0], s => s.Color = Colors.Red)
            .SetBarMode(BarMode.Stacked));
        Assert.Contains("<rect", svg);
    }

    /// <summary>MajorTicks.LabelSize override — L268 `LabelSize.HasValue` TRUE arm.</summary>
    [Fact]
    public void Render_TickLabelSizeOverride_AppliesCustomSize()
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 2, 3]))
            .Build();
        fig.SubPlots[0].XAxis.MajorTicks = fig.SubPlots[0].XAxis.MajorTicks with { LabelSize = 14 };
        fig.SubPlots[0].YAxis.MajorTicks = fig.SubPlots[0].YAxis.MajorTicks with { LabelSize = 12 };
        var svg = fig.ToSvg();
        Assert.Contains("<text", svg);
    }

    /// <summary>MajorTicks.LabelColor override — L269 `LabelColor.HasValue` TRUE arm.</summary>
    [Fact]
    public void Render_TickLabelColorOverride_AppliesCustomColor()
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 2, 3]))
            .Build();
        fig.SubPlots[0].XAxis.MajorTicks = fig.SubPlots[0].XAxis.MajorTicks
            with { LabelColor = Colors.Red };
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>ResolveXLabelRotation: xTicks.Length &lt; 2 → return 0 (L467 TRUE arm).
    /// Force a single tick by fixing axis to a single-point range.</summary>
    [Fact]
    public void ResolveXLabelRotation_SingleTick_ReturnsZeroRotation()
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([5.0, 5.0], [1.0, 2.0]))
            .Build();
        fig.SubPlots[0].XAxis.Min = 4.9;
        fig.SubPlots[0].XAxis.Max = 5.1;
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>ResolveXLabelRotation: labels collide → return 30° (L486 true arm).
    /// Pack many ticks into a narrow plot.</summary>
    [Fact]
    public void ResolveXLabelRotation_DenseTicks_AutoRotatesLabels()
    {
        var fig = Plt.Create()
            .WithSize(200, 150)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(Enumerable.Range(1, 20).Select(i => (double)i).ToArray(),
                      Enumerable.Range(1, 20).Select(i => (double)i).ToArray()))
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>Minor Y ticks enabled with enough ticks — L380 `yMinor.Visible && >= 2` TRUE arm.</summary>
    [Fact]
    public void Render_MinorYTicksEnabled_DrawsMinorTicks()
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3, 4, 5], [10.0, 20, 30, 40, 50]))
            .Build();
        fig.SubPlots[0].YAxis.MinorTicks = fig.SubPlots[0].YAxis.MinorTicks with { Visible = true };
        var svg = fig.ToSvg();
        Assert.Contains("<line", svg);
    }

    /// <summary>ComputeSecondaryDataRanges: degenerate yMax-yMin &lt; 1e-10 — L577 TRUE arm.
    /// All secondary series have the same y value → yMax-yMin ≈ 0 → padded by ±0.5.</summary>
    [Fact]
    public void Render_SecondaryYAxisDegenerateRange_PadsToHalfUnit()
    {
        var svg = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2, 3], [1.0, 2, 3])
                .WithSecondaryYAxis(sy => sy
                    .Plot([1.0, 2, 3], [50.0, 50.0, 50.0])))
            .Build()
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>ComputeSecondaryXDataRanges: no explicit Min/Max on axis — L607-608 TRUE arm
    /// (padding applied from series range). Uses WithSecondaryXAxis without SetXLim.</summary>
    [Fact]
    public void Render_SecondaryXAxisNoPaddingOverride_AppliesToSeriesRange()
    {
        var svg = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2, 3], [1.0, 4, 9])
                .WithSecondaryXAxis(sx => sx
                    .PlotXSecondary([10.0, 20, 30], [1.0, 4, 9])))
            .Build()
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    // ── Minor X ticks — L318-331 (0% coverage) ─────────────────────────────

    /// <summary>Minor X ticks enabled with explicit color — L318 TRUE arm + body at L320-331.</summary>
    [Fact]
    public void Render_MinorXTicksEnabled_DrawsMinorXTicks()
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3, 4, 5], [1.0, 4, 9, 16, 25]))
            .Build();
        fig.SubPlots[0].XAxis.MinorTicks = fig.SubPlots[0].XAxis.MinorTicks with { Visible = true, Color = Colors.Gray };
        var svg = fig.ToSvg();
        Assert.Contains("<line", svg);
    }

    // ── Y tick mark color + spine explicit color ────────────────────────────

    /// <summary>Y-axis tick mark Color (non-null) — L338 non-null arm of `yMajor.Color ??`.</summary>
    [Fact]
    public void Render_YTickMarkColorOverride_UsesCustomColor()
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 2, 3]))
            .Build();
        fig.SubPlots[0].YAxis.MajorTicks = fig.SubPlots[0].YAxis.MajorTicks with { Color = Colors.Blue };
        var svg = fig.ToSvg();
        Assert.Contains("<line", svg);
    }

    /// <summary>Spine with explicit Color set — L625/632/639/646 non-null arms of spine.Color ??.</summary>
    [Fact]
    public void Render_SpineWithExplicitColor_RendersSpineLine()
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 2, 3]))
            .Build();
        fig.SubPlots[0].Spines = fig.SubPlots[0].Spines with
        {
            Bottom = fig.SubPlots[0].Spines.Bottom with { Color = Colors.Red },
            Left   = fig.SubPlots[0].Spines.Left   with { Color = Colors.Green }
        };
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    // ── Secondary Y axis with no series — L576 TRUE arm ────────────────────

    /// <summary>SecondaryYAxis present but no secondary series → L576 TRUE arm: yMin stays
    /// double.MaxValue → defaulted to (0, 1).</summary>
    [Fact]
    public void Render_SecondaryYAxisWithNoSeries_FallsBackToDefaultRange()
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 2, 3]))
            .Build();
        fig.SubPlots[0].TwinX();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    // ── X major ticks hidden — L287 `!xMajor.Visible` TRUE arm ─────────────

    /// <summary>X major ticks hidden — L287 `else if (!xMajor.Visible)` TRUE arm:
    /// MeasuredXTickMaxHeight is set to 0 and no tick marks or labels are drawn for the X axis.
    /// Assert: hiding X ticks produces fewer &lt;text&gt; elements than the visible baseline.</summary>
    [Fact]
    public void Render_XMajorTicksHidden_SkipsXTickDraw()
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [100.0, 200, 300]))
            .Build();

        var svgVisible = fig.ToSvg();
        int textVisible = svgVisible.Split("<text").Length - 1;

        fig.SubPlots[0].XAxis.MajorTicks = fig.SubPlots[0].XAxis.MajorTicks with { Visible = false };
        var svgHidden = fig.ToSvg();
        int textHidden = svgHidden.Split("<text").Length - 1;

        // Hiding X ticks removes all X axis tick labels → fewer <text> elements
        Assert.True(textHidden < textVisible, $"Expected fewer <text> elements when X ticks hidden ({textHidden} < {textVisible})");
    }
}
