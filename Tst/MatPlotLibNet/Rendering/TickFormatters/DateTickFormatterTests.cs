// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickFormatters;

namespace MatPlotLibNet.Tests.Rendering.TickFormatters;

/// <summary>Verifies <see cref="DateTickFormatter"/> behavior.</summary>
public class DateTickFormatterTests
{
    /// <summary>Verifies that an OADate value formats to the correct date string.</summary>
    [Fact]
    public void Format_OADate_ReturnsDateString()
    {
        var formatter = new DateTickFormatter();
        // 2026-01-15 = OADate 46032
        double oaDate = new DateTime(2026, 1, 15).ToOADate();
        Assert.Equal("2026-01-15", formatter.Format(oaDate));
    }

    /// <summary>Verifies that a custom format string is applied.</summary>
    [Fact]
    public void Format_CustomFormat_AppliesFormat()
    {
        var formatter = new DateTickFormatter { DateFormat = "MMM yyyy" };
        double oaDate = new DateTime(2026, 3, 1).ToOADate();
        Assert.Equal("Mar 2026", formatter.Format(oaDate));
    }

    /// <summary>Verifies that different dates produce different output.</summary>
    [Fact]
    public void Format_DifferentDates_CorrectOutput()
    {
        var formatter = new DateTickFormatter();
        double d1 = new DateTime(2025, 6, 15).ToOADate();
        double d2 = new DateTime(2025, 12, 31).ToOADate();
        Assert.Equal("2025-06-15", formatter.Format(d1));
        Assert.Equal("2025-12-31", formatter.Format(d2));
    }

    /// <summary>Verifies that the formatter implements ITickFormatter.</summary>
    [Fact]
    public void ImplementsITickFormatter()
    {
        ITickFormatter formatter = new DateTickFormatter();
        Assert.NotNull(formatter);
    }
}
