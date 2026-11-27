// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Where events stand inside the cell of their day: the cell is inset a tenth on every edge and the events of a day
/// share its inner width in stripes side by side, in the order given.</summary>
public class CalendarEventLayoutTests
{
    private static GridAxesExtensions.CalendarGrid Of(DateTime first, DateTime last, DayOfWeek start = DayOfWeek.Monday) =>
        GridAxesExtensions.CalendarGrid.Of([first, last], [0, 0], start);

    [Theory]
    [InlineData(2025, 1, 1, 2, 0)]     // a Wednesday: the third row of a week that starts on Monday, in the first column
    [InlineData(2025, 1, 6, 0, 1)]     // the next Monday: the top row of the second column
    [InlineData(2025, 1, 12, 6, 1)]    // the Sunday that closes that week
    public void ADay_StandsInItsRowAndColumn(int year, int month, int day, int row, int column)
    {
        var grid = Of(new DateTime(2025, 1, 1), new DateTime(2025, 3, 31));

        Assert.Equal(row, grid.RowOf(new DateTime(year, month, day)));
        Assert.Equal(column, grid.ColumnOf(new DateTime(year, month, day)));
        Assert.Equal(new DateTime(year, month, day), grid.DateAt(row, column));
    }

    [Fact]
    public void ALeapDay_HasItsOwnCell()
    {
        var grid = Of(new DateTime(2012, 2, 1), new DateTime(2012, 3, 31));

        var leap = new DateTime(2012, 2, 29);   // a Wednesday
        Assert.Equal(2, grid.RowOf(leap));
        Assert.Equal(leap, grid.DateAt(grid.RowOf(leap), grid.ColumnOf(leap)));
        Assert.Equal(new DateTime(2012, 3, 1), grid.DateAt(3, grid.ColumnOf(leap)));   // the Thursday after it, one row down
    }

    [Fact]
    public void ACellOutsideTheSpan_HasNoDate()
    {
        var grid = Of(new DateTime(2025, 1, 1), new DateTime(2025, 1, 10));

        Assert.Null(grid.DateAt(0, 0));          // the Monday before January 1
        Assert.Null(grid.DateAt(6, 1));          // the Sunday after January 10 (a Friday)... Saturday 11th is also outside
        Assert.Null(grid.DateAt(5, 1));
        Assert.Equal(new DateTime(2025, 1, 10), grid.DateAt(4, 1));
    }

    [Fact]
    public void OneEvent_FillsTheInnerWidthOfItsCell()
    {
        var grid = Of(new DateTime(2025, 1, 1), new DateTime(2025, 3, 31));

        var stripe = grid.StripeOf(new DateTime(2025, 1, 1), 0, 1, 1);

        Assert.Equal(0 + 0.1 + 0.4, stripe.Centre, 12);                  // column 0: the middle of the cell
        Assert.Equal(0.8 * GridAxesExtensions.StripeFill, stripe.Width, 12);
        Assert.Equal(0.8, stripe.Height, 12);
        // Row 2 of 7, counted from the top: its slot along Y runs 4..5, so the mark begins a tenth above 4.
        Assert.Equal(4 + 0.1, stripe.Foot, 12);
    }

    [Fact]
    public void TwoEventsOnADay_TakeTwoStripes_SideBySide_InTheOrderGiven_WithoutOverlap()
    {
        var grid = Of(new DateTime(2025, 1, 1), new DateTime(2025, 3, 31));
        var day = new DateTime(2025, 1, 8);   // column 1, row 2

        var first = grid.StripeOf(day, 0, 2, 2);
        var second = grid.StripeOf(day, 1, 2, 2);

        Assert.True(first.Centre < second.Centre);
        Assert.True(first.Centre + first.Width / 2 < second.Centre - second.Width / 2, "the two marks are disjoint");
        Assert.True(first.Centre - first.Width / 2 >= 1 + GridAxesExtensions.CellInset - 1e-12, "inside the cell on the left");
        Assert.True(second.Centre + second.Width / 2 <= 2 - GridAxesExtensions.CellInset + 1e-12, "inside the cell on the right");
        Assert.Equal(first.Foot, second.Foot);
        Assert.Equal(first.Height, second.Height);
    }

