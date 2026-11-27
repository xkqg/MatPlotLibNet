// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.TickFormatters;

/// <summary>matplotlib 3.11.2's <c>LogFormatter.set_locs</c>, the one rule which multiples a log scale names: "if numticks &gt;
/// self.minor_thresholds[0]: # Label only bases" (1), "elif numdec &gt; self.minor_thresholds[1]" the coefficients 1, 2, 3, 4, 6, 10
/// (2), else every one (3); "if linthresh is None and vmin &lt;= 0: self._sublabels = {1}"; on a symmetric-log axis "Only count ticks
/// and decades in the logarithmic part of the axis". Each level below is the <c>_sublabels</c> matplotlib prints for that view.</summary>
public sealed class LogLabelThresholdsTests
{
    [Theory]
    [InlineData(1.0, 100.0, 1)]   // two powers in view
    [InlineData(100.0, 1.0, 1)]   // the same view, its ends the other way round
    [InlineData(2.0, 80.0, 2)]    // one power, 1.6 decades
    [InlineData(2.1, 2.9, 3)]     // no power, 0.14 of a decade
    [InlineData(-1.0, 10.0, 1)]   // a log view that reaches 0: the powers alone
    public void NamedOver_ALogView(double lo, double hi, int level) =>
        Assert.Equal(level, LogLabelThresholds.Default.NamedOver(lo, hi));

    [Theory]
    [InlineData(-1000.0, 1000.0, 2.0, 2)] // printed: [-100, -5, 0, 5, 100] -> '-10^2', '', '0', '', '10^2'
    [InlineData(-50.0, 50.0, 100.0, 3)]   // inside the linear part: no decade, every multiple
    [InlineData(-1000.0, 1.0, 2.0, 2)]    // the negative log part alone (matplotlib counts its powers as floor(log|rhs|) - floor(log|vmin|))
    [InlineData(-1.0, 1000.0, 2.0, 1)]    // the positive log part alone: three powers
    public void NamedOver_ASymlogView_CountsItsLogPartsAlone(double lo, double hi, double linThresh, int level) =>
        Assert.Equal(level, LogLabelThresholds.Default.NamedOver(lo, hi, linThresh));

    [Fact]
    public void TheClassicThresholds_NameThePowersAlone_OnceAPowerStandsInView()
    {
        Assert.Equal(1, LogLabelThresholds.Classic.NamedOver(0.5, 3));
        Assert.Equal(2, LogLabelThresholds.Classic.NamedOver(4.2, 4.9));
    }

    [Fact]
    public void ALogFormatterToldAView_BlanksATickItsCoefficientIsNotNamedFor_OnEitherSideOfZero()
    {
        var formatter = ((ITickSetFormatter)new LogTickFormatter()).ForTicks([-100, -5, 0, 5, 100], new Range1D(-1000, 1000), 2,
            Theme.MatplotlibV2);

        Assert.Equal(["−10²", "", "0", "", "10²"], new[] { -100.0, -5, 0, 5, 100 }.Select(formatter.Format));
    }

    [Fact]
    public void ALogFormatterToldNoView_IsItself_AndNamesEveryTick()
    {
        ITickSetFormatter formatter = new LogTickFormatter();

        Assert.Same(formatter, formatter.ForTicks([5.0], null, null, Theme.MatplotlibV2));
        Assert.Equal("5×10⁰", ((ITickFormatter)formatter).Format(5));
    }
}
