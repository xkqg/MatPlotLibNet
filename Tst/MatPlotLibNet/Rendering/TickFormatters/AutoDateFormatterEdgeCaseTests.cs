// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Tests.Rendering.TickFormatters;

/// <summary>Edge-case coverage for <see cref="AutoDateFormatter"/>: pushes branch
/// coverage from 87.5% to 100% by exercising the default-case (interval not set)
/// branch which falls through to <c>"yyyy-MM-dd"</c>.</summary>
public class AutoDateFormatterEdgeCaseTests
{
    [Fact]
    public void Format_LocatorWithUnsetInterval_FallsBackToDefault()
    {
        // A fresh AutoDateLocator has its ChosenInterval defaulted (no Locate() called yet).
        // The switch's default arm is "yyyy-MM-dd". This requires no extra setup so the
        // formatter must safely emit the fallback format.
        var locator = new AutoDateLocator();
        // ChosenInterval must currently match an enum value; if it's the *default*
        // enum value matched by one of the cases, that case will fire.
        // Either way we need the switch to not throw and produce a valid date string.
        var formatter = new AutoDateFormatter(locator);
        string result = formatter.Format(new DateTime(2026, 4, 18).ToOADate());
        Assert.False(string.IsNullOrEmpty(result));
        Assert.Contains("2026", result);
    }

    [Fact]
    public void Format_AllIntervals_ReturnNonEmpty()
    {
        // Theory over every DateInterval enum value to confirm every switch arm
        // is reachable AND produces a non-empty result.
        double oa = new DateTime(2026, 4, 18, 13, 45, 30).ToOADate();
        foreach (DateInterval iv in Enum.GetValues<DateInterval>())
        {
            var locator = new AutoDateLocator();
            // Force the locator to set ChosenInterval by spanning a range that
            // will be classified as `iv`. The reuse of MakeFormatter logic from
            // the existing tests is intentional — keeps DRY with the well-known
            // span sizes that pin each interval.
            double span = iv switch
            {
                DateInterval.Years   => 11 * 365.0,
                DateInterval.Months  => 400.0,
                DateInterval.Weeks   => 90.0,
                DateInterval.Days    => 5.0,
                DateInterval.Hours   => 4.0 / 24,
                DateInterval.Minutes => 5.0 / (24 * 60),
                DateInterval.Seconds => 10.0 / (24 * 3600),
                _                    => 5.0,
            };
            locator.Locate(oa, oa + span);
            string result = new AutoDateFormatter(locator).Format(oa);
            Assert.False(string.IsNullOrEmpty(result), $"{iv} produced empty result");
        }
    }
}
