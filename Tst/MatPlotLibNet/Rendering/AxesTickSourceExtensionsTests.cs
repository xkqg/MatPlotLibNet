// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The one rule for the primary ticks of an axis, read by the axes renderer (the picture) and by the data table (a
/// position a named tick stands on reads as that name), so the two cannot name one place differently.</summary>
public class AxesTickSourceExtensionsTests
{
    private static readonly string[] Names = ["Alpha", "Beta", "Gamma"];

    private static Axes WithBars(BarOrientation orientation = BarOrientation.Vertical)
    {
        var axes = new Axes();
        axes.AddSeries(new BarSeries(Names, [1, 2, 3]) { Orientation = orientation });
        return axes;
    }

    [Fact]
    public void AnAxisOfItsOwn_WithACategoryFormatter_NamesCategories()
    {
        var axes = new Axes();
        axes.XAxis.TickLocator = new FixedLocator([0.5, 1.5]);
        axes.XAxis.TickFormatter = new CategoryFormatter(["a", "b"]);

        var source = axes.PrimaryTickSource(isX: true);

        Assert.True(source.NamesCategories);
        Assert.Same(axes.XAxis.TickLocator, source.Locator);
        Assert.Same(axes.XAxis.TickFormatter, source.Formatter);
    }

    [Fact]
    public void ALocatorOfItsOwn_WithoutAFormatter_ReadsNumbers_EvenOnALogScale()
    {
        // The locator is the axis' own, so the scale brings no formatter with it.
        var axes = new Axes();
        axes.YAxis.Scale = AxisScale.Log;
        axes.YAxis.TickLocator = new MultipleLocator(1);

        var source = axes.PrimaryTickSource(isX: false);

        Assert.False(source.NamesCategories);
        Assert.Same(axes.YAxis.TickLocator, source.Locator);
        Assert.Null(source.Formatter);
    }

    [Fact]
    public void ALogScale_BringsItsLocatorAndItsFormatter()
    {
        var axes = new Axes();
        axes.YAxis.Scale = AxisScale.Log;

        var source = axes.PrimaryTickSource(isX: false);

        Assert.IsType<LogLocator>(source.Locator);
        Assert.IsType<LogTickFormatter>(source.Formatter);
        Assert.False(source.NamesCategories);
    }

    [Fact]
    public void ADateScale_BringsDateTicks_OnTheXAxisOnly()
    {
        var axes = new Axes();
        axes.XAxis.Scale = AxisScale.Date;
        axes.YAxis.Scale = AxisScale.Date;

        Assert.IsType<AutoDateFormatter>(axes.PrimaryTickSource(isX: true).Formatter);
        Assert.Null(axes.PrimaryTickSource(isX: false).Locator);
    }

    [Fact]
    public void ASymLogScale_BringsItsLocator_AndNoFormatter()
    {
        var axes = new Axes();
        axes.XAxis.Scale = AxisScale.SymLog;

        var source = axes.PrimaryTickSource(isX: true);

        Assert.IsType<SymlogLocator>(source.Locator);
        Assert.Null(source.Formatter);
    }

    [Fact]
    public void UprightBars_NameTheirCategoriesAlongX_OnTheSlotCentres()
    {
        var axes = WithBars();

        var x = axes.PrimaryTickSource(isX: true);
        var y = axes.PrimaryTickSource(isX: false);

        Assert.True(x.NamesCategories);
        Assert.Equal([0.5, 1.5, 2.5], x.Locator!.Locate(0, 3));
        Assert.Equal("Beta", x.Formatter!.Format(1.5));
        Assert.False(y.NamesCategories);
    }

    [Fact]
    public void BarsThatLieDown_NameTheirCategoriesAlongY()
    {
        var axes = WithBars(BarOrientation.Horizontal);

        Assert.True(axes.PrimaryTickSource(isX: false).NamesCategories);
        Assert.False(axes.PrimaryTickSource(isX: true).NamesCategories);
    }

    [Fact]
    public void BarsOnNumericPositions_NameNothing()
    {
        var axes = new Axes();
        axes.AddSeries(new BarSeries(Names, [1, 2, 3]) { XCoordinate = [10, 20, 30] });

        Assert.Equal(new TickSource(null, null, false), axes.PrimaryTickSource(isX: true));
    }

    [Fact]
    public void AFormatterOfItsOwn_WritesTheCategories()
    {
        var axes = WithBars();
        var own = new NumericTickFormatter();
        axes.XAxis.TickFormatter = own;

        var source = axes.PrimaryTickSource(isX: true);

        Assert.True(source.NamesCategories);
        Assert.Same(own, source.Formatter);
    }

