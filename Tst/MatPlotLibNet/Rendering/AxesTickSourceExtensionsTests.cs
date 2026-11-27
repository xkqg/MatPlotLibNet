// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The one rule for the primary ticks of an axis, read by the axes renderer (the picture) and by the data table (a
/// position a named tick stands on reads as that name), so the two cannot name one place differently.</summary>
public class AxesTickSourceExtensionsTests
{
    private static readonly string[] Names = ["Alpha", "Beta", "Gamma"];

    /// <summary>A log axis names 2, 3, 4, 6 x 10^k beside the power over a view of one power (matplotlib 3.11.2, ylim (2, 80): majors
    /// [10.0], the multiples labelled minors); under the matplotlib themes they are told apart, under the library's own not, and only
    /// the scale's own log or logit locator names minors.</summary>
    [Fact]
    public void MinorNamedAmong_TellsTheLabelledMinors_OfALogOrLogitLocator_UnderTheMatplotlibThemes()
    {
        var log = new TickSource(new LogLocator(), null, false);
        double[] ticks = [2, 3, 10, 20];
        var view = new Range1D(2, 80);

        Assert.Equal([true, true, false, true], log.MinorNamedAmong(ticks, view, Theme.MatplotlibV2)!);
        Assert.Null(log.MinorNamedAmong(ticks, view, Theme.Default));
        Assert.Null(log.MinorNamedAmong(ticks, null, Theme.MatplotlibV2));
        Assert.Null(log.MinorNamedAmong([1.0, 10, 100], new Range1D(1, 100), Theme.MatplotlibV2));
        Assert.Null(new TickSource(new FixedLocator([2.0, 5]), null, false).MinorNamedAmong([2.0, 5], view, Theme.MatplotlibV2));
        Assert.Null(new TickSource(null, null, false).MinorNamedAmong(ticks, view, Theme.MatplotlibV2));

        // A logit view of four ideal ticks or more names its minors; a narrower one lays majors alone (MaxNLocator).
        var logit = new TickSource(new LogitLocator(), null, false);
        Assert.Equal([false, true, false], logit.MinorNamedAmong([0.1, 0.2, 0.5], new Range1D(0.05, 0.95), Theme.MatplotlibV2)!);
        Assert.Null(logit.MinorNamedAmong([0.4, 0.5, 0.6], new Range1D(0.3, 0.7), Theme.MatplotlibV2));
    }

    [Fact]
    public void MajorsAmong_LeavesTheLabelledMinorsOut_AndHandsBackTheSameTicksWhenThereAreNone()
    {
        var log = new TickSource(new LogLocator(), null, false);
        double[] ticks = [2, 3, 10, 20];
        double[] powers = [1, 10, 100];

        Assert.Equal([10.0], log.MajorsAmong(ticks, new Range1D(2, 80), Theme.MatplotlibV2));
        Assert.Same(powers, log.MajorsAmong(powers, new Range1D(1, 100), Theme.MatplotlibV2));
        Assert.Same(ticks, log.MajorsAmong(ticks, new Range1D(2, 80), Theme.Default));
    }

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

        var source = axes.PrimaryTickSource(isX: true, Theme.Default);

