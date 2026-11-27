// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The rules the builders share instead of each spelling them out: the pinned time window of a time axis, the open
/// frame of a strip panel, and the top X axis a secondary X builder holds from the start. That a verb configures its series
/// once the series is on the axes is held for every verb by <see cref="VerbContractTests"/>.</summary>
public class BuilderSharedRulesTests
{
    private static readonly double[] X = [1.0, 2.0, 3.0];
    private static readonly double[] Y = [4.0, 5.0, 6.0];

    [Fact]
    public void PinTimeWindow_PutsTheXAxisOnTheWindowAsDates_WithTimeAwareTicks()
    {
        var axes = new Axes();
        var builder = new AxesBuilder(axes);
        var end = new DateTime(2026, 3, 1, 12, 0, 0);
        var span = TimeSpan.FromHours(6);

        var returned = builder.PinTimeWindow(end, span);

        Assert.Same(builder, returned);
        Assert.Equal((end - span).ToOADate(), axes.XAxis.Min);
        Assert.Equal(end.ToOADate(), axes.XAxis.Max);
        Assert.Equal(AxisScale.Date, axes.XAxis.Scale);
        Assert.IsType<AutoDateLocator>(axes.XAxis.TickLocator);
    }

    [Fact]
    public void OpenFrame_HidesTheTopAndRightSpines_AndKeepsTheOthers()
    {
        var axes = new Axes();
        var builder = new AxesBuilder(axes);

        var returned = builder.OpenFrame();

        Assert.Same(builder, returned);
        Assert.False(axes.Spines.Top.Visible);
        Assert.False(axes.Spines.Right.Visible);
        Assert.True(axes.Spines.Left.Visible);
        Assert.True(axes.Spines.Bottom.Visible);
    }

    [Fact]
    public void AStreamingSeries_StaysOnTheAxes_WhenItsConfigureThrows()
    {
        var axes = new Axes();

        Assert.Throws<InvalidOperationException>(() =>
            new AxesBuilder(axes).StreamingPlot(8, _ => throw new InvalidOperationException("configure failed")));

        Assert.IsType<StreamingLineSeries>(Assert.Single(axes.Series));
    }

    [Fact]
    public void ASecondaryXAxisBuilder_HoldsTheTopAxisFromTheStart_EvenOverAxesThatHadNone()
    {
        var axes = new Axes();

        new SecondaryXAxisBuilder(axes).SetXLabel("top").SetXLim(1, 9);

        Assert.Equal("top", axes.SecondaryXAxis!.Label);
        Assert.Equal(1, axes.SecondaryXAxis.Min);
        Assert.Equal(9, axes.SecondaryXAxis.Max);
    }

    [Fact]
    public void ASecondaryXAxisBuilder_KeepsTheTopAxisTheAxesAlreadyHad()
    {
        var axes = new Axes().TwinY();
        var top = axes.SecondaryXAxis;

        new SecondaryXAxisBuilder(axes).PlotXSecondary(X, Y);

        Assert.Same(top, axes.SecondaryXAxis);
        Assert.Single(axes.XSecondarySeries);
    }

    [Fact]
    public void AddingNoSeries_IsRefusedAtTheCall_OnTheAxes()
    {
        var axes = new Axes();

        var refusal = Assert.Throws<ArgumentNullException>(() => axes.AddSeries<LineSeries>(null!));

        Assert.Equal("series", refusal.ParamName);
        Assert.Empty(axes.Series);
    }

    [Fact]
    public void AddingNoSeries_IsRefusedAtTheCall_OnTheBuilder()
    {
        var axes = new Axes();

        var refusal = Assert.Throws<ArgumentNullException>(() => new AxesBuilder(axes).AddSeries<LineSeries>(null!));

        Assert.Equal("series", refusal.ParamName);
        Assert.Empty(axes.Series);
    }
}
