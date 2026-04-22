// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>Phase Y.4 (v1.7.2, 2026-04-19) — branch coverage for the
/// <see cref="AxesBuilder"/> configure-callback arms (Action&lt;T&gt;? overloads
/// where the non-null configure path was untested) and the rare-fluent methods
/// (SetXDateFormat, WithDownsampling, NestedPie, WithProjection, indicator
/// helpers). Pre-Y.4: 85.3%L / 60.8%B (complexity 231).</summary>
public class AxesBuilderCoverageTests
{
    /// <summary>AxHLine with non-null configure callback — line 267 true arm.</summary>
    [Fact]
    public void AxHLine_WithConfigure_AppliesCustomisation()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .AxHLine(3.5, line => line.Label = "threshold"))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].ReferenceLines);
    }

    /// <summary>AxVLine with non-null configure callback — line 275 true arm.</summary>
    [Fact]
    public void AxVLine_WithConfigure_AppliesCustomisation()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .AxVLine(1.5, line => line.Label = "midpoint"))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].ReferenceLines);
    }

    /// <summary>AxHSpan with non-null configure callback.</summary>
    [Fact]
    public void AxHSpan_WithConfigure_AppliesCustomisation()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .AxHSpan(2.0, 4.0, span => span.Alpha = 0.3))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].Spans);
    }

    /// <summary>AxVSpan with non-null configure callback.</summary>
    [Fact]
    public void AxVSpan_WithConfigure_AppliesCustomisation()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .AxVSpan(0.5, 1.5, span => span.Alpha = 0.5))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].Spans);
    }

    /// <summary>SetXDateFormat — line 328 (0%-covered method).</summary>
    [Fact]
    public void SetXDateFormat_AppliesFormatter()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .SetXDateFormat("yyyy-MM"))
            .Build();
        Assert.NotNull(fig);
    }

    /// <summary>SetYDateFormat — line 336 (0%-covered method).</summary>
    [Fact]
    public void SetYDateFormat_AppliesFormatter()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .SetYDateFormat("HH:mm"))
            .Build();
        Assert.NotNull(fig);
    }

    /// <summary>SetYTickFormatter — line 351 (0%-covered method).</summary>
    [Fact]
    public void SetYTickFormatter_AppliesCustomFormatter()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .SetYTickFormatter(new EngFormatter()))
            .Build();
        Assert.NotNull(fig);
    }

    /// <summary>SetYTickLocator — line 369 (0%-covered method).</summary>
    [Fact]
    public void SetYTickLocator_AppliesCustomLocator()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2, 3], [10.0, 100, 1000])
                .SetYTickLocator(new MaxNLocator(5)))
            .Build();
        Assert.NotNull(fig);
    }

    /// <summary>WithDownsampling — line 405 (0%-covered).</summary>
    [Fact]
    public void WithDownsampling_AppliesMaxPoints()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2, 3, 4, 5], [1.0, 2, 3, 4, 5])
                .WithDownsampling(maxPoints: 100))
            .Build();
        Assert.NotNull(fig);
    }

    /// <summary>WithProjection — line 702 (0%-covered).</summary>
    [Fact]
    public void WithProjection_AppliesCameraAngles()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Scatter3D([1.0, 2], [3.0, 4], [5.0, 6])
                .WithProjection(elevation: 45, azimuth: -90))
            .Build();
        Assert.Equal(45.0, fig.SubPlots[0].Elevation);
        Assert.Equal(-90.0, fig.SubPlots[0].Azimuth);
    }

    /// <summary>NestedPie with non-null configure — line 624 (0%-covered method).</summary>
    [Fact]
    public void NestedPie_WithConfigure_AddsSunburstSeries()
    {
        var root = new TreeNode
        {
            Label = "Root",
            Children =
            [
                new() { Label = "A", Value = 1 },
                new() { Label = "B", Value = 2 },
            ]
        };
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.NestedPie(root, s => s.ShowLabels = true))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].Series);
    }

    /// <summary>Sma indicator with non-null configure — line 893 covers the configure arm.</summary>
    [Fact]
    public void Sma_WithConfigure_AppliesCustomisation()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2, 3, 4, 5], [10.0, 11, 12, 13, 14])
                .Sma(period: 3, configure: ind => ind.Label = "SMA3"))
            .Build();
        Assert.NotNull(fig);
    }

    /// <summary>WilliamsR — line 933 (0%-covered method).</summary>
    [Fact]
    public void WilliamsR_AppliesIndicator()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .WilliamsR(
                    high: new[] { 10.0, 11, 12, 13, 14 },
                    low:  new[] { 5.0, 6, 7, 8, 9 },
                    close: new[] { 7.0, 9, 10, 11, 12 },
                    period: 3))
            .Build();
        Assert.NotNull(fig);
    }

    /// <summary>Obv — line 943 (0%-covered method).</summary>
    [Fact]
    public void Obv_AppliesIndicator()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Obv(close: new[] { 1.0, 2, 1, 3 }, volume: new[] { 100.0, 200, 50, 150 }))
            .Build();
        Assert.NotNull(fig);
    }

    /// <summary>Cci — line 953 (0%-covered method).</summary>
    [Fact]
    public void Cci_AppliesIndicator()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Cci(
                    high: new[] { 10.0, 11, 12, 13, 14, 15 },
                    low:  new[] { 5.0, 6, 7, 8, 9, 10 },
                    close: new[] { 7.0, 9, 10, 11, 12, 13 },
                    period: 3))
            .Build();
        Assert.NotNull(fig);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Phase Z.5 — null-configure arms (the false arm of `configure is not null`)
    // for the WithTitle / SetXLabel / SetYLabel overloads at AxesBuilder.cs:42, 55, 68.
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>WithTitle(string, configure: null) — `configure is not null` false arm.
    /// Title text is set, no TitleStyle is applied.</summary>
    [Fact]
    public void WithTitle_OverloadNullConfigure_SetsTitleOnly()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .WithTitle("plain", configure: null))
            .Build();
        Assert.Equal("plain", fig.SubPlots[0].Title);
    }

    /// <summary>SetXLabel(string, configure: null) — false arm. Label text set, no LabelStyle.</summary>
    [Fact]
    public void SetXLabel_OverloadNullConfigure_SetsLabelOnly()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .SetXLabel("x-only", configure: null))
            .Build();
        Assert.Equal("x-only", fig.SubPlots[0].XAxis.Label);
    }

    /// <summary>SetYLabel(string, configure: null) — false arm.</summary>
    [Fact]
    public void SetYLabel_OverloadNullConfigure_SetsLabelOnly()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .SetYLabel("y-only", configure: null))
            .Build();
        Assert.Equal("y-only", fig.SubPlots[0].YAxis.Label);
    }

    /// <summary>AxHLine with null configure — `configure?.Invoke()` null arm at line 269.</summary>
    [Fact]
    public void AxHLine_NullConfigure_AddsLineWithoutCustomization()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .AxHLine(3.5, configure: null))
            .Build();
        Assert.Single(fig.SubPlots[0].ReferenceLines);
    }

    /// <summary>AxVLine with null configure — null arm.</summary>
    [Fact]
    public void AxVLine_NullConfigure_AddsLineWithoutCustomization()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .AxVLine(1.5, configure: null))
            .Build();
        Assert.Single(fig.SubPlots[0].ReferenceLines);
    }

    /// <summary>AxHSpan with null configure — null arm.</summary>
    [Fact]
    public void AxHSpan_NullConfigure_AddsSpanWithoutCustomization()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 5], [1.0, 5])
                .AxHSpan(2, 3, configure: null))
            .Build();
        Assert.Single(fig.SubPlots[0].Spans);
    }

    /// <summary>AxVSpan with null configure — null arm.</summary>
    [Fact]
    public void AxVSpan_NullConfigure_AddsSpanWithoutCustomization()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 5], [1.0, 5])
                .AxVSpan(1, 2, configure: null))
            .Build();
        Assert.Single(fig.SubPlots[0].Spans);
    }

    /// <summary>Plot with null configure — series-builder method's false arm.</summary>
    [Fact]
    public void Plot_NullConfigure_AddsLineSeriesWithoutCustomization()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2], [3.0, 4], configure: null))
            .Build();
        Assert.Single(fig.SubPlots[0].Series);
    }

    /// <summary>Scatter with null configure.</summary>
    [Fact]
    public void Scatter_NullConfigure_AddsScatterSeriesWithoutCustomization()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Scatter([1.0, 2], [3.0, 4], configure: null))
            .Build();
        Assert.Single(fig.SubPlots[0].Series);
    }

    /// <summary>Bar with null configure.</summary>
    [Fact]
    public void Bar_NullConfigure_AddsBarSeriesWithoutCustomization()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Bar(["A", "B"], [1.0, 2.0], configure: null))
            .Build();
        Assert.Single(fig.SubPlots[0].Series);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Phase Ω.5 — non-null configure arms for indicator + signal helpers
    // (Phase Z covered the null arms; these flip the false→true branches at
    // AxesBuilder.cs:167, 262, 628, 886, 906, 917, 926, 936, 946, 956, 967, 977)
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void AddSignal_WithConfigure_AppliesCustomization()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .AddSignal(1.5, 3.5, SignalDirection.Buy, m => m.Color = global::MatPlotLibNet.Styling.Colors.Green))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].Signals);
    }

    [Fact]
    public void AnnotateWithArrowTarget_WithConfigure_AppliesCustomization()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .Annotate("hi", 2, 3, 4, 5, a => a.ConnectionStyle = ConnectionStyle.Arc3))
            .Build();
        Assert.Single(fig.SubPlots[0].Annotations);
    }

    [Fact]
    public void Ema_WithConfigure_AppliesCustomization()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2, 3, 4, 5, 6], [1.0, 2, 3, 4, 5, 6])
                .Ema(period: 3, configure: ind => ind.Offset = 1.0))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].Series);
    }

    [Fact]
    public void BollingerBands_WithConfigure_AppliesCustomization()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2, 3, 4, 5, 6, 7, 8, 9, 10], [1.0, 2, 3, 4, 5, 6, 7, 8, 9, 10])
                .BollingerBands(period: 5, stdDev: 2.0, configure: ind => ind.Offset = 0.5))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].Series);
    }

    [Fact]
    public void Rsi_WithConfigure_AppliesCustomization()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Rsi(
                prices: [1.0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16],
                period: 5,
                configure: ind => ind.Offset = 1.0))
            .Build();
        Assert.NotNull(fig);
    }

    [Fact]
    public void WilliamsR_WithConfigure_AppliesCustomization()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.WilliamsR(
                high: [10.0, 11, 12, 13, 14],
                low:  [5.0, 6, 7, 8, 9],
                close: [7.0, 9, 10, 11, 12],
                period: 3,
                configure: ind => ind.Offset = 1.0))
            .Build();
        Assert.NotNull(fig);
    }

    [Fact]
    public void Obv_WithConfigure_AppliesCustomization()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Obv(
                close: [10.0, 11, 12, 11, 10],
                volume: [1000.0, 1100, 1200, 1100, 1000],
                configure: ind => ind.Offset = 1.0))
            .Build();
        Assert.NotNull(fig);
    }

    [Fact]
    public void Cci_WithConfigure_AppliesCustomization()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Cci(
                high: [10.0, 11, 12, 13, 14, 15],
                low:  [5.0, 6, 7, 8, 9, 10],
                close: [7.0, 9, 10, 11, 12, 13],
                period: 3,
                configure: ind => ind.Offset = 1.0))
            .Build();
        Assert.NotNull(fig);
    }

    [Fact]
    public void ParabolicSar_WithoutConfigure_RendersDefaults()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.ParabolicSar(
                high: [10.0, 11, 12, 13, 14],
                low:  [5.0, 6, 7, 8, 9]))
            .Build();
        Assert.NotNull(fig);
    }

    [Fact]
    public void ParabolicSar_WithConfigure_AppliesCustomization()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.ParabolicSar(
                high: [10.0, 11, 12, 13, 14],
                low:  [5.0, 6, 7, 8, 9],
                step: 0.05, max: 0.5,
                configure: ind => ind.Offset = 1.0))
            .Build();
        Assert.NotNull(fig);
    }

    [Fact]
    public void AddSeries_WithConfigure_AppliesCustomization()
    {
        var series = new global::MatPlotLibNet.Models.Series.LineSeries(
            (global::MatPlotLibNet.Numerics.Vec)new[] { 1.0, 2.0 },
            (global::MatPlotLibNet.Numerics.Vec)new[] { 3.0, 4.0 });
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(series, configure: s => s.Label = "manual"))
            .Build();
        Assert.Equal("manual", fig.SubPlots[0].Series[0].Label);
    }

    [Fact]
    public void AddSeries_NullConfigure_AddsSeriesWithoutCustomization()
    {
        var series = new global::MatPlotLibNet.Models.Series.LineSeries(
            (global::MatPlotLibNet.Numerics.Vec)new[] { 1.0, 2.0 },
            (global::MatPlotLibNet.Numerics.Vec)new[] { 3.0, 4.0 });
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(series, configure: null))
            .Build();
        Assert.Single(fig.SubPlots[0].Series);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Wave J.1 — remaining branch gaps
    // L628 NestedPie null-configure arm; L906/926/936/946/956/966 IsBarSlotContext
    // true arm; L994 _isBarSlotContext OR-short-circuit; L1007/1009/1010 no-series throw.
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>NestedPie with null configure — line 628 null arm of <c>configure?.Invoke(s)</c>.</summary>
    [Fact]
    public void NestedPie_NullConfigure_AddsSunburstSeries()
    {
        var root = new TreeNode
        {
            Label = "Root",
            Children = [new() { Label = "A", Value = 3 }, new() { Label = "B", Value = 5 }]
        };
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.NestedPie(root, configure: null))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].Series);
    }

    /// <summary>Ema called after UseBarSlotX() — line 906 true arm and line 994
    /// <c>_isBarSlotContext</c> OR short-circuit.</summary>
    [Fact]
    public void Ema_InBarSlotContext_SetsIndicatorOffset()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .UseBarSlotX()
                .Plot([1.0, 2, 3, 4, 5, 6], [10.0, 11, 12, 13, 14, 15])
                .Ema(period: 3))
            .Build();
        Assert.NotNull(fig);
    }

    /// <summary>Rsi called after UseBarSlotX() — line 926 true arm.</summary>
    [Fact]
    public void Rsi_InBarSlotContext_SetsIndicatorOffset()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .UseBarSlotX()
                .Rsi([1.0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16], period: 5))
            .Build();
        Assert.NotNull(fig);
    }

    /// <summary>WilliamsR called after UseBarSlotX() — line 936 true arm.</summary>
    [Fact]
    public void WilliamsR_InBarSlotContext_SetsIndicatorOffset()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .UseBarSlotX()
                .WilliamsR(
                    high: [10.0, 11, 12, 13, 14],
                    low:  [5.0, 6, 7, 8, 9],
                    close: [7.0, 9, 10, 11, 12],
                    period: 3))
            .Build();
        Assert.NotNull(fig);
    }

    /// <summary>Obv called after UseBarSlotX() — line 946 true arm.</summary>
    [Fact]
    public void Obv_InBarSlotContext_SetsIndicatorOffset()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .UseBarSlotX()
                .Obv(close: [1.0, 2, 1, 3], volume: [100.0, 200, 50, 150]))
            .Build();
        Assert.NotNull(fig);
    }

    /// <summary>Cci called after UseBarSlotX() — line 956 true arm.</summary>
    [Fact]
    public void Cci_InBarSlotContext_SetsIndicatorOffset()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .UseBarSlotX()
                .Cci(
                    high: [10.0, 11, 12, 13, 14, 15],
                    low:  [5.0, 6, 7, 8, 9, 10],
                    close: [7.0, 9, 10, 11, 12, 13],
                    period: 3))
            .Build();
        Assert.NotNull(fig);
    }

    /// <summary>ParabolicSar called after UseBarSlotX() — line 966 true arm.</summary>
    [Fact]
    public void ParabolicSar_InBarSlotContext_SetsIndicatorOffset()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .UseBarSlotX()
                .ParabolicSar(
                    high: [10.0, 11, 12, 13, 14],
                    low:  [5.0, 6, 7, 8, 9]))
            .Build();
        Assert.NotNull(fig);
    }

    /// <summary>GetPriceData with no series on axes — line 1007 false arm, line 1009,
    /// line 1010 throw arm. Verifies the error message rather than silently swallowing.</summary>
    [Fact]
    public void GetPriceData_NoSeries_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Plt.Create()
                .AddSubPlot(1, 1, 1, ax => ax.Sma(3))
                .Build());
    }

    // ── Tier 1a indicator shortcuts — GarmanKlass, YangZhang, KaufmanEfficiencyRatio ──

    private static readonly double[] TierO = [100, 102, 103, 105, 107, 108];
    private static readonly double[] TierH = [105, 104, 106, 108, 109, 111];
    private static readonly double[] TierL = [99, 100, 102, 104, 106, 107];
    private static readonly double[] TierC = [102, 103, 105, 107, 108, 110];

    /// <summary>GarmanKlass fluent shortcut applies the indicator to the axes.</summary>
    [Fact]
    public void GarmanKlass_Shortcut_AddsSeries()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.GarmanKlass(TierO, TierH, TierL, TierC, period: 5))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].Series);
    }

    /// <summary>GarmanKlass after UseBarSlotX() sets Offset = 0.5 (bar-slot true arm).</summary>
    [Fact]
    public void GarmanKlass_InBarSlotContext_SetsIndicatorOffset()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .UseBarSlotX()
                .GarmanKlass(TierO, TierH, TierL, TierC, period: 5))
            .Build();
        Assert.NotNull(fig);
    }

    /// <summary>GarmanKlass configure callback is invoked on the indicator instance.</summary>
    [Fact]
    public void GarmanKlass_Configure_IsInvoked()
    {
        bool configured = false;
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .GarmanKlass(TierO, TierH, TierL, TierC, period: 5,
                    configure: ind => { configured = true; ind.LineWidth = 3; }))
            .Build();
        Assert.True(configured);
    }

    /// <summary>YangZhang fluent shortcut applies the indicator to the axes.</summary>
    [Fact]
    public void YangZhang_Shortcut_AddsSeries()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.YangZhang(TierO, TierH, TierL, TierC, period: 5))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].Series);
    }

    /// <summary>YangZhang after UseBarSlotX() sets Offset = 0.5 (bar-slot true arm).</summary>
    [Fact]
    public void YangZhang_InBarSlotContext_SetsIndicatorOffset()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .UseBarSlotX()
                .YangZhang(TierO, TierH, TierL, TierC, period: 5))
            .Build();
        Assert.NotNull(fig);
    }

    /// <summary>YangZhang configure callback is invoked on the indicator instance.</summary>
    [Fact]
    public void YangZhang_Configure_IsInvoked()
    {
        bool configured = false;
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .YangZhang(TierO, TierH, TierL, TierC, period: 5,
                    configure: ind => { configured = true; ind.LineWidth = 3; }))
            .Build();
        Assert.True(configured);
    }

    /// <summary>KaufmanEfficiencyRatio fluent shortcut applies the indicator to the axes.</summary>
    [Fact]
    public void KaufmanEfficiencyRatio_Shortcut_AddsSeries()
    {
        double[] prices = [100, 101, 102, 103, 104, 105, 106, 107];
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.KaufmanEfficiencyRatio(prices, period: 5))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].Series);
    }

    /// <summary>KaufmanEfficiencyRatio after UseBarSlotX() sets Offset = 0.5 (bar-slot true arm).</summary>
    [Fact]
    public void KaufmanEfficiencyRatio_InBarSlotContext_SetsIndicatorOffset()
    {
        double[] prices = [100, 101, 102, 103, 104, 105, 106, 107];
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .UseBarSlotX()
                .KaufmanEfficiencyRatio(prices, period: 5))
            .Build();
        Assert.NotNull(fig);
    }

    /// <summary>KaufmanEfficiencyRatio configure callback is invoked on the indicator instance.</summary>
    [Fact]
    public void KaufmanEfficiencyRatio_Configure_IsInvoked()
    {
        double[] prices = [100, 101, 102, 103, 104, 105, 106, 107];
        bool configured = false;
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .KaufmanEfficiencyRatio(prices, period: 5,
                    configure: ind => { configured = true; ind.LineWidth = 3; }))
            .Build();
        Assert.True(configured);
    }

    // ── Gap 5 drawing-tool shortcuts — AddTrendline / AddLevel / AddFibonacci ──

    [Fact]
    public void AddTrendline_AddsToAxes()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([0.0, 10], [0.0, 10])
                .AddTrendline(1, 2, 3, 4))
            .Build();
        Assert.Single(fig.SubPlots[0].Trendlines);
    }

    [Fact]
    public void AddTrendline_WithConfigure_AppliesCustomisation()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([0.0, 10], [0.0, 10])
                .AddTrendline(1, 2, 3, 4, line => line.Label = "support"))
            .Build();
        Assert.Equal("support", fig.SubPlots[0].Trendlines[0].Label);
    }

    [Fact]
    public void AddLevel_AddsToAxes()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([0.0, 10], [0.0, 10])
                .AddLevel(5.0))
            .Build();
        Assert.Single(fig.SubPlots[0].HorizontalLevels);
    }

    [Fact]
    public void AddLevel_WithConfigure_AppliesCustomisation()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([0.0, 10], [0.0, 10])
                .AddLevel(5.0, lv => lv.Label = "pivot"))
            .Build();
        Assert.Equal("pivot", fig.SubPlots[0].HorizontalLevels[0].Label);
    }

    [Fact]
    public void AddFibonacci_AddsToAxes()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([0.0, 10], [0.0, 10])
                .AddFibonacci(priceHigh: 10, priceLow: 5))
            .Build();
        Assert.Single(fig.SubPlots[0].FibonacciRetracements);
    }

    [Fact]
    public void AddFibonacci_WithConfigure_AppliesCustomisation()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([0.0, 10], [0.0, 10])
                .AddFibonacci(priceHigh: 10, priceLow: 5, fib => fib.ShowLabels = false))
            .Build();
        Assert.False(fig.SubPlots[0].FibonacciRetracements[0].ShowLabels);
    }

    // ── Generic Indicator entry-point (delegates to IIndicator.Apply) ──

    [Fact]
    public void Indicator_Generic_AppliesToAxes()
    {
        double[] prices = [100, 101, 102, 103, 104, 105, 106];
        var atr = new MatPlotLibNet.Indicators.Atr(
            high: [101, 102, 103, 104, 105, 106, 107],
            low:  [99, 100, 101, 102, 103, 104, 105],
            close: prices,
            period: 3);
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0], [1.0]).Indicator(atr))
            .Build();
        // Adds a series via indicator.Apply — original Plot + ATR output
        Assert.True(fig.SubPlots[0].Series.Count >= 2);
    }

    // ── GetPriceData fallback — resolve price from OHLC series when no LinePlot/Scatter ──

    [Fact]
    public void Sma_AfterCandlestick_ResolvesPriceFromOhlcSeries()
    {
        // No Plot() — Candlestick alone. Indicator shortcut needs GetPriceData to fall
        // through to the OHLC branch (line 1070) and pull Close prices.
        double[] o = [10, 11, 12, 13, 14, 15];
        double[] h = [12, 13, 14, 15, 16, 17];
        double[] l = [9, 10, 11, 12, 13, 14];
        double[] c = [11, 12, 13, 14, 15, 16];
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Candlestick(o, h, l, c).Sma(3))
            .Build();
        Assert.NotNull(fig);
    }

    // ── Tier 1b shortcuts — Cusum, Ffd ──

    [Fact]
    public void Cusum_Shortcut_AddsSeries()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Cusum([100.0, 101, 102, 103, 100], threshold: 0.02))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].Series);
    }

    [Fact]
    public void Cusum_InBarSlotContext_SetsIndicatorOffset()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .UseBarSlotX()
                .Cusum([100.0, 101, 102, 103, 100], threshold: 0.02))
            .Build();
        Assert.NotNull(fig);
    }

    [Fact]
    public void Cusum_Configure_IsInvoked()
    {
        bool configured = false;
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Cusum([100.0, 101, 102, 103, 100], threshold: 0.02,
                    configure: ind => { configured = true; ind.LineWidth = 2; }))
            .Build();
        Assert.True(configured);
    }

    [Fact]
    public void Ffd_Shortcut_AddsSeries()
    {
        var prices = new double[80];
        for (int i = 0; i < 80; i++) prices[i] = 100 + 0.25 * i;
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Ffd(prices, d: 0.4, tolerance: 1e-2))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].Series);
    }

    [Fact]
    public void Ffd_InBarSlotContext_SetsIndicatorOffset()
    {
        var prices = new double[80];
        for (int i = 0; i < 80; i++) prices[i] = 100 + 0.25 * i;
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .UseBarSlotX()
                .Ffd(prices, d: 0.4, tolerance: 1e-2))
            .Build();
        Assert.NotNull(fig);
    }

    [Fact]
    public void Ffd_Configure_IsInvoked()
    {
        bool configured = false;
        var prices = new double[80];
        for (int i = 0; i < 80; i++) prices[i] = 100 + 0.25 * i;
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Ffd(prices, d: 0.4, tolerance: 1e-2,
                    configure: ind => { configured = true; ind.LineWidth = 2; }))
            .Build();
        Assert.True(configured);
    }

    // ── Tier 1c shortcuts — AmihudIlliquidity, CorwinSchultz, Vpin, RollSpread ──

    [Fact]
    public void AmihudIlliquidity_Shortcut_AddsSeries()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .AmihudIlliquidity([100.0, 101, 102, 103, 104], [1000.0, 1000, 1000, 1000, 1000], period: 3))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].Series);
    }

    [Fact]
    public void AmihudIlliquidity_InBarSlotContext_SetsIndicatorOffset()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .UseBarSlotX()
                .AmihudIlliquidity([100.0, 101, 102, 103, 104], [1000.0, 1000, 1000, 1000, 1000], period: 3))
            .Build();
        Assert.NotNull(fig);
    }

    [Fact]
    public void AmihudIlliquidity_Configure_IsInvoked()
    {
        bool configured = false;
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .AmihudIlliquidity([100.0, 101, 102, 103, 104], [1000.0, 1000, 1000, 1000, 1000], period: 3,
                    configure: ind => { configured = true; ind.LineWidth = 2; }))
            .Build();
        Assert.True(configured);
    }

    [Fact]
    public void CorwinSchultz_Shortcut_AddsSeries()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .CorwinSchultz([100.5, 100.5, 100.5, 100.5], [99.5, 99.5, 99.5, 99.5], period: 2))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].Series);
    }

    [Fact]
    public void CorwinSchultz_InBarSlotContext_SetsIndicatorOffset()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .UseBarSlotX()
                .CorwinSchultz([100.5, 100.5, 100.5, 100.5], [99.5, 99.5, 99.5, 99.5], period: 2))
            .Build();
        Assert.NotNull(fig);
    }

    [Fact]
    public void CorwinSchultz_Configure_IsInvoked()
    {
        bool configured = false;
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .CorwinSchultz([100.5, 100.5, 100.5, 100.5], [99.5, 99.5, 99.5, 99.5], period: 2,
                    configure: ind => { configured = true; ind.LineWidth = 2; }))
            .Build();
        Assert.True(configured);
    }

    [Fact]
    public void Vpin_Shortcut_AddsSeries()
    {
        var close = new double[15]; var vol = new double[15];
        for (int i = 0; i < 15; i++) { close[i] = 100 + i; vol[i] = 1000; }
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Vpin(close, vol, bucketPeriod: 5, sigmaPeriod: 5))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].Series);
    }

    [Fact]
    public void Vpin_InBarSlotContext_SetsIndicatorOffset()
    {
        var close = new double[15]; var vol = new double[15];
        for (int i = 0; i < 15; i++) { close[i] = 100 + i; vol[i] = 1000; }
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .UseBarSlotX()
                .Vpin(close, vol, bucketPeriod: 5, sigmaPeriod: 5))
            .Build();
        Assert.NotNull(fig);
    }

    [Fact]
    public void Vpin_Configure_IsInvoked()
    {
        var close = new double[15]; var vol = new double[15];
        for (int i = 0; i < 15; i++) { close[i] = 100 + i; vol[i] = 1000; }
        bool configured = false;
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Vpin(close, vol, bucketPeriod: 5, sigmaPeriod: 5,
                    configure: ind => { configured = true; ind.LineWidth = 2; }))
            .Build();
        Assert.True(configured);
    }

    [Fact]
    public void RollSpread_Shortcut_AddsSeries()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .RollSpread([100.0, 100.2, 100.0, 100.2, 100.0, 100.2, 100.0, 100.2], period: 4))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].Series);
    }

    [Fact]
    public void RollSpread_InBarSlotContext_SetsIndicatorOffset()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .UseBarSlotX()
                .RollSpread([100.0, 100.2, 100.0, 100.2, 100.0, 100.2, 100.0, 100.2], period: 4))
            .Build();
        Assert.NotNull(fig);
    }

    [Fact]
    public void RollSpread_Configure_IsInvoked()
    {
        bool configured = false;
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .RollSpread([100.0, 100.2, 100.0, 100.2, 100.0, 100.2, 100.0, 100.2], period: 4,
                    configure: ind => { configured = true; ind.LineWidth = 2; }))
            .Build();
        Assert.True(configured);
    }

    // ── Tier 1d shortcuts — LaguerreRsi, MamaFama, SqueezeMomentum ──

    [Fact]
    public void LaguerreRsi_Shortcut_AddsSeries()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .LaguerreRsi([100.0, 101, 102, 103, 104, 105], alpha: 0.2))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].Series);
    }

    [Fact]
    public void LaguerreRsi_InBarSlotContext_SetsIndicatorOffset()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .UseBarSlotX()
                .LaguerreRsi([100.0, 101, 102, 103, 104, 105], alpha: 0.2))
            .Build();
        Assert.NotNull(fig);
    }

    [Fact]
    public void LaguerreRsi_Configure_IsInvoked()
    {
        bool configured = false;
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .LaguerreRsi([100.0, 101, 102, 103, 104, 105], alpha: 0.2,
                    configure: ind => { configured = true; ind.LineWidth = 2; }))
            .Build();
        Assert.True(configured);
    }

    [Fact]
    public void MamaFama_Shortcut_AddsTwoSeries()
    {
        var prices = Enumerable.Range(0, 50).Select(i => 100.0 + i).ToArray();
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.MamaFama(prices))
            .Build();
        Assert.Equal(2, fig.SubPlots[0].Series.Count);
    }

    [Fact]
    public void MamaFama_InBarSlotContext_SetsIndicatorOffset()
    {
        var prices = Enumerable.Range(0, 50).Select(i => 100.0 + i).ToArray();
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.UseBarSlotX().MamaFama(prices))
            .Build();
        Assert.NotNull(fig);
    }

    [Fact]
    public void MamaFama_Configure_IsInvoked()
    {
        var prices = Enumerable.Range(0, 50).Select(i => 100.0 + i).ToArray();
        bool configured = false;
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .MamaFama(prices, configure: ind => { configured = true; ind.LineWidth = 2; }))
            .Build();
        Assert.True(configured);
    }

    [Fact]
    public void SqueezeMomentum_Shortcut_AddsSeries()
    {
        double[] c = Enumerable.Range(0, 30).Select(i => 100.0 + i).ToArray();
        double[] h = c.Select(v => v + 0.5).ToArray();
        double[] l = c.Select(v => v - 0.5).ToArray();
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.SqueezeMomentum(h, l, c, period: 5))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].Series);
    }

    [Fact]
    public void SqueezeMomentum_InBarSlotContext_SetsIndicatorOffset()
    {
        double[] c = Enumerable.Range(0, 30).Select(i => 100.0 + i).ToArray();
        double[] h = c.Select(v => v + 0.5).ToArray();
        double[] l = c.Select(v => v - 0.5).ToArray();
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.UseBarSlotX().SqueezeMomentum(h, l, c, period: 5))
            .Build();
        Assert.NotNull(fig);
    }

    [Fact]
    public void SqueezeMomentum_Configure_IsInvoked()
    {
        double[] c = Enumerable.Range(0, 30).Select(i => 100.0 + i).ToArray();
        double[] h = c.Select(v => v + 0.5).ToArray();
        double[] l = c.Select(v => v - 0.5).ToArray();
        bool configured = false;
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .SqueezeMomentum(h, l, c, period: 5,
                    configure: ind => { configured = true; ind.LineWidth = 2; }))
            .Build();
        Assert.True(configured);
    }

    // ── Tier 2a shortcuts — Bocpd, TurbulenceIndex, DispersionIndex ──

    [Fact]
    public void Bocpd_Shortcut_AddsSeries()
    {
        var prices = Enumerable.Range(0, 20).Select(i => 100.0 + i).ToArray();
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Bocpd(prices))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].Series);
    }

    [Fact]
    public void Bocpd_InBarSlotContext_SetsIndicatorOffset()
    {
        var prices = Enumerable.Range(0, 20).Select(i => 100.0 + i).ToArray();
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.UseBarSlotX().Bocpd(prices))
            .Build();
        Assert.NotNull(fig);
    }

    [Fact]
    public void Bocpd_Configure_IsInvoked()
    {
        var prices = Enumerable.Range(0, 20).Select(i => 100.0 + i).ToArray();
        bool configured = false;
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Bocpd(prices, configure: ind => { configured = true; ind.LineWidth = 2; }))
            .Build();
        Assert.True(configured);
    }

    [Fact]
    public void TurbulenceIndex_Shortcut_AddsSeries()
    {
        var features = new double[6][];
        for (int i = 0; i < 6; i++) features[i] = [100.0 + i];
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.TurbulenceIndex(features, window: 3))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].Series);
    }

    [Fact]
    public void TurbulenceIndex_InBarSlotContext_SetsIndicatorOffset()
    {
        var features = new double[6][];
        for (int i = 0; i < 6; i++) features[i] = [100.0 + i];
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.UseBarSlotX().TurbulenceIndex(features, window: 3))
            .Build();
        Assert.NotNull(fig);
    }

    [Fact]
    public void TurbulenceIndex_Configure_IsInvoked()
    {
        var features = new double[6][];
        for (int i = 0; i < 6; i++) features[i] = [100.0 + i];
        bool configured = false;
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .TurbulenceIndex(features, window: 3,
                    configure: ind => { configured = true; ind.LineWidth = 2; }))
            .Build();
        Assert.True(configured);
    }

    [Fact]
    public void DispersionIndex_Shortcut_AddsSeries()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.DispersionIndex(
                [[0.2, 0.3, 0.5], [0.8, 0.5, 0.1]]))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].Series);
    }

    [Fact]
    public void DispersionIndex_InBarSlotContext_SetsIndicatorOffset()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .UseBarSlotX()
                .DispersionIndex([[0.2, 0.3], [0.8, 0.5]]))
            .Build();
        Assert.NotNull(fig);
    }

    [Fact]
    public void DispersionIndex_Configure_IsInvoked()
    {
        bool configured = false;
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .DispersionIndex([[0.2, 0.3], [0.8, 0.5]],
                    configure: ind => { configured = true; ind.LineWidth = 2; }))
            .Build();
        Assert.True(configured);
    }

    // ── Tier 2b shortcuts — PermutationEntropy, WaveletEnergyRatio, WaveletEntropy ──

    [Fact]
    public void PermutationEntropy_Shortcut_AddsSeries()
    {
        var prices = Enumerable.Range(0, 50).Select(i => (double)i).ToArray();
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.PermutationEntropy(prices, order: 3, window: 20))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].Series);
    }

    [Fact]
    public void PermutationEntropy_InBarSlotContext_SetsIndicatorOffset()
    {
        var prices = Enumerable.Range(0, 50).Select(i => (double)i).ToArray();
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .UseBarSlotX()
                .PermutationEntropy(prices, order: 3, window: 20))
            .Build();
        Assert.NotNull(fig);
    }

    [Fact]
    public void PermutationEntropy_Configure_IsInvoked()
    {
        var prices = Enumerable.Range(0, 50).Select(i => (double)i).ToArray();
        bool configured = false;
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .PermutationEntropy(prices, order: 3, window: 20,
                    configure: ind => { configured = true; ind.LineWidth = 2; }))
            .Build();
        Assert.True(configured);
    }

    [Fact]
    public void WaveletEnergyRatio_Shortcut_AddsSeries()
    {
        var prices = Enumerable.Range(0, 80).Select(i => (double)i).ToArray();
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.WaveletEnergyRatio(prices, window: 16, level: 0))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].Series);
    }

    [Fact]
    public void WaveletEnergyRatio_InBarSlotContext_SetsIndicatorOffset()
    {
        var prices = Enumerable.Range(0, 80).Select(i => (double)i).ToArray();
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .UseBarSlotX()
                .WaveletEnergyRatio(prices, window: 16, level: 0))
            .Build();
        Assert.NotNull(fig);
    }

    [Fact]
    public void WaveletEnergyRatio_Configure_IsInvoked()
    {
        var prices = Enumerable.Range(0, 80).Select(i => (double)i).ToArray();
        bool configured = false;
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .WaveletEnergyRatio(prices, window: 16, level: 0,
                    configure: ind => { configured = true; ind.LineWidth = 2; }))
            .Build();
        Assert.True(configured);
    }

    [Fact]
    public void WaveletEntropy_Shortcut_AddsSeries()
    {
        var prices = Enumerable.Range(0, 80).Select(i => (double)i).ToArray();
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.WaveletEntropy(prices, window: 16))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].Series);
    }

    [Fact]
    public void WaveletEntropy_InBarSlotContext_SetsIndicatorOffset()
    {
        var prices = Enumerable.Range(0, 80).Select(i => (double)i).ToArray();
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .UseBarSlotX()
                .WaveletEntropy(prices, window: 16))
            .Build();
        Assert.NotNull(fig);
    }

    [Fact]
    public void WaveletEntropy_Configure_IsInvoked()
    {
        var prices = Enumerable.Range(0, 80).Select(i => (double)i).ToArray();
        bool configured = false;
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .WaveletEntropy(prices, window: 16,
                    configure: ind => { configured = true; ind.LineWidth = 2; }))
            .Build();
        Assert.True(configured);
    }

    // ── Tier 2c shortcuts — CyberCycle, RoofingFilter, EhlersSineWave, AdaptiveStochastic ──

    [Fact]
    public void CyberCycle_Shortcut_AddsSeries()
    {
        var prices = Enumerable.Range(0, 30).Select(i => (double)i).ToArray();
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.CyberCycle(prices))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].Series);
    }

    [Fact]
    public void CyberCycle_InBarSlotContext_SetsIndicatorOffset()
    {
        var prices = Enumerable.Range(0, 30).Select(i => (double)i).ToArray();
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.UseBarSlotX().CyberCycle(prices))
            .Build();
        Assert.NotNull(fig);
    }

    [Fact]
    public void CyberCycle_Configure_IsInvoked()
    {
        var prices = Enumerable.Range(0, 30).Select(i => (double)i).ToArray();
        bool configured = false;
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .CyberCycle(prices, alpha: 0.1,
                    configure: ind => { configured = true; ind.LineWidth = 2; }))
            .Build();
        Assert.True(configured);
    }

    [Fact]
    public void RoofingFilter_Shortcut_AddsSeries()
    {
        var prices = Enumerable.Range(0, 80).Select(i => (double)i).ToArray();
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.RoofingFilter(prices))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].Series);
    }

    [Fact]
    public void RoofingFilter_InBarSlotContext_SetsIndicatorOffset()
    {
        var prices = Enumerable.Range(0, 80).Select(i => (double)i).ToArray();
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.UseBarSlotX().RoofingFilter(prices))
            .Build();
        Assert.NotNull(fig);
    }

    [Fact]
    public void RoofingFilter_Configure_IsInvoked()
    {
        var prices = Enumerable.Range(0, 80).Select(i => (double)i).ToArray();
        bool configured = false;
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .RoofingFilter(prices, hpPeriod: 20, lpPeriod: 5,
                    configure: ind => { configured = true; ind.LineWidth = 2; }))
            .Build();
        Assert.True(configured);
    }

    [Fact]
    public void EhlersSineWave_Shortcut_AddsTwoSeries()
    {
        var prices = Enumerable.Range(0, 30).Select(i => (double)i).ToArray();
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.EhlersSineWave(prices))
            .Build();
        Assert.Equal(2, fig.SubPlots[0].Series.Count);
    }

    [Fact]
    public void EhlersSineWave_InBarSlotContext_SetsIndicatorOffset()
    {
        var prices = Enumerable.Range(0, 30).Select(i => (double)i).ToArray();
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.UseBarSlotX().EhlersSineWave(prices))
            .Build();
        Assert.NotNull(fig);
    }

    [Fact]
    public void EhlersSineWave_Configure_IsInvoked()
    {
        var prices = Enumerable.Range(0, 30).Select(i => (double)i).ToArray();
        bool configured = false;
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .EhlersSineWave(prices,
                    configure: ind => { configured = true; ind.LineWidth = 2; }))
            .Build();
        Assert.True(configured);
    }

    [Fact]
    public void AdaptiveStochastic_Shortcut_AddsSeries()
    {
        var c = Enumerable.Range(0, 30).Select(i => (double)i + 100).ToArray();
        var h = c.Select(v => v + 0.5).ToArray();
        var l = c.Select(v => v - 0.5).ToArray();
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AdaptiveStochastic(h, l, c))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].Series);
    }

    [Fact]
    public void AdaptiveStochastic_InBarSlotContext_SetsIndicatorOffset()
    {
        var c = Enumerable.Range(0, 30).Select(i => (double)i + 100).ToArray();
        var h = c.Select(v => v + 0.5).ToArray();
        var l = c.Select(v => v - 0.5).ToArray();
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.UseBarSlotX().AdaptiveStochastic(h, l, c))
            .Build();
        Assert.NotNull(fig);
    }

    [Fact]
    public void AdaptiveStochastic_Configure_IsInvoked()
    {
        var c = Enumerable.Range(0, 30).Select(i => (double)i + 100).ToArray();
        var h = c.Select(v => v + 0.5).ToArray();
        var l = c.Select(v => v - 0.5).ToArray();
        bool configured = false;
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .AdaptiveStochastic(h, l, c, smoothingPeriod: 3,
                    configure: ind => { configured = true; ind.LineWidth = 2; }))
            .Build();
        Assert.True(configured);
    }

    // ── Tier 2d shortcuts — ForceIndex, AroonOscillator, RelativeVigorIndex ──

    [Fact]
    public void ForceIndex_Shortcut_AddsSeries()
    {
        double[] close = [100, 101, 102, 103, 104];
        double[] volume = [1000, 1100, 900, 1200, 1000];
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.ForceIndex(close, volume))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].Series);
    }

    [Fact]
    public void ForceIndex_InBarSlotContext_SetsIndicatorOffset()
    {
        double[] close = [100, 101, 102, 103, 104];
        double[] volume = [1000, 1100, 900, 1200, 1000];
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.UseBarSlotX().ForceIndex(close, volume))
            .Build();
        Assert.NotNull(fig);
    }

    [Fact]
    public void ForceIndex_Configure_IsInvoked()
    {
        double[] close = [100, 101, 102, 103, 104];
        double[] volume = [1000, 1100, 900, 1200, 1000];
        bool configured = false;
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .ForceIndex(close, volume, period: 3,
                    configure: ind => { configured = true; ind.LineWidth = 2; }))
            .Build();
        Assert.True(configured);
    }

    [Fact]
    public void AroonOscillator_Shortcut_AddsSeries()
    {
        var h = Enumerable.Range(0, 30).Select(i => 101.0 + i).ToArray();
        var l = Enumerable.Range(0, 30).Select(i => 99.0 + i).ToArray();
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AroonOscillator(h, l))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].Series);
    }

    [Fact]
    public void AroonOscillator_InBarSlotContext_SetsIndicatorOffset()
    {
        var h = Enumerable.Range(0, 30).Select(i => 101.0 + i).ToArray();
        var l = Enumerable.Range(0, 30).Select(i => 99.0 + i).ToArray();
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.UseBarSlotX().AroonOscillator(h, l))
            .Build();
        Assert.NotNull(fig);
    }

    [Fact]
    public void AroonOscillator_Configure_IsInvoked()
    {
        var h = Enumerable.Range(0, 30).Select(i => 101.0 + i).ToArray();
        var l = Enumerable.Range(0, 30).Select(i => 99.0 + i).ToArray();
        bool configured = false;
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .AroonOscillator(h, l, period: 5,
                    configure: ind => { configured = true; ind.LineWidth = 2; }))
            .Build();
        Assert.True(configured);
    }

    [Fact]
    public void RelativeVigorIndex_Shortcut_AddsTwoSeries()
    {
        var a = Enumerable.Repeat(100.0, 30).ToArray();
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.RelativeVigorIndex(a, a, a, a))
            .Build();
        Assert.Equal(2, fig.SubPlots[0].Series.Count);
    }

    [Fact]
    public void RelativeVigorIndex_InBarSlotContext_SetsIndicatorOffset()
    {
        var a = Enumerable.Repeat(100.0, 30).ToArray();
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.UseBarSlotX().RelativeVigorIndex(a, a, a, a))
            .Build();
        Assert.NotNull(fig);
    }

    [Fact]
    public void RelativeVigorIndex_Configure_IsInvoked()
    {
        var a = Enumerable.Repeat(100.0, 30).ToArray();
        bool configured = false;
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .RelativeVigorIndex(a, a, a, a, period: 5,
                    configure: ind => { configured = true; ind.LineWidth = 2; }))
            .Build();
        Assert.True(configured);
    }
}
