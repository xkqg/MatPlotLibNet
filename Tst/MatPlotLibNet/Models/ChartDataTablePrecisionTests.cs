// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Models;

/// <summary>A date cell prints at the precision its own value carries: a midnight is a date, a whole minute is
/// a minute, a second is a second, a millisecond is a millisecond. Rounding to the minute looked tidy and threw
/// away exactly what told a control room's 30-second samples apart.</summary>
public class ChartDataTablePrecisionTests
{
    private static string Text(DateTime when) => ChartDataTable.CellText(DataCell.FromNumber(when.ToOADate()), DataColumnKind.Date);

    [Fact]
    public void AMidnight_PrintsAsADate() => Assert.Equal("2026-03-15", Text(new DateTime(2026, 3, 15)));

    [Fact]
    public void AWholeMinute_PrintsToTheMinute() => Assert.Equal("2026-03-15 14:30", Text(new DateTime(2026, 3, 15, 14, 30, 0)));

    [Fact]
    public void AWholeSecond_PrintsToTheSecond() => Assert.Equal("2026-03-15 14:30:30", Text(new DateTime(2026, 3, 15, 14, 30, 30)));

    [Fact]
    public void AMillisecond_PrintsToTheMillisecond() =>
        Assert.Equal("2026-03-15 14:30:30.250", Text(new DateTime(2026, 3, 15, 14, 30, 30, 250)));

    /// <summary>The same rule in every emitter: CSV is the form a machine reads, and it is the one where a
    /// silently rounded timestamp does the most damage.</summary>
    [Fact]
    public void ToCsv_KeepsTheSeconds()
    {
        var table = new ChartDataTable(null, [new("time", DataColumnKind.Date)],
            [[DataCell.FromNumber(new DateTime(2026, 3, 15, 14, 30, 30).ToOADate())]]);

        Assert.Equal("time\r\n2026-03-15 14:30:30\r\n", table.ToCsv());
    }
}
