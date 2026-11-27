// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.TickLocators;

/// <summary>Verifies <see cref="AutoDateLocator"/> interval selection, tick alignment, and boundary cases.</summary>
public class AutoDateLocatorTests
{
    private static double OA(int year, int month = 1, int day = 1,
        int hour = 0, int minute = 0, int second = 0) =>
        new DateTime(year, month, day, hour, minute, second).ToOADate();

    // --- Interval detection ---

    /// <summary>Every granularity the locator picks, one row each: the window it is picked for, and the one boundary every tick of it lies
    /// on (the format reads the instant down to the field the interval steps by and holds the rest at its start). A member drawn by another
    /// member's walk puts a tick off that boundary or leaves the window with fewer than two.</summary>
    [Theory]
    [InlineData(DateInterval.Years, "2010-01-01T00:00:00", "2021-01-01T00:00:00", "MM-dd HH:mm:ss.fff", "01-01 00:00:00.000")]
    [InlineData(DateInterval.Months, "2024-06-01T00:00:00", "2026-01-01T00:00:00", "dd HH:mm:ss.fff", "01 00:00:00.000")]
    [InlineData(DateInterval.Weeks, "2026-01-01T00:00:00", "2026-04-01T00:00:00", "dddd HH:mm:ss.fff", "Monday 00:00:00.000")]
    [InlineData(DateInterval.Days, "2026-01-01T00:00:00", "2026-01-11T00:00:00", "HH:mm:ss.fff", "00:00:00.000")]
    [InlineData(DateInterval.Hours, "2026-01-01T00:00:00", "2026-01-01T06:00:00", "mm:ss.fff", "00:00.000")]
    [InlineData(DateInterval.Minutes, "2026-01-01T12:00:00", "2026-01-01T12:10:00", "ss.fff", "00.000")]
    [InlineData(DateInterval.Seconds, "2026-01-01T12:00:00", "2026-01-01T12:00:30", "fff", "000")]
    public void EveryInterval_TicksItsWindowOnItsOwnBoundary(DateInterval interval, string first, string last, string boundaryFormat, string boundary)
    {
        var locator = new AutoDateLocator();
        double min = DateTime.Parse(first, System.Globalization.CultureInfo.InvariantCulture).ToOADate();
        double max = DateTime.Parse(last, System.Globalization.CultureInfo.InvariantCulture).ToOADate();

        double[] ticks = locator.Locate(min, max);

        Assert.Equal(interval, locator.ChosenInterval);
        Assert.True(ticks.Length >= 2, $"{interval}: {ticks.Length} ticks");
        Assert.All(ticks, t =>
        {
            Assert.InRange(t, min, max);
            Assert.Equal(boundary, DateTime.FromOADate(t).ToString(boundaryFormat, System.Globalization.CultureInfo.InvariantCulture));
        });
    }

    // --- General contract ---

    [Fact]
    public void Locate_ReturnsAtLeastOneTick()
    {
        var locator = new AutoDateLocator();
        var ticks = locator.Locate(OA(2026, 1, 1), OA(2026, 12, 31));
        Assert.NotEmpty(ticks);
    }

    [Fact]
    public void Locate_ReturnsAscendingOrder()
    {
        var locator = new AutoDateLocator();
        var ticks = locator.Locate(OA(2020), OA(2026));
        for (int i = 1; i < ticks.Length; i++)
            Assert.True(ticks[i] > ticks[i - 1]);
    }

    private static readonly double LastDay = new DateTime(9999, 12, 31).ToOADate();

    [Theory]
    [InlineData(4000.0)]                 // years
    [InlineData(400.0)]                  // months
    [InlineData(100.0)]                  // weeks
    [InlineData(10.0)]                   // days
    [InlineData(10.0 / 24)]              // hours
    [InlineData(10.0 / 1440)]            // minutes
    [InlineData(10.0 / 86400)]           // seconds
    public void AWindowRunningPastTheLastDate_HasTicksOnlyOnDates(double span)
    {
        double min = LastDay + 1.0 - span;            // a span before the end of 9999-12-31
        double max = LastDay + 1.0 + span / 2;        // and half a span past it: the granularity of the span

        double[] ticks = new AutoDateLocator().Locate(min, max);

        Assert.NotEmpty(ticks);
        Assert.All(ticks, tick => Assert.True(tick.IsOleDate));
    }

    [Theory]
    [InlineData(4000.0)]
    [InlineData(10.0)]
    public void AWindowRunningPastTheFirstDate_HasTicksOnlyOnDates(double span)
    {
        double first = new DateTime(1, 1, 1).ToOADate();
        double[] ticks = new AutoDateLocator().Locate(first - 50, first + span);

        Assert.NotEmpty(ticks);
        Assert.All(ticks, tick => Assert.True(tick.IsOleDate));
    }

    [Theory]
    [InlineData(3_000_000.0, 3_000_010.0)]   // wholly after the calendar
    [InlineData(-700_010.0, -700_000.0)]     // wholly before it
    [InlineData(double.NaN, 1.0)]
    [InlineData(5.0, 1.0)]                   // a window turned round
    public void AWindowWithNoDateInIt_HasNoTicks(double min, double max)
    {
        Assert.Empty(new AutoDateLocator().Locate(min, max));
    }

    /// <summary>A span so long that the step runs off the end of the human ladder still produces round ticks —
    /// it rounds up to whole minutes rather than picking an arbitrary number.</summary>
    [Fact]
    public void AVeryLongSecondSpan_RoundsTheStepUpToWholeMinutes()
    {
        var locator = new AutoDateLocator();
        var start = new DateTime(2026, 7, 12, 14, 0, 0).ToOADate();
        var end = new DateTime(2026, 7, 12, 14, 1, 59).ToOADate();   // 119 s — 6 labels wants a ~20 s step

        double[] ticks = locator.Locate(start, end);

        Assert.NotEmpty(ticks);
        foreach (double tick in ticks)
        {
            Assert.True(DateTime.FromOADate(tick).Millisecond < 2);
        }
    }

    /// <summary>A window with only a handful of ticks is left alone — nothing to thin.</summary>
    [Fact]
    public void AShortWindow_KeepsEveryTick()
    {
        var locator = new AutoDateLocator();
        var start = new DateTime(2026, 7, 12, 14, 0, 0).ToOADate();
        var end = new DateTime(2026, 7, 12, 14, 0, 4).ToOADate();

        Assert.NotEmpty(locator.Locate(start, end));
    }

    /// <summary>A window of a few seconds that starts inside a second ticks on the whole seconds in it, the first being the next one.</summary>
    [Fact]
    public void AWindowOfSecondsStartingInsideASecond_TicksFromTheNextWholeSecond()
    {
        var locator = new AutoDateLocator();
        var start = new DateTime(2026, 7, 12, 12, 0, 0, 300);

        double[] ticks = locator.Locate(start.ToOADate(), start.AddSeconds(4).ToOADate());

        Assert.Equal(DateInterval.Seconds, locator.ChosenInterval);
        Assert.Equal("12:00:01", DateTime.FromOADate(ticks[0]).ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture));
    }
}

