// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>A top X axis or a Y axis past the primary linked to the primary (matplotlib's <c>secondary_xaxis</c> and
/// <c>secondary_yaxis</c>): the seat's own builder sets the link (<see cref="SecondaryXAxisBuilder.Link"/>,
/// <see cref="YAxisSlotBuilder.Link"/>), and since a linked axis shows the primary's values in other units it holds no series of
/// its own: the builder refuses a link on an axis that holds a series, and a series on an axis that is linked, whichever comes
/// first, before anything changes.</summary>
public class AxisLinkBuilderTests
{
    private static readonly double[] Hours = [0, 6, 12, 18];
    private static readonly double[] Celsius = [12, 18, 24, 16];

    private static readonly AxisTransform Fahrenheit = AxisTransform.Linear(1.8, 32);

    // ---- the link is set ------------------------------------------------------------------------------------------------

    /// <summary>The example of <see cref="YAxisSlotBuilder.Link"/>: Fahrenheit on the right of a plot in Celsius.</summary>
    [Fact]
    public void YAxisSlotBuilder_Link_SetsTheLinkOfThatAxis_AndOfNoOther()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(Hours, Celsius).SetYLabel("°C")
            .WithAddRightYAxis(fahrenheit => fahrenheit.SetYLabel("°F").Link(AxisTransform.Linear(1.8, 32)))
            .WithAddLeftYAxis(other => other.SetYLabel("other"))).Build();
        var axes = figure.SubPlots[0];

        var link = Assert.IsAssignableFrom<AxisTransform>(axes.YAxes.Right[0].Axis.Link);
        Assert.Equal(212.0, link.Forward(100));
        Assert.Equal("°F", axes.YAxes.Right[0].Axis.Label);
        Assert.Null(axes.YAxis.Link);
        Assert.Null(axes.YAxes.Left[1].Axis.Link);
        Assert.Null(axes.XAxis.Link);
    }

    [Fact]
    public void YAxisSlotBuilder_Link_OnTheLeft_SetsTheLinkOfThatAxis()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(Hours, Celsius)
            .WithAddLeftYAxis(kelvin => kelvin.Link(AxisTransform.Linear(1, 273.15)))).Build();

        Assert.Equal(273.15, figure.SubPlots[0].YAxes.Left[1].Axis.Link!.Forward(0));
    }

    /// <summary>The example of <see cref="SecondaryXAxisBuilder.Link"/>: the period of a frequency along the top.</summary>
    [Fact]
    public void SecondaryXAxisBuilder_Link_SetsTheLinkOfTheTopAxis()
    {
        double[] frequency = [1, 10, 100], power = [3, 2, 1];
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(frequency, power).SetXLabel("frequency (Hz)")
            .WithSecondaryXAxis(top => top.SetXLabel("period (s)").Link(AxisTransform.Reciprocal()))).Build();
        var axes = figure.SubPlots[0];

        Assert.Equal(0.1, axes.SecondaryXAxis!.Link!.Forward(10));
        Assert.Equal("period (s)", axes.SecondaryXAxis.Label);
        Assert.Null(axes.XAxis.Link);
        Assert.Empty(axes.XSecondarySeries);
    }

    /// <summary>A second link replaces the first, as a second label or a second limit does.</summary>
    [Fact]
    public void Link_CalledAgain_ReplacesTheEarlierLink()
    {
        var kelvin = AxisTransform.Linear(1, 273.15);
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(Hours, Celsius)
            .WithAddRightYAxis(right => right.Link(Fahrenheit).Link(kelvin))
            .WithSecondaryXAxis(top => top.Link(Fahrenheit).Link(kelvin))).Build();
        var axes = figure.SubPlots[0];

        Assert.Same(kelvin, axes.YAxes.Right[0].Axis.Link);
        Assert.Same(kelvin, axes.SecondaryXAxis!.Link);
    }

    [Fact]
    public void Link_ReturnsTheSameBuilder_ForChaining()
    {
        YAxisSlotBuilder? slot = null, slotLinked = null;
        SecondaryXAxisBuilder? top = null, topLinked = null;

        Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(Hours, Celsius)
            .WithAddRightYAxis(right => { slot = right; slotLinked = right.Link(Fahrenheit); })
            .WithSecondaryXAxis(t => { top = t; topLinked = t.Link(Fahrenheit); })).Build();

        Assert.Same(slot, slotLinked);
        Assert.Same(top, topLinked);
    }

    // ---- refused ---------------------------------------------------------------------------------------------------------

    [Fact]
    public void Link_RefusesNoTransform_ByName_AndLeavesTheAxisUnlinked()
    {
        var axes = new Axes();
        var slot = axes.YAxes.Right.Add();
        axes.TwinY();

        Assert.Equal("transform", Assert.Throws<ArgumentNullException>(() => new YAxisSlotBuilder(axes, slot).Link(null!)).ParamName);
        Assert.Equal("transform", Assert.Throws<ArgumentNullException>(() => new SecondaryXAxisBuilder(axes).Link(null!)).ParamName);
        Assert.Null(slot.Axis.Link);
        Assert.Null(axes.SecondaryXAxis!.Link);
    }

    /// <summary>A link after a series: the axis keeps its series and gets no link.</summary>
    [Theory]
    [InlineData("Plot")]
    [InlineData("Scatter")]
    [InlineData("StreamingPlot")]
    public void YAxisSlotBuilder_Link_OnAnAxisThatHoldsASeries_IsRefused_AndChangesNothing(string verb)
    {
        var axes = new Axes();
        var slot = axes.YAxes.Right.Add();
        var builder = new YAxisSlotBuilder(axes, slot);
        AddSeries(builder, verb);

        var refusal = Assert.Throws<ArgumentException>(() => builder.Link(Fahrenheit));

        Assert.Equal("transform", refusal.ParamName);
        Assert.StartsWith("This axis holds a series; a linked axis", refusal.Message, StringComparison.Ordinal);
        Assert.Null(slot.Axis.Link);
        Assert.Single(slot.Series);
    }

    /// <summary>A series after a link: the axis keeps its link and gets no series.</summary>
    [Theory]
    [InlineData("Plot", "y")]
    [InlineData("Scatter", "y")]
    [InlineData("StreamingPlot", "capacity")]
    public void YAxisSlotBuilder_ASeriesOnALinkedAxis_IsRefused_AndChangesNothing(string verb, string parameter)
    {
        var axes = new Axes();
        var slot = axes.YAxes.Right.Add();
        var builder = new YAxisSlotBuilder(axes, slot).Link(Fahrenheit);

        var refusal = Assert.Throws<ArgumentException>(() => AddSeries(builder, verb));

        Assert.Equal(parameter, refusal.ParamName);
        Assert.StartsWith("This axis is linked to the primary axis; a linked axis", refusal.Message, StringComparison.Ordinal);
        Assert.Same(Fahrenheit, slot.Axis.Link);
        Assert.Empty(slot.Series);
    }

    [Theory]
    [InlineData("PlotXSecondary")]
    [InlineData("ScatterXSecondary")]
    public void SecondaryXAxisBuilder_Link_OnATopAxisThatHoldsASeries_IsRefused_AndChangesNothing(string verb)
    {
        var axes = new Axes();
        axes.TwinY();
        var builder = new SecondaryXAxisBuilder(axes);
        AddSeries(builder, verb);

        var refusal = Assert.Throws<ArgumentException>(() => builder.Link(Fahrenheit));

        Assert.Equal("transform", refusal.ParamName);
        Assert.StartsWith("This axis holds a series; a linked axis", refusal.Message, StringComparison.Ordinal);
        Assert.Null(axes.SecondaryXAxis!.Link);
        Assert.Single(axes.XSecondarySeries);
    }

    [Theory]
    [InlineData("PlotXSecondary")]
    [InlineData("ScatterXSecondary")]
    public void SecondaryXAxisBuilder_ASeriesOnALinkedTopAxis_IsRefused_AndChangesNothing(string verb)
    {
        var axes = new Axes();
        axes.TwinY();
        var builder = new SecondaryXAxisBuilder(axes).Link(Fahrenheit);

        var refusal = Assert.Throws<ArgumentException>(() => AddSeries(builder, verb));

        Assert.Equal("x", refusal.ParamName);
        Assert.StartsWith("This axis is linked to the primary axis; a linked axis", refusal.Message, StringComparison.Ordinal);
        Assert.Same(Fahrenheit, axes.SecondaryXAxis!.Link);
        Assert.Empty(axes.XSecondarySeries);
    }

    /// <summary>The seat holds the rule, not the builder instance: a second call of the verb that hands out the builder of the
    /// same axis meets the series or the link the first call left there.</summary>
    [Fact]
    public void TheRule_HoldsAcrossTwoBuildersOfTheSameAxis()
    {
        var linkAfterSeries = Record.Exception(() => Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(Hours, Celsius)
            .WithSecondaryXAxis(top => top.PlotXSecondary(Hours, Celsius))
            .WithSecondaryXAxis(top => top.Link(Fahrenheit))).Build());
        var seriesAfterLink = Record.Exception(() => Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(Hours, Celsius)
            .WithSecondaryXAxis(top => top.Link(Fahrenheit))
            .WithSecondaryXAxis(top => top.PlotXSecondary(Hours, Celsius))).Build());

        Assert.Equal("transform", Assert.IsType<ArgumentException>(linkAfterSeries).ParamName);
        Assert.Equal("x", Assert.IsType<ArgumentException>(seriesAfterLink).ParamName);
    }

    private static void AddSeries(YAxisSlotBuilder builder, string verb)
    {
        switch (verb)
        {
            case "Plot": builder.Plot(Hours, Celsius); break;
            case "Scatter": builder.Scatter(Hours, Celsius); break;
            default: builder.StreamingPlot(16); break;
        }
    }

    private static void AddSeries(SecondaryXAxisBuilder builder, string verb)
    {
        if (verb == "PlotXSecondary") builder.PlotXSecondary(Hours, Celsius);
        else builder.ScatterXSecondary(Hours, Celsius);
    }
}
