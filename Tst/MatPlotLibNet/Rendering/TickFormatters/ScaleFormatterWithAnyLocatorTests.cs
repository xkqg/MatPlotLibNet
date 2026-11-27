// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.TickFormatters;

/// <summary>A log, symmetric-log or logit axis keeps its scale's formatter whatever locator places its ticks, as matplotlib's scale sets
/// the major formatter in <c>set_default_locators_and_formatters</c> and <c>set_major_locator</c> leaves it. On a log or symlog axis
/// that formatter is <c>LogFormatterSciNotation</c>, whose <c>set_locs</c> reads the VIEW ("Use axis view limits to control which ticks
/// are labeled. The *locs* parameter is ignored"): with more powers of ten in view than <c>minor_thresholds[0]</c> it labels the powers
/// alone, past <c>minor_thresholds[1]</c> decades the coefficients 1, 2, 3, 4, 6, 10, and every one below; <c>__call__</c> returns ''
/// for a tick whose rounded coefficient is not among them. Printed with matplotlib 3.11.2, <c>set_major_locator(FixedLocator(...))</c>:
/// log ylim (1, 100), [1, 5, 50] -> ['10^0', '', ''] under both styles; log (2, 8), [3, 5] -> ['3\times10^0', '']; symlog
/// (linthresh 2) ylim (-1000, 1000), [-100, -5, 0, 5, 100] -> ['-10^2', '', '0', '', '10^2'].</summary>
public sealed class ScaleFormatterWithAnyLocatorTests
{
    private static List<string> YLabels(Theme theme, AxisScale scale, double lo, double hi, double[] fixedTicks, double linthresh = 1)
    {
        var figure = Plt.Create().WithSize(640, 480).WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax =>
            {
                // X over 10 .. 20, so no X label reads as a Y label.
                ax.Plot([10.0, 20.0], [lo, hi]);
                if (scale == AxisScale.SymLog) ax.WithSymlogYScale(linthresh); else ax.SetYScale(scale);
                ax.SetYLim(lo, hi).SetYTickLocator(new FixedLocator(fixedTicks));
            })
            .Build();
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);
        // The Y labels are the right-aligned texts (the X labels are centred), read from the bottom up.
        return [.. ctx.Of<DrawTextCall>()
            .Where(a => a.Alignment == TextAlignment.Right)
            .OrderByDescending(a => (a.Position).Y).Select(a => a.Text)];
    }

    /// <summary>Each row is the case matplotlib 3.11.2 printed (see the summary above): the theme, the scale and its limits, the fixed
    /// locator's ticks, the linear threshold of a symmetric-log axis, and the labels read from the bottom up.</summary>
    public static TheoryData<string, AxisScale, double, double, double[], double, string[]> FixedLocators { get; } = new()
    {
        { "v2", AxisScale.Log, 1, 100, [1, 5, 50], 1, ["10⁰", "", ""] },
        { "classic", AxisScale.Log, 1, 100, [1, 5, 50], 1, ["10⁰", "", ""] },
        { "v2", AxisScale.Log, 2, 8, [3, 5], 1, ["3×10⁰", ""] },
        { "v2", AxisScale.SymLog, -1000, 1000, [-100, -5, 0, 5, 100], 2, ["−10²", "", "0", "", "10²"] },
        { "v2", AxisScale.Logit, 0.05, 0.95, [0.1, 0.5, 0.9], 1, ["10⁻¹", "½", "1−10⁻¹"] },
    };

    [Theory]
    [MemberData(nameof(FixedLocators))]
    public void AFixedLocator_LeavesTheScalesFormatterToName_ThePowersAloneOrTheCoefficients(
        string theme, AxisScale scale, double lo, double hi, double[] fixedTicks, double linthresh, string[] expected)
    {
        Assert.Equal(expected, YLabels(theme.AsTheme(), scale, lo, hi, fixedTicks, linthresh));
    }

    [Fact]
    public void AnAxisFormatterOfItsOwn_StillWins()
    {
        var axis = new Axis { Scale = AxisScale.Log, TickLocator = new FixedLocator([1.0, 5]), TickFormatter = new PercentFormatter(1) };

        Assert.IsType<PercentFormatter>(axis.OwnTickSource(isX: false, Theme.Default).Formatter);
    }

    /// <summary>The formatter told no view names every tick, as it always did, and its own scale's ticks (which its locator names by the
    /// same thresholds) read as before.</summary>
    [Fact]
    public void TheLogFormatter_WithNoViewTold_NamesEveryTick()
    {
        var formatter = new LogTickFormatter();

        Assert.Equal("5×10⁰", formatter.Format(5));
        Assert.Equal("10²", formatter.Format(100));
    }
}