    [Fact]
    public void EveryMarkOfACalendar_IsAsWideAsTheNarrowestStripe_WhateverTheEventsOfItsDay()
    {
        var grid = Of(new DateTime(2025, 1, 1), new DateTime(2025, 3, 31));

        var alone = grid.StripeOf(new DateTime(2025, 1, 1), 0, 1, 3);
        var inAPair = grid.StripeOf(new DateTime(2025, 1, 8), 1, 2, 3);
        var inATrio = grid.StripeOf(new DateTime(2025, 1, 15), 2, 3, 3);

        Assert.Equal(inATrio.Width, alone.Width, 12);
        Assert.Equal(inATrio.Width, inAPair.Width, 12);
        Assert.Equal(0 + 0.1 + 0.4, alone.Centre, 12);           // a single event stands in the middle of its cell
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    public void TheStripesOfADay_AreTheSameWidth_AndTileTheInnerWidthEvenly(int count)
    {
        var grid = Of(new DateTime(2025, 1, 1), new DateTime(2025, 3, 31));
        var day = new DateTime(2025, 2, 5);

        var stripes = Enumerable.Range(0, count).Select(i => grid.StripeOf(day, i, count, count)).ToArray();

        Assert.All(stripes, stripe => Assert.Equal(stripes[0].Width, stripe.Width, 12));
        for (int i = 1; i < count; i++)
        {
            Assert.Equal(0.8 / count, stripes[i].Centre - stripes[i - 1].Centre, 12);
        }
    }

    [Fact]
    public void EveryMark_StaysInsideTheCellOfItsDay_WhateverTheRowAndTheWeekStart()
    {
        foreach (var start in Enum.GetValues<DayOfWeek>())
        {
            var grid = Of(new DateTime(2025, 1, 1), new DateTime(2025, 2, 28), start);
            for (var day = new DateTime(2025, 1, 1); day <= new DateTime(2025, 2, 28); day = day.AddDays(1))
            {
                var stripe = grid.StripeOf(day, 0, 1, 3);
                int column = grid.ColumnOf(day), rowFromBottom = GridAxesExtensions.CalendarGrid.DaysPerWeek - 1 - grid.RowOf(day);

                Assert.InRange(stripe.Centre - stripe.Width / 2, column, column + 1);
                Assert.InRange(stripe.Centre + stripe.Width / 2, column, column + 1);
                Assert.InRange(stripe.Foot, rowFromBottom, rowFromBottom + 1);
                Assert.InRange(stripe.Foot + stripe.Height, rowFromBottom, rowFromBottom + 1);
            }
        }
    }

    [Fact]
    public void TheDayCells_AreZeroInsideTheSpan_AndMissingOutsideIt()
    {
        var grid = Of(new DateTime(2025, 1, 1), new DateTime(2025, 1, 10));

        var cells = grid.DayCells();

        Assert.Equal(GridAxesExtensions.CalendarGrid.DaysPerWeek, cells.GetLength(0));
        Assert.Equal(grid.Columns, cells.GetLength(1));
        for (int row = 0; row < cells.GetLength(0); row++)
        {
            for (int column = 0; column < cells.GetLength(1); column++)
            {
                if (grid.DateAt(row, column) is null) Assert.True(double.IsNaN(cells[row, column]), $"({row}, {column}) lies outside the span");
                else Assert.Equal(0.0, cells[row, column]);
            }
        }
    }

    [Fact]
    public void TheEventMarks_AreOneSeriesForEachKind_InTheOrderTheKindsFirstAppear()
    {
        DateTime[] days = [new(2025, 1, 1), new(2025, 1, 1), new(2025, 1, 8)];
        string[] kinds = ["deploy", "incident", "deploy"];
        var grid = GridAxesExtensions.CalendarGrid.Of(days, new double[3], DayOfWeek.Monday);

        var marks = grid.EventMarks(days, kinds);

        Assert.Equal(["deploy", "incident"], marks.Select(series => series.Label));
        // The two events of January 1 share its cell side by side, so the mark is as wide as the narrowest stripe (two to a day).
        var first = grid.StripeOf(days[0], 0, 2, 2);
        var second = grid.StripeOf(days[1], 1, 2, 2);
        var next = grid.StripeOf(days[2], 0, 1, 2);
        Assert.Equal(["2025-01-01", "2025-01-08"], marks[0].Categories);
        Assert.Equal([first.Centre, next.Centre], marks[0].XCoordinate!);
        Assert.Equal([first.Foot, next.Foot], marks[0].StackBaseline!);
        Assert.Equal([first.Height, next.Height], marks[0].Values);
        Assert.Equal(first.Width, marks[0].BarWidth);
        Assert.Equal(["2025-01-01"], marks[1].Categories);
        Assert.Equal([second.Centre], marks[1].XCoordinate!);
        Assert.Null(marks[0].Color);
    }

    [Fact]
    public void AnEventOfNoKind_IsAMarkWithNoLegendName()
    {
        DateTime[] days = [new(2025, 1, 1), new(2025, 1, 2)];
        var grid = GridAxesExtensions.CalendarGrid.Of(days, new double[2], DayOfWeek.Monday);

        var marks = grid.EventMarks(days, [null!, ""]);

        var only = Assert.Single(marks);
        Assert.Null(only.Label);
        Assert.Equal(2, only.Categories.Length);
    }
}
