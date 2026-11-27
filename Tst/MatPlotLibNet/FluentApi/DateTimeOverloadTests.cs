// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Tests.FluentApi;

/// <summary>Verifies DateTime overloads on <see cref="AxesBuilder"/> and <see cref="FigureBuilder"/>.</summary>
public class DateTimeOverloadTests
{
    private static DateTime[] MakeDates(int count) =>
        Enumerable.Range(0, count)
            .Select(i => new DateTime(2026, 1, 1).AddDays(i))
            .ToArray();

    private static double[] MakeValues(int count) =>
        Enumerable.Range(0, count).Select(i => (double)i).ToArray();

    // --- AxesBuilder ---

    /// <summary>Dates, given to the line or the scatter verb, put the X axis on the date scale with the automatic date locator,
    /// once: a second dated series finds it there.</summary>
    [Fact]
    public void Dates_PutTheXAxisOnTheDateScale_WithTheAutomaticLocator_Once()
    {
        var axes = new FigureBuilder()
            .AddSubPlot(1, 1, 1, ax => ax.Plot(MakeDates(10), MakeValues(10)).Scatter(MakeDates(10), MakeValues(10)))
            .Build().SubPlots[0];

        Assert.Equal(2, axes.Series.Count);
        Assert.Equal(AxisScale.Date, axes.XAxis.Scale);
        Assert.IsType<AutoDateLocator>(axes.XAxis.TickLocator);
    }

    [Fact]
    public void AxesBuilder_SetXDateAxis_SetsLocatorAndFormatter()
    {
        var figure = new FigureBuilder()
            .AddSubPlot(1, 1, 1, ax => ax.SetXDateAxis())
            .Build();

        var xAxis = figure.SubPlots[0].XAxis;
        Assert.Equal(AxisScale.Date, xAxis.Scale);
        Assert.IsType<AutoDateLocator>(xAxis.TickLocator);
        Assert.IsType<MatPlotLibNet.Rendering.TickFormatters.AutoDateFormatter>(xAxis.TickFormatter);
    }

    [Fact]
    public void AxesBuilder_Plot_DateTime_ConvertsToOADates()
    {
        var dates = MakeDates(5);
        LineSeries? captured = null;
        new FigureBuilder()
            .AddSubPlot(1, 1, 1, ax => ax.Plot(dates, MakeValues(5), s => captured = s))
            .Build();

        Assert.NotNull(captured);
        Assert.Equal(dates[0].ToOADate(), captured!.XData[0], precision: 6);
        Assert.Equal(dates[^1].ToOADate(), captured.XData[^1], precision: 6);
    }

    [Fact]
    public void FigureBuilder_Plot_DateTime_ThenPlotDouble_DateScaleRetained()
    {
        // Adding a normal (double[]) series after a DateTime series must not clear the date axis
        var dates = MakeDates(5);
        var y = MakeValues(5);
        var figure = new FigureBuilder()
            .Plot(dates, y)
            .Plot(dates.Select(d => d.ToOADate()).ToArray(), y)
            .Build();

        Assert.Equal(AxisScale.Date, figure.SubPlots[0].XAxis.Scale);
    }
}
