// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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
        return Thin(ticks, 10, min, max, 0);
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
        return Thin(ticks, 12, min, max, 0);
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
        return Thin(ticks, 10, min, max, TimeSpan.TicksPerDay * 7);
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
        return Thin(ticks, 10, min, max, TimeSpan.TicksPerDay);
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
        return Thin(ticks, 8, min, max, TimeSpan.TicksPerHour);
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
        return Thin(ticks, 6, min, max, TimeSpan.TicksPerMinute);
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
        return Thin(ticks, 6, min, max, TimeSpan.TicksPerSecond);
    }

    /// <summary>Thins a tick list to at most <paramref name="maxCount"/> entries, anchored on ABSOLUTE time.
    ///
    /// <para>It used to keep every k-th entry counting from index 0 — and index 0 is the first tick inside the
    /// window. On a static chart that is harmless. On a <b>sliding</b> window it is not: the moment the window
    /// advances past a tick, index 0 becomes a different instant, the whole selection shifts by one, and every
    /// label on the axis jumps to a new place while the trace beneath it glides smoothly. A rolling strip chart
    /// looked broken for exactly this reason.</para>
    ///
    /// <para>Two things make it phase-stable. The step is derived from the window's <b>span</b> — which is
    /// constant as the window slides — and not from how many ticks happen to fall inside it, which flickers
    /// between n and n+1. And the kept ticks are chosen by their ordinal in <b>absolute time</b>, so a given
    /// clock instant is a labelled tick or it is not, regardless of where the window begins. The labels then
    /// glide out of frame together with the data they belong to.</para>
    ///
    /// <para>The step is also rounded up onto a human ladder (1, 2, 5, 10, 15, 20, 30, 60): an axis labelled
    /// every 8 seconds is arithmetically fine and unreadable to a person.</para></summary>
    private static double[] Thin(List<double> ticks, int maxCount, DateTime min, DateTime max, long unitTicks)
    {
        if (ticks.Count <= maxCount)
        {
            return [.. ticks];
        }

        // Calendar intervals (months, years) have no fixed length, and a window measured in years does not
        // slide frame by frame — index thinning is correct and cheap there.
        if (unitTicks <= 0)
        {
            int stride = (int)Math.Ceiling((double)ticks.Count / maxCount);
            return ticks.Where((_, i) => i % stride == 0).ToArray();
        }

        // The step comes from the window's SPAN, which is constant while the window slides — never from the
        // number of ticks that happen to fall inside it, which flickers between n and n+1 and would change the
        // step (and thus every label) from one frame to the next.
        double units = (max - min).Ticks / (double)unitTicks;
        long step = NiceStep((long)Math.Ceiling(units / maxCount));

        // The ordinal is computed in INTEGER ticks, never by dividing the OLE date by the spacing: an OA date
        // is ~46,000 and a one-second spacing is ~1.16e-5, so that division amplifies the rounding error of a
        // double subtraction into hundreds of seconds — and the modulo below turns into noise. Integers do not
        // have that problem.
        return ticks
            .Where(t => DateTime.FromOADate(t).Ticks / unitTicks % step == 0)
            .ToArray();
    }

    /// <summary>Rounds a step up onto the ladder people actually read clocks on. An axis labelled every eight
    /// seconds is arithmetically fine and unreadable to a human.
    /// <para>The ladder's top rung (60) is the cap by construction: every interval in
    /// <see cref="Locate"/> is chosen so that the window holds at most ~120 units, and the largest
    /// <c>maxCount</c> divisor is 6 — so a raw step above 60 cannot arise.</para></summary>
    private static long NiceStep(long raw)
    {
        long[] ladder = [1, 2, 5, 10, 15, 20, 30, 60];
        return ladder.FirstOrDefault(candidate => raw <= candidate, ladder[^1]);
    }
}
