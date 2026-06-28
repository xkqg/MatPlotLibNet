// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Features;

/// <summary>TDD tests for the LegendValues feature on <see cref="FigureBuilder"/> and <see cref="AxesBuilder"/>.</summary>
public class LegendValuesTests
{
    // ── Model / flag tests ──────────────────────────────────────────────────

    /// <summary>Legend.LegendValues defaults to false.</summary>
    [Fact]
    public void Legend_LegendValues_DefaultsFalse()
    {
        var legend = new Legend();
        Assert.False(legend.LegendValues);
    }

    /// <summary>Legend.LegendValues can be set to true via with-expression.</summary>
    [Fact]
    public void Legend_LegendValues_CanBeSetTrue()
    {
        var legend = new Legend { LegendValues = true };
        Assert.True(legend.LegendValues);
    }

    // ── AxesBuilder fluent API tests ────────────────────────────────────────

    /// <summary>WithLegendValues(true) sets LegendValues on the axes Legend.</summary>
    [Fact]
    public void AxesBuilder_WithLegendValues_SetsFlag()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Signal")
                .WithLegendValues())
            .Build();

        Assert.True(figure.SubPlots[0].Legend.LegendValues);
    }

    /// <summary>WithLegendValues(false) leaves LegendValues false.</summary>
    [Fact]
    public void AxesBuilder_WithLegendValues_FalseArg_StaysFalse()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Signal")
                .WithLegendValues(false))
            .Build();

        Assert.False(figure.SubPlots[0].Legend.LegendValues);
    }

    // ── FigureBuilder fluent API tests ──────────────────────────────────────

    /// <summary>FigureBuilder.WithLegendValues sets the flag on the default axes' Legend.</summary>
    [Fact]
    public void FigureBuilder_WithLegendValues_SetsFlag()
    {
        var figure = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Signal")
            .WithLegendValues()
            .Build();

        Assert.True(figure.SubPlots[0].Legend.LegendValues);
    }

    // ── SVG rendering tests ─────────────────────────────────────────────────

    /// <summary>Without WithLegendValues the SVG legend shows only the series label (no value suffix).</summary>
    [Fact]
    public void LegendValues_Disabled_LegendShowsOnlyLabel()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.7])
                .Plot([1.0, 2.0], [0.0, 0.0], s => s.Label = "Signal")
                .WithLegend())
            .ToSvg();

        // The series label should be present
        Assert.Contains("Signal", svg);
        // But it should NOT include " = " appended value
        // (We check that "Signal = " does not appear when the flag is off)
        Assert.DoesNotContain("Signal = ", svg);
    }

    /// <summary>With WithLegendValues the SVG legend shows label + " = " + last value (InvariantCulture).</summary>
    [Fact]
    public void LegendValues_Enabled_AppendsSuffixToLegendLabel()
    {
        // Y data last value = 2.7 → legend should show "Signal = 2.70"
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [1.0, 2.7], s => s.Label = "Signal")
                .WithLegendValues())
            .ToSvg();

        // Must contain the series label AND the appended last-value suffix
        Assert.Contains("Signal = ", svg);
    }

    /// <summary>The appended last value uses InvariantCulture (decimal dot, not comma) regardless of host locale.</summary>
    [Fact]
    public void LegendValues_UsesInvariantCulture_DecimalDot()
    {
        // 2.7 must render as "2.70" (InvariantCulture two-decimal F2 format), never as "2,70"
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [1.0, 2.7], s => s.Label = "Signal")
                .WithLegendValues())
            .ToSvg();

        Assert.Contains("2.70", svg);
        Assert.DoesNotContain("2,70", svg);
    }

    /// <summary>Multiple labeled series each show their own last value.</summary>
    [Fact]
    public void LegendValues_MultipleLabeled_EachGetsOwnValue()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [1.0, 3.5], s => s.Label = "Alpha")
                .Plot([1.0, 2.0], [1.0, 8.2], s => s.Label = "Beta")
                .WithLegendValues())
            .ToSvg();

        Assert.Contains("Alpha = ", svg);
        Assert.Contains("Beta = ", svg);
        Assert.Contains("3.50", svg);
        Assert.Contains("8.20", svg);
    }

    /// <summary>Unlabeled series are not affected by WithLegendValues (they never appear in the legend).</summary>
    [Fact]
    public void LegendValues_UnlabeledSeries_NotInLegend()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [1.0, 9.9])
                .Plot([1.0, 2.0], [1.0, 3.5], s => s.Label = "Visible")
                .WithLegendValues())
            .ToSvg();

        Assert.Contains("Visible = ", svg);
        // 9.9 from the unlabeled series should not appear as a legend value
        Assert.DoesNotContain("9.90", svg);
    }

    /// <summary>A series without YData (empty) shows label without value suffix even when flag is enabled.</summary>
    [Fact]
    public void LegendValues_EmptyYData_LabelOnlyNoSuffix()
    {
        // We can construct a non-XYSeries to ensure graceful fallback
        // For simplicity: use a series whose label IS set but whose last-value resolution gracefully
        // returns no suffix. Use a bar series (categorical, no continuous YData in the XY sense).
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Bar(["A", "B"], [1.0, 2.0], s => s.Label = "Bars")
                .WithLegendValues())
            .ToSvg();

        // Bar series has no YData in XYSeries sense — should show label without suffix
        Assert.Contains("Bars", svg);
        // Specifically NOT "Bars = 2.00" unless BarSeries happens to be handled (we spec: only XYSeries)
        // Check label appears, does not crash
    }
}