    [Fact]
    public void ScaleFormatter_OfNoLocatorOrAPlainOne_IsNone()
    {
        Assert.Null(((ITickLocator?)null).ScaleFormatter());
        Assert.Null(new MultipleLocator(1).ScaleFormatter());
    }

    [Fact]
    public void NamesAt_ReadsEveryPositionOnATick_AsItsName()
    {
        Assert.Equal(["Gamma", "Alpha"], WithBars().NamesAt(isX: true, [2.5, 0.5])!);
    }

    [Fact]
    public void NamesAt_LeavesAGapUnnamed()
    {
        Assert.Equal(["Alpha", null, "Beta"], WithBars().NamesAt(isX: true, [0.5, double.NaN, 1.5])!);
    }

    [Fact]
    public void NamesAt_NamesNothing_WhenOnePositionStandsBetweenTheNames()
    {
        Assert.Null(WithBars().NamesAt(isX: true, [0.5, 1.0]));
    }

    [Fact]
    public void NamesAt_NamesNothing_WhenNoPositionCanBePlaced()
    {
        Assert.Null(WithBars().NamesAt(isX: true, [double.NaN]));
        Assert.Null(WithBars().NamesAt(isX: true, []));
    }

    [Fact]
    public void NamesAt_NamesNothing_AlongAnAxisOfNumbers()
    {
        Assert.Null(WithBars().NamesAt(isX: false, [0.5, 1.5]));
    }

    [Fact]
    public void NamesAt_NamesACategoryALimitLeavesOutOfThePicture()
    {
        // The ticks are taken over the positions, not over the drawn range: a table holds the data.
        var axes = WithBars();
        axes.XAxis.Min = 0;
        axes.XAxis.Max = 1;

        Assert.Equal(Names, axes.NamesAt(isX: true, [0.5, 1.5, 2.5]));
    }

    // ── the ticks a Y axis slot stacked beside the plot draws ──

    private static readonly DataRange SlotRange = new(0, 1, -0.05, 1.05);

    /// <summary>A slot's own locator places its ticks; its spacing does when it has no locator; the default placement
    /// otherwise. Its drawing and the layout's reserve both read this one source.</summary>
    [Fact]
    public void SlotTickValues_AreTheLocatorsTheSpacingsOrTheDefaultPlacement()
    {
        var located = new Axis { TickLocator = new FixedLocator([0.1, 0.7]) };
        var spaced = new Axis();
        spaced.MajorTicks = spaced.MajorTicks with { Spacing = 0.25 };

        Assert.Equal([0.1, 0.7], located.SlotTickValues(SlotRange, AxesRenderer.FullTickDensityLength));
        Assert.Equal([0.0, 0.25, 0.5, 0.75, 1.0], spaced.SlotTickValues(SlotRange, AxesRenderer.FullTickDensityLength));
        Assert.Equal(AxesRenderer.ComputeTickValuesInternal(-0.05, 1.05), new Axis().SlotTickValues(SlotRange, AxesRenderer.FullTickDensityLength));
    }

    /// <summary>A slot's ticks follow the rule the primary's do: its own locator wins; without one its scale brings its
    /// locator, the log scale with its formatter; a linear slot keeps its own formatter, or none.</summary>
    [Fact]
    public void SlotTickSource_IsItsLocator_OrItsScales_OrNone()
    {
        var own = new Axis { Scale = AxisScale.Log, TickLocator = new FixedLocator([1.0, 5.0]) };
        var log = new Axis { Scale = AxisScale.Log };
        var symlog = new Axis { Scale = AxisScale.SymLog, SymLogLinThresh = 2 };
        var linear = new Axis { TickFormatter = new PercentFormatter(1) };

        Assert.IsType<FixedLocator>(own.SlotTickSource().Locator);
        Assert.Null(own.SlotTickSource().Formatter);
        Assert.IsType<LogLocator>(log.SlotTickSource().Locator);
        Assert.IsType<LogTickFormatter>(log.SlotTickSource().Formatter);
        Assert.IsType<SymlogLocator>(symlog.SlotTickSource().Locator);
        Assert.Null(symlog.SlotTickSource().Formatter);
        Assert.Equal(new TickSource(null, linear.TickFormatter, false), linear.SlotTickSource());
        Assert.Equal(new LogLocator().LocateInterval(1, 1000), log.SlotTickValues(new DataRange(0, 1, 1, 1000), AxesRenderer.FullTickDensityLength));
    }
}
