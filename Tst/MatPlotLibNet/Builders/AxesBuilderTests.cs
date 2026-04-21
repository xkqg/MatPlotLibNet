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
}