        Assert.True(source.NamesCategories);
        Assert.Same(axes.XAxis.TickLocator, source.Locator);
        Assert.Same(axes.XAxis.TickFormatter, source.Formatter);
    }

    [Fact]
    public void ALocatorOfItsOwn_WithoutAFormatter_WritesTheLogScalesLabels()
    {
        // The locator is the axis' own; the log scale's formatter stays, as matplotlib's set_major_locator leaves the formatter its
        // scale set.
        var axes = new Axes();
        axes.YAxis.Scale = AxisScale.Log;
        axes.YAxis.TickLocator = new MultipleLocator(1);

        var source = axes.PrimaryTickSource(isX: false, Theme.Default);

        Assert.False(source.NamesCategories);
        Assert.Same(axes.YAxis.TickLocator, source.Locator);
        Assert.IsType<LogTickFormatter>(source.Formatter);
    }

    [Fact]
    public void ALogScale_BringsItsLocatorAndItsFormatter()
    {
        var axes = new Axes();
        axes.YAxis.Scale = AxisScale.Log;

        var source = axes.PrimaryTickSource(isX: false, Theme.Default);

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

        Assert.IsType<AutoDateFormatter>(axes.PrimaryTickSource(isX: true, Theme.Default).Formatter);
        Assert.Null(axes.PrimaryTickSource(isX: false, Theme.Default).Locator);
    }

    [Fact]
    public void ASymLogScale_BringsItsLocator_AndTheLogFormatter()
    {
        // matplotlib's SymmetricalLogScale sets LogFormatterSciNotation as its major formatter.
        var axes = new Axes();
        axes.XAxis.Scale = AxisScale.SymLog;

        var source = axes.PrimaryTickSource(isX: true, Theme.Default);

        Assert.IsType<SymlogLocator>(source.Locator);
        Assert.IsType<LogTickFormatter>(source.Formatter);
    }

    [Fact]
    public void UprightBars_NameTheirCategoriesAlongX_OnTheSlotCentres()
    {
        var axes = WithBars();

        var x = axes.PrimaryTickSource(isX: true, Theme.Default);
        var y = axes.PrimaryTickSource(isX: false, Theme.Default);

        Assert.True(x.NamesCategories);
        Assert.Equal([0.5, 1.5, 2.5], x.Locator!.Locate(0, 3));
        Assert.Equal("Beta", x.Formatter!.Format(1.5));
        Assert.False(y.NamesCategories);
    }

    [Fact]
    public void BarsThatLieDown_NameTheirCategoriesAlongY()
    {
        var axes = WithBars(BarOrientation.Horizontal);

        Assert.True(axes.PrimaryTickSource(isX: false, Theme.Default).NamesCategories);
        Assert.False(axes.PrimaryTickSource(isX: true, Theme.Default).NamesCategories);
    }

    [Fact]
    public void BarsOnNumericPositions_NameNothing()
    {
        var axes = new Axes();
        axes.AddSeries(new BarSeries(Names, [1, 2, 3]) { XCoordinate = [10, 20, 30] });

        Assert.Equal(new TickSource(null, null, false), axes.PrimaryTickSource(isX: true, Theme.Default));
    }

    [Fact]
    public void AFormatterOfItsOwn_WritesTheCategories()
    {
        var axes = WithBars();
        var own = new NumericTickFormatter();
        axes.XAxis.TickFormatter = own;

        var source = axes.PrimaryTickSource(isX: true, Theme.Default);

        Assert.True(source.NamesCategories);
        Assert.Same(own, source.Formatter);
    }

    [Fact]
    public void AnyLocator_ComesWithTheFormatterOfTheScaleItIsDrawnOn()
    {
        // matplotlib's scale sets the major formatter (set_default_locators_and_formatters) and set_major_locator leaves it: a log axis
        // given a FixedLocator keeps LogFormatterSciNotation (printed: [1, 5, 50] over (1, 100) labelled '10^0', '', ''). A date
        // X axis keeps its AutoDateFormatter too (FixedLocatorOnADateAxis_KeepsTheAutoDateFormatter_AtTheDayScale).
        var symlog = new Axis { Scale = AxisScale.SymLog, TickLocator = new SymlogLocator(100) };
        var log = new Axis { Scale = AxisScale.Log, TickLocator = new LogLocator() };
        var date = new Axis { Scale = AxisScale.Date, TickLocator = new AutoDateLocator() };
        var logOnLinear = new Axis { TickLocator = new LogLocator() };
        var symlogOnLog = new Axis { Scale = AxisScale.Log, TickLocator = new SymlogLocator(1) };
        var fixedOnLogit = new Axis { Scale = AxisScale.Logit, TickLocator = new FixedLocator([0.5]) };
        var fixedOnDate = new Axis { Scale = AxisScale.Date, TickLocator = new FixedLocator([45000.0]) };
        var own = new Axis { Scale = AxisScale.SymLog, TickLocator = new SymlogLocator(100), TickFormatter = new PercentFormatter(1) };

        Assert.IsType<LogTickFormatter>(symlog.OwnTickSource(isX: false, Theme.Default).Formatter);
        Assert.IsType<LogTickFormatter>(log.OwnTickSource(isX: false, Theme.Default).Formatter);
        Assert.IsType<AutoDateFormatter>(date.OwnTickSource(isX: true, Theme.Default).Formatter);
        Assert.Null(date.OwnTickSource(isX: false, Theme.Default).Formatter);
        Assert.Null(logOnLinear.OwnTickSource(isX: false, Theme.Default).Formatter);
        Assert.IsType<LogTickFormatter>(symlogOnLog.OwnTickSource(isX: false, Theme.Default).Formatter);
        Assert.IsType<LogitFormatter>(fixedOnLogit.OwnTickSource(isX: false, Theme.Default).Formatter);
        Assert.IsType<AutoDateFormatter>(fixedOnDate.OwnTickSource(isX: true, Theme.Default).Formatter);
        Assert.IsType<PercentFormatter>(own.OwnTickSource(isX: false, Theme.Default).Formatter);
    }

    /// <summary>A date X axis given a locator of another kind keeps its <see cref="AutoDateFormatter"/>, as matplotlib's does: printed with
    /// 3.11.2, <c>ax.plot(dates, ...)</c> then <c>ax.xaxis.set_major_locator(FixedLocator([date2num(2024-01-15), date2num(2024-02-15)]))</c> keeps
    /// the <c>AutoDateFormatter</c> (the same object), which <c>set_major_locator</c> points at the new locator; a locator with no
    /// <c>_get_unit</c> reads the day scale (<c>locator_unit_scale = 1</c>), so the labels are dates of a day, '2024-01-15' and '2024-02-15'
    /// under the default style ('Jan 15 2024' under classic), never the numbers. The library's formatter writes a day in its own format.</summary>
    [Theory]
    [MemberData(nameof(DateAxisOwners))]
    public void FixedLocatorOnADateAxis_KeepsTheAutoDateFormatter_AtTheDayScale(bool primary)
    {
        double jan15 = new DateTime(2024, 1, 15).ToOADate(), feb15 = new DateTime(2024, 2, 15).ToOADate();
        var axes = new Axes();
        var axis = primary ? axes.XAxis : new Axis();
        axis.Scale = AxisScale.Date;
        axis.TickLocator = new FixedLocator([jan15, feb15]);

        var formatter = (primary ? axes.PrimaryTickSource(isX: true, Theme.Default) : axis.OwnTickSource(isX: true, Theme.Default)).Formatter;

        Assert.IsType<AutoDateFormatter>(formatter);
        Assert.Equal(["Jan 15", "Feb 15"], new[] { formatter!.Format(jan15), formatter.Format(feb15) });
    }

    public static TheoryData<bool> DateAxisOwners => new() { true, false };

    [Fact]
    public void FormatterWith_IsNone_ForALinearScale_AndForADateYAxis()
    {
        Assert.Null(ScaleRule.Linear.Formatter(new MultipleLocator(1), isX: true));
        Assert.Null(ScaleRule.Date.Formatter(new AutoDateLocator(), isX: false));
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

    [Theory]
    [InlineData(true, new[] { 0.5, 1.0 })]  // one position stands between the names
    [InlineData(false, new[] { 0.5, 1.5 })] // along an axis of numbers
    public void NamesAt_NamesNothing(bool isX, double[] positions)
    {
        Assert.Null(WithBars().NamesAt(isX, positions));
    }

    [Fact]
    public void NamesAt_NamesNothing_WhenNoPositionCanBePlaced()
    {
        Assert.Null(WithBars().NamesAt(isX: true, [double.NaN]));
        Assert.Null(WithBars().NamesAt(isX: true, []));
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

        Assert.Equal([0.1, 0.7], located.SlotTickValues(SlotRange, AutoLocator.FullDensityLength, Theme.Default));
        Assert.Equal([0.0, 0.25, 0.5, 0.75, 1.0], spaced.SlotTickValues(SlotRange, AutoLocator.FullDensityLength, Theme.Default));
        Assert.Equal(new AutoLocator(8).Locate(-0.05, 1.05), new Axis().SlotTickValues(SlotRange, AutoLocator.FullDensityLength, Theme.Default));
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

        Assert.IsType<FixedLocator>(own.SlotTickSource(Theme.Default).Locator);
        Assert.IsType<LogTickFormatter>(own.SlotTickSource(Theme.Default).Formatter);
        Assert.IsType<LogLocator>(log.SlotTickSource(Theme.Default).Locator);
        Assert.IsType<LogTickFormatter>(log.SlotTickSource(Theme.Default).Formatter);
        Assert.IsType<SymlogLocator>(symlog.SlotTickSource(Theme.Default).Locator);
        Assert.IsType<LogTickFormatter>(symlog.SlotTickSource(Theme.Default).Formatter);
        Assert.Equal(new TickSource(null, linear.TickFormatter, false), linear.SlotTickSource(Theme.Default));
        Assert.Equal(new LogLocator().LocateInterval(1, 1000), log.SlotTickValues(new DataRange(0, 1, 1, 1000), AutoLocator.FullDensityLength, Theme.Default));
    }
    public static TheoryData<AxisScale, bool> EveryScaleOnEitherAxis()
    {
        var data = new TheoryData<AxisScale, bool>();
        foreach (var scale in Enum.GetValues<AxisScale>()) { data.Add(scale, true); data.Add(scale, false); }
        return data;
    }

    /// <summary>Whether a scale brings a locator is asked without making one, and says what <c>ScaleLocator</c> makes.</summary>
    [Theory]
    [MemberData(nameof(EveryScaleOnEitherAxis))]
    public void BringsScaleLocator_SaysWhetherScaleLocatorMakesOne(AxisScale scale, bool isX)
    {
        var axis = new Axis { Scale = scale };

        Assert.Equal(axis.ScaleRuleUnder(Theme.Default).Locator(isX) is not null, axis.Scale.Rule.BringsLocator(isX));
    }

    /// <summary>The source of an axis with a locator of its own (a date axis put on dates by its verb, drawn every frame of a
    /// live wall) is read without making the locator its scale would bring.</summary>
    [Fact]
    public void TheSourceOfAnAxisWithItsOwnLocator_IsReadWithoutAllocating()
    {
        var axis = new Axis();
        new AxisVerbs(new Axes(), axis, null, []).AutoDates();
        var source = axis.LocatedTickSource(isX: true, Theme.Default); // warm up the JIT

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++) source = axis.LocatedTickSource(isX: true, Theme.Default);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
        Assert.Same(axis.TickLocator, source!.Value.Locator);
        Assert.Same(axis.TickFormatter, source.Value.Formatter);
    }
}
