// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Tests.Rendering.TickLocators;

/// <summary>Verifies <see cref="AutoDateLocator"/> interval selection, tick alignment, and boundary cases.</summary>
public class AutoDateLocatorTests
{
    private static double OA(int year, int month = 1, int day = 1,
        int hour = 0, int minute = 0, int second = 0) =>
        new DateTime(year, month, day, hour, minute, second).ToOADate();

    // --- Interval detection ---

    [Fact]
    public void Locate_11YearRange_ChoosesYears()
    {
        var locator = new AutoDateLocator();
        locator.Locate(OA(2010), OA(2021));
        Assert.Equal(DateInterval.Years, locator.ChosenInterval);
    }

    [Fact]
    public void Locate_2YearRange_ChoosesMonths()
    {
        var locator = new AutoDateLocator();
        locator.Locate(OA(2024, 1, 1), OA(2026, 1, 1));
        Assert.Equal(DateInterval.Months, locator.ChosenInterval);
    }

    [Fact]
    public void Locate_90DayRange_ChoosesWeeks()
    {
        var locator = new AutoDateLocator();
        locator.Locate(OA(2026, 1, 1), OA(2026, 4, 1));
        Assert.Equal(DateInterval.Weeks, locator.ChosenInterval);
    }

    [Fact]
    public void Locate_10DayRange_ChoosesDays()
    {
        var locator = new AutoDateLocator();
        locator.Locate(OA(2026, 1, 1), OA(2026, 1, 11));
        Assert.Equal(DateInterval.Days, locator.ChosenInterval);
    }

    [Fact]
    public void Locate_6HourRange_ChoosesHours()
    {
        var locator = new AutoDateLocator();
        locator.Locate(OA(2026, 1, 1, 0, 0, 0), OA(2026, 1, 1, 6, 0, 0));
        Assert.Equal(DateInterval.Hours, locator.ChosenInterval);
    }

    [Fact]
    public void Locate_10MinuteRange_ChoosesMinutes()
    {
        var locator = new AutoDateLocator();
        locator.Locate(OA(2026, 1, 1, 12, 0, 0), OA(2026, 1, 1, 12, 10, 0));
        Assert.Equal(DateInterval.Minutes, locator.ChosenInterval);
    }

    [Fact]
    public void Locate_30SecondRange_ChoosesSeconds()
    {
        var locator = new AutoDateLocator();
        locator.Locate(OA(2026, 1, 1, 12, 0, 0), OA(2026, 1, 1, 12, 0, 30));
        Assert.Equal(DateInterval.Seconds, locator.ChosenInterval);
    }

    // --- Year tick alignment ---

    [Fact]
    public void Locate_YearRange_TicksAreJanFirst()
    {
        var locator = new AutoDateLocator();
        var ticks = locator.Locate(OA(2010), OA(2020));
        Assert.All(ticks, t =>
        {
            var dt = DateTime.FromOADate(t);
            Assert.Equal(1,  dt.Month);
            Assert.Equal(1,  dt.Day);
            Assert.Equal(0,  dt.Hour);
        });
    }

    [Fact]
    public void Locate_YearRange_TicksWithinRange()
    {
        var locator = new AutoDateLocator();
        double min = OA(2010);
        double max = OA(2020);
        var ticks = locator.Locate(min, max);
        Assert.All(ticks, t => Assert.InRange(t, min, max));
    }

    // --- Month tick alignment ---

    [Fact]
    public void Locate_MonthRange_TicksAreFirstOfMonth()
    {
        var locator = new AutoDateLocator();
        // ~18 months (>365 days, <3650) → Months interval
        var ticks = locator.Locate(OA(2024, 6, 1), OA(2026, 1, 1));
        Assert.Equal(DateInterval.Months, locator.ChosenInterval);
        Assert.All(ticks, t =>
        {
            var dt = DateTime.FromOADate(t);
            Assert.Equal(1, dt.Day);
            Assert.Equal(0, dt.Hour);
        });
    }

    // --- Week tick alignment ---

    [Fact]
    public void Locate_WeekRange_TicksAreMondays()
    {
        var locator = new AutoDateLocator();
        var ticks = locator.Locate(OA(2026, 1, 1), OA(2026, 3, 31));
        Assert.All(ticks, t =>
        {
            var dt = DateTime.FromOADate(t);
            Assert.Equal(DayOfWeek.Monday, dt.DayOfWeek);
            Assert.Equal(0, dt.Hour);
        });
    }

    // --- Day tick alignment ---

    [Fact]
    public void Locate_DayRange_TicksAreMidnight()
    {
        var locator = new AutoDateLocator();
        var ticks = locator.Locate(OA(2026, 1, 1), OA(2026, 1, 10));
        Assert.All(ticks, t =>
        {
            var dt = DateTime.FromOADate(t);
            Assert.Equal(0, dt.Hour);
            Assert.Equal(0, dt.Minute);
            Assert.Equal(0, dt.Second);
        });
    }

    // --- Hour tick alignment ---

    [Fact]
    public void Locate_HourRange_TicksAreHourBoundaries()
    {
        var locator = new AutoDateLocator();
        var ticks = locator.Locate(OA(2026, 1, 1, 0, 0, 0), OA(2026, 1, 1, 6, 0, 0));
        Assert.All(ticks, t =>
        {
            var dt = DateTime.FromOADate(t);
            Assert.Equal(0, dt.Minute);
            Assert.Equal(0, dt.Second);
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
}
