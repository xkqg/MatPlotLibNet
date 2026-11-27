// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Indicators;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Streaming;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet;

namespace MatPlotLibNet.Tests.Rendering.TickFormatters;

/// <summary>Verifies <see cref="AutoDateFormatter"/> produces the correct format for each <see cref="DateInterval"/>.</summary>
public class AutoDateFormatterTests
{
    private static AutoDateFormatter MakeFormatter(DateInterval interval)
    {
        // Prime the locator by calling Locate over a range that selects the desired interval
        var locator = new AutoDateLocator();
        double seed = new DateTime(2026, 1, 1).ToOADate();
        double span = interval switch
        {
            DateInterval.Years   => 11 * 365.0,
            DateInterval.Months  => 400.0,
            DateInterval.Weeks   => 90.0,
            DateInterval.Days    => 5.0,
            DateInterval.Hours   => 4.0 / 24,
            DateInterval.Minutes => 5.0 / (24 * 60),
            _                    => 10.0 / (24 * 3600) // Seconds
        };
        locator.Locate(seed, seed + span);
        return new AutoDateFormatter(locator);
    }

    [Theory]
    [InlineData(DateInterval.Years, 1, 1, "2026")] // Format_Years_ReturnsYyyy
    [InlineData(DateInterval.Months, 3, 1, "Mar 2026")] // Format_Months_ReturnsMmmYyyy
    [InlineData(DateInterval.Weeks, 1, 5, "Jan 05")] // Format_Weeks_ReturnsMmmDd
    [InlineData(DateInterval.Days, 2, 14, "Feb 14")] // Format_Days_ReturnsMmmDd
    public void Format_WritesADateAsItsIntervalNeeds(DateInterval interval, int month, int day, string expected)
    {
        var formatter = MakeFormatter(interval);
        var result = formatter.Format(new DateTime(2026, month, day).ToOADate());
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(DateInterval.Hours, 9, 0, 0, "09:00")] // Format_Hours_ReturnsHhMm
    [InlineData(DateInterval.Minutes, 14, 35, 0, "14:35")] // Format_Minutes_ReturnsHhMm
    [InlineData(DateInterval.Seconds, 9, 45, 30, "09:45:30")] // Format_Seconds_ReturnsHhMmSs
    public void Format_WritesATimeAsItsIntervalNeeds(DateInterval interval, int hour, int minute, int second, string expected)
    {
        var formatter = MakeFormatter(interval);
        var result = formatter.Format(new DateTime(2026, 1, 1, hour, minute, second).ToOADate());
        Assert.Equal(expected, result);
    }

    /// <summary>A locator that has not located yet holds the first member of the enum, the years, so its formatter writes the year.</summary>
    [Fact]
    public void Format_LocatorThatHasNotLocated_WritesTheYear()
    {
        var formatter = new AutoDateFormatter(new AutoDateLocator());

        Assert.Equal("2026", formatter.Format(new DateTime(2026, 4, 18).ToOADate()));
    }
}
