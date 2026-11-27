// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Rendering.TickFormatters;

/// <summary>The labels of a symmetric-log axis. matplotlib 3.11.2's <c>SymmetricalLogScale.set_default_locators_and_formatters</c>
/// sets <c>axis.set_major_formatter(LogFormatterSciNotation(self.base))</c>, whose <c>__call__</c> (in <c>LogFormatterMathtext</c>)
/// writes <c>if x == 0: return r'$\mathdefault{0}$'</c> and <c>sign_string = '-' if x &lt; 0 else ''</c> before the power
/// (<c>r'$\mathdefault{%s%s^{%d}}$'</c>), a value off a power as <c>%g\times%s^{%d}</c>; mathtext draws that '-' as U+2212 whatever
/// <c>axes.unicode_minus</c> says (parsed glyphs: 0x2212 under both settings). Printed, <c>ax.plot(x, x**3)</c> for x in -50 .. 50 with
/// <c>ax.set_yscale('symlog', linthresh=100)</c>: default style -10^5, -10^4, -10^3, -10^2, 0, 10^2, 10^3, 10^4, 10^5; classic the
/// same with -10^6 and 10^6 (its view rounds out to ±1e6); over -50 .. 50 -5x10^1, 0, 5x10^1. The library wrote plain numbers
/// ("100000", "-1000000").</summary>
public sealed class SymlogTickLabelsTests
{
    [Theory]
    [InlineData(0.0, "0")]
    [InlineData(-100000.0, "−10⁵")]
    [InlineData(-50.0, "−5×10¹")]
    [InlineData(-0.5, "−5×10⁻¹")]
    [InlineData(100.0, "10²")]
    public void TheLogFormatter_WritesZeroAndASign_AsLogFormatterSciNotationDoes(double value, string expected) =>
        Assert.Equal(expected, new LogTickFormatter().Format(value));

    private static List<string> YLabels(Theme theme)
    {
        double[] x = [.. Enumerable.Range(-50, 101).Select(i => (double)i)];
        double[] y = [.. x.Select(v => v * v * v)];
        var svg = Plt.Create().WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax => ax.Plot(x, y).WithSymlogYScale(linthresh: 100)).Build().ToSvg().Xml();
        return [.. svg.Each("text").Where(e => (string?)e.Attribute("text-anchor") == "end").Select(e => e.Value)];
    }

    [Fact]
    public void UnderTheDefaultStyle_ASymlogAxisWritesPowersWithTheirSign_AndZero() =>
        Assert.Equal(["−10⁵", "−10⁴", "−10³", "−10²", "0", "10²", "10³", "10⁴", "10⁵"],
            YLabels(Theme.MatplotlibV2));

    [Fact]
    public void UnderTheClassicStyle_ASymlogAxisWritesPowersWithTheirSign_AndZero() =>
        Assert.Equal(["−10⁶", "−10⁵", "−10⁴", "−10³", "−10²", "0", "10²", "10³", "10⁴", "10⁵",
            "10⁶"], YLabels(Theme.MatplotlibClassic));

    [Fact]
    public void UnderTheLibrarysTheme_TheSymlogScaleBringsTheSameFormatter_AsTheLogScaleDoes()
    {
        // The scale brings its formatter whatever the theme, as a log scale brings LogTickFormatter: no label is a plain number.
        var labels = YLabels(Theme.Default);

        Assert.Contains("0", labels);
        Assert.All(labels.Where(label => label != "0"), label => Assert.Contains("10", label));
        Assert.DoesNotContain(labels, label => label.Contains("000"));
    }
}
