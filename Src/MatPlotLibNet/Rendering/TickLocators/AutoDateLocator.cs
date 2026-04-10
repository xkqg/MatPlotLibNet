// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.TickLocators;

/// <summary>Automatically places date tick marks at calendar-aligned boundaries,
/// choosing the granularity based on the visible range.</summary>
/// <remarks>
/// Values are OLE Automation dates (the same representation as <see cref="DateTime.ToOADate"/>).
/// After <see cref="Locate"/> is called, <see cref="ChosenInterval"/> reflects the selected granularity
/// and can be read by <see cref="MatPlotLibNet.Rendering.TickFormatters.AutoDateFormatter"/>.
///
/// Interval selection (OLE Automation: 1 unit = 1 day):
/// <list type="table">
///   <listheader><term>Span</term><description>Interval</description></listheader>
///   <item><term>&gt; 3650 days (≈ 10 years)</term><description>Years — Jan 1st</description></item>
///   <item><term>&gt; 365 days</term><description>Months — 1st of month</description></item>
///   <item><term>&gt; 60 days</term><description>Weeks — Monday midnight</description></item>
///   <item><term>&gt; 2 days</term><description>Days — midnight</description></item>
///   <item><term>&gt; 2 hours</term><description>Hours — :00</description></item>
///   <item><term>&gt; 2 minutes</term><description>Minutes — :00</description></item>
///   <item><term>else</term><description>Seconds — :00</description></item>
/// </list>
/// </remarks>
public sealed class AutoDateLocator : ITickLocator
{
    private const double OneDayInOA    = 1.0;
    private const double OneHourInOA   = 1.0 / 24;
    private const double OneMinuteInOA = 1.0 / (24 * 60);

    /// <summary>Gets the granularity chosen by the most recent call to <see cref="Locate"/>.</summary>
    public DateInterval ChosenInterval { get; private set; }

    /// <inheritdoc />
    public double[] Locate(double min, double max)
    {
        double span = max - min; // in OA days

        ChosenInterval = span switch
        {
            > 3650 * OneDayInOA   => DateInterval.Years,
            > 365  * OneDayInOA   => DateInterval.Months,
            > 60   * OneDayInOA   => DateInterval.Weeks,
            > 2    * OneDayInOA   => DateInterval.Days,
            > 2    * OneHourInOA  => DateInterval.Hours,
            > 2    * OneMinuteInOA => DateInterval.Minutes,
            _                      => DateInterval.Seconds
        };

        var dtMin = DateTime.FromOADate(min);
        var dtMax = DateTime.FromOADate(max);

        return ChosenInterval switch
        {
            DateInterval.Years   => LocateYears(dtMin, dtMax),
            DateInterval.Months  => LocateMonths(dtMin, dtMax),
            DateInterval.Weeks   => LocateWeeks(dtMin, dtMax),
            DateInterval.Days    => LocateDays(dtMin, dtMax),
            DateInterval.Hours   => LocateHours(dtMin, dtMax),
            DateInterval.Minutes => LocateMinutes(dtMin, dtMax),
            DateInterval.Seconds => LocateSeconds(dtMin, dtMax),
            _                    => LocateDays(dtMin, dtMax)
        };
    }

    private static double[] LocateYears(DateTime min, DateTime max)
    {
        var ticks = new List<double>();
        int year = min.Month > 1 || min.Day > 1 ? min.Year + 1 : min.Year;
        while (year <= max.Year)
        {
            ticks.Add(new DateTime(year, 1, 1).ToOADate());
            year++;
        }
        return Thin(ticks, 10);
    }

    private static double[] LocateMonths(DateTime min, DateTime max)
    {
        var ticks = new List<double>();
        var current = new DateTime(min.Year, min.Month, 1);
        if (current < min.Date) current = current.AddMonths(1);
        while (current <= max)
        {
            ticks.Add(current.ToOADate());
            current = current.AddMonths(1);
        }
        return Thin(ticks, 12);
    }

    private static double[] LocateWeeks(DateTime min, DateTime max)
    {
        var ticks = new List<double>();
        var current = min.Date;
        // Advance to next Monday (or stay if already Monday)
        int daysToMonday = ((int)DayOfWeek.Monday - (int)current.DayOfWeek + 7) % 7;
        current = current.AddDays(daysToMonday);
        while (current <= max)
        {
            ticks.Add(current.ToOADate());
            current = current.AddDays(7);
        }
        return Thin(ticks, 10);
    }

    private static double[] LocateDays(DateTime min, DateTime max)
    {
        var ticks = new List<double>();
        var current = min.Date;
        if (min.TimeOfDay != TimeSpan.Zero) current = current.AddDays(1);
        while (current <= max)
        {
            ticks.Add(current.ToOADate());
            current = current.AddDays(1);
        }
        return Thin(ticks, 10);
    }

    private static double[] LocateHours(DateTime min, DateTime max)
    {
        var ticks = new List<double>();
        var current = new DateTime(min.Year, min.Month, min.Day, min.Hour, 0, 0);
        if (min.Minute != 0 || min.Second != 0) current = current.AddHours(1);
        while (current <= max)
        {
            ticks.Add(current.ToOADate());
            current = current.AddHours(1);
        }
        return Thin(ticks, 8);
    }

    private static double[] LocateMinutes(DateTime min, DateTime max)
    {
        var ticks = new List<double>();
        var current = new DateTime(min.Year, min.Month, min.Day, min.Hour, min.Minute, 0);
        if (min.Second != 0) current = current.AddMinutes(1);
        while (current <= max)
        {
            ticks.Add(current.ToOADate());
            current = current.AddMinutes(1);
        }
        return Thin(ticks, 6);
    }

    private static double[] LocateSeconds(DateTime min, DateTime max)
    {
        var ticks = new List<double>();
        var current = new DateTime(min.Year, min.Month, min.Day,
            min.Hour, min.Minute, min.Second);
        if (current < min) current = current.AddSeconds(1);
        while (current <= max)
        {
            ticks.Add(current.ToOADate());
            current = current.AddSeconds(1);
        }
        return Thin(ticks, 6);
    }

    /// <summary>Thins a tick list to at most <paramref name="maxCount"/> evenly-sampled entries.</summary>
    private static double[] Thin(List<double> ticks, int maxCount)
    {
        if (ticks.Count <= maxCount) return [.. ticks];
        int step = (int)Math.Ceiling((double)ticks.Count / maxCount);
        return ticks.Where((_, i) => i % step == 0).ToArray();
    }
}
