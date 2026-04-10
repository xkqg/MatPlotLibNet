// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;

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
            DateInterval.Seconds => 10.0 / (24 * 3600),
            _                    => 5.0
        };
        locator.Locate(seed, seed + span);
        return new AutoDateFormatter(locator);
    }

    [Fact]
    public void Format_Years_ReturnsYyyy()
    {
        var formatter = MakeFormatter(DateInterval.Years);
        var result = formatter.Format(new DateTime(2026, 1, 1).ToOADate());
        Assert.Equal("2026", result);
    }

    [Fact]
    public void Format_Months_ReturnsMmmYyyy()
    {
        var formatter = MakeFormatter(DateInterval.Months);
        var result = formatter.Format(new DateTime(2026, 3, 1).ToOADate());
        Assert.Equal("Mar 2026", result);
    }

    [Fact]
    public void Format_Weeks_ReturnsMmmDd()
    {
        var formatter = MakeFormatter(DateInterval.Weeks);
        var result = formatter.Format(new DateTime(2026, 1, 5).ToOADate());
        Assert.Equal("Jan 05", result);
    }

    [Fact]
    public void Format_Days_ReturnsMmmDd()
    {
        var formatter = MakeFormatter(DateInterval.Days);
        var result = formatter.Format(new DateTime(2026, 2, 14).ToOADate());
        Assert.Equal("Feb 14", result);
    }

    [Fact]
    public void Format_Hours_ReturnsHhMm()
    {
        var formatter = MakeFormatter(DateInterval.Hours);
        var result = formatter.Format(new DateTime(2026, 1, 1, 9, 0, 0).ToOADate());
        Assert.Equal("09:00", result);
    }

    [Fact]
    public void Format_Minutes_ReturnsHhMm()
    {
        var formatter = MakeFormatter(DateInterval.Minutes);
        var result = formatter.Format(new DateTime(2026, 1, 1, 14, 35, 0).ToOADate());
        Assert.Equal("14:35", result);
    }

    [Fact]
    public void Format_Seconds_ReturnsHhMmSs()
    {
        var formatter = MakeFormatter(DateInterval.Seconds);
        var result = formatter.Format(new DateTime(2026, 1, 1, 9, 45, 30).ToOADate());
        Assert.Equal("09:45:30", result);
    }
}
