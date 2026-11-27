// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies the AddIndicator fluent API on the figure builder.</summary>
public class AddIndicatorBuilderTests
{
    /// <summary>Verifies that AddIndicator adds the indicator series alongside the existing plot series.</summary>
    [Fact]
    public void AddIndicator_FluentChaining()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0, 3.0, 4.0, 5.0], [10, 20, 30, 40, 50])
                .AddIndicator(new Sma([10, 20, 30, 40, 50], 3)))
            .Build();

        Assert.Equal(2, figure.SubPlots[0].Series.Count); // original + SMA
    }

    /// <summary>An indicator of the caller's own, one that implements <see cref="IIndicator"/> and does not derive from the
    /// library's <c>Indicator</c>, is applied to the axes as it is: there is no offset of the library's to nudge.</summary>
    [Fact]
    public void AddIndicator_AnIndicatorOfYourOwn_IsAppliedToTheAxesAsItIs()
    {
        var axes = new Axes();
        var builder = new AxesBuilder(axes);
        var own = new OwnIndicator();

        var returned = builder.AddIndicator(own);

        Assert.Same(builder, returned);
        Assert.Same(axes, own.AppliedTo);
    }

    private sealed class OwnIndicator : IIndicator
    {
        public Axes? AppliedTo { get; private set; }

        public void Apply(Axes axes) => AppliedTo = axes;
    }

    /// <summary>AddIndicator is the generic entry point for indicators with no dedicated shortcut verb (Macd,
    /// Stochastic, Atr, Ichimoku, ...). In a bar-slot context (a panel with a categorical series: Bar,
    /// Candlestick, ...), every DEDICATED indicator verb (Sma, Rsi, ...) nudges its indicator's X positions onto
    /// the category slot's centre (<c>CategorySlot.CentreOffset</c> = 0.5, via
    /// <c>AxesBuilder.ApplyIndicator&lt;T&gt;</c>) -- but AddIndicator called <c>indicator.Apply(_axes)</c>
    /// directly and skipped that nudge, so the SAME Sma misaligned by half a category slot depending on which
    /// entry point added it.</summary>
    [Fact]
    public void AddIndicator_InBarSlotContext_NudgesOffsetOntoTheCategorySlotCentre()
    {
        var sma = new Sma([10, 20, 30, 40, 50], 3);

        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Bar(["a", "b", "c", "d", "e"], [10, 20, 30, 40, 50]) // categorical series -> bar-slot context
                .AddIndicator(sma))
            .Build();

        Assert.Equal(0.5, sma.Offset); // CategorySlot.CentreOffset -- the same nudge every dedicated verb applies
    }
}
