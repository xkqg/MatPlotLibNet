// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The ticks of an axis: a formatter and a locator are the axis' own, and a date format puts the axis on the date scale.</summary>
public class AxesBuilderTickVerbsTests
{
    [Fact]
    public void AFormatterAndALocator_AreTheAxisOwn()
    {
        var formatter = new EngFormatter();
        var locator = new MaxNLocator(5);

        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.SetYTickFormatter(formatter).SetYTickLocator(locator)).Build().SubPlots[0];

        Assert.Same(formatter, axes.YAxis.TickFormatter);
        Assert.Same(locator, axes.YAxis.TickLocator);
    }

    /// <summary>A date format puts an axis on the date scale with a date formatter of that format.</summary>
    [Fact]
    public void ADateFormat_PutsTheAxisOnTheDateScale_WithAFormatterOfThatFormat()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot([1.0, 2.0, 3.0], [1.0, 2.0, 3.0])
            .SetXDateFormat("yyyy-MM")
            .SetYDateFormat("HH:mm")).Build().SubPlots[0];

        Assert.Equal(AxisScale.Date, axes.XAxis.Scale);
        Assert.Equal(AxisScale.Date, axes.YAxis.Scale);
        Assert.Equal("yyyy-MM", Assert.IsType<DateTickFormatter>(axes.XAxis.TickFormatter).DateFormat);
        Assert.Equal("HH:mm", Assert.IsType<DateTickFormatter>(axes.YAxis.TickFormatter).DateFormat);
    }
}
