// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Tests.Packaging;

namespace MatPlotLibNet.Tests.Models;

/// <summary>The table is the data said in a form a reader who cannot see the picture can read: columns with a
/// kind, rows of cells, and three textual forms. What it must never do is lie about the data — a row is refused
/// when it does not match the columns, numbers print in full, dates print as dates — and what it must never do to
/// a page is flood it, so the readable forms cap the rows and say how many they left out.</summary>
public class ChartDataTableTests
{
    private static ChartDataColumn Col(string header, DataColumnKind kind = DataColumnKind.Number) => new(header, kind);

    private static ChartDataTable Revenue() => new(
        "Revenue",
        [Col("Quarter"), Col("2026")],
        [
            [DataCell.FromNumber(1), DataCell.FromNumber(12)],
            [DataCell.FromNumber(2), DataCell.FromNumber(18)],
            [DataCell.FromNumber(3), DataCell.FromNumber(15)],
            [DataCell.FromNumber(4), DataCell.FromNumber(22)],
        ]);

    // ---- the shape ---------------------------------------------------------------------------------------------

    [Fact]
    public void ARow_WithTheWrongNumberOfCells_IsRefused()
    {
        var ex = Assert.Throws<ArgumentException>(() => new ChartDataTable(
            "t", [Col("a"), Col("b")], [[DataCell.FromNumber(1)]]));

        Assert.Contains("2", ex.Message);
        Assert.Contains("1", ex.Message);
    }

    [Fact]
    public void ATable_WithoutColumns_IsRefused()
    {
        Assert.Throws<ArgumentException>(() => new ChartDataTable("t", [], []));
    }

    [Fact]
    public void RowCount_EmptyInput_IsZero()
    {
        var table = new ChartDataTable("t", [Col("a")], []);

        Assert.Equal(0, table.RowCount);
        Assert.Empty(table.Rows);
    }

    [Fact]
    public void RowCount_SinglePoint_IsOne()
    {
        var table = new ChartDataTable("t", [Col("a")], [[DataCell.FromNumber(1)]]);

        Assert.Equal(1, table.RowCount);
    }

    // ---- HTML --------------------------------------------------------------------------------------------------

    [Fact]
    public void ToHtml_IsATableWithCaptionHeadAndBody()
    {
        string html = Revenue().ToHtml();
        var table = XElement.Parse(html);

        Assert.Equal("table", table.Name.LocalName);
        Assert.Equal("mpl-data-table", table.Attribute("class")?.Value);
        Assert.Equal("Revenue", table.Element("caption")?.Value);
        var headers = table.Element("thead")!.Descendants("th").ToArray();
        Assert.Equal(["Quarter", "2026"], headers.Select(h => h.Value));
        Assert.All(headers, h => Assert.Equal("col", h.Attribute("scope")?.Value));
        Assert.Equal(4, table.Element("tbody")!.Elements("tr").Count());
    }

    [Fact]
    public void ToHtml_TheFirstCellOfARow_IsARowHeader()
    {
        var row = XElement.Parse(Revenue().ToHtml()).Element("tbody")!.Elements("tr").First();

        var first = row.Elements().First();
        Assert.Equal("th", first.Name.LocalName);
        Assert.Equal("row", first.Attribute("scope")?.Value);
        Assert.Equal("1", first.Value);
        Assert.Equal("td", row.Elements().Last().Name.LocalName);
        Assert.Equal("12", row.Elements().Last().Value);
    }

    [Fact]
    public void ToHtml_WithoutACaption_OmitsTheCaptionElement()
    {
        var table = new ChartDataTable(null, [Col("a")], [[DataCell.FromNumber(1)]]);

        Assert.Null(XElement.Parse(table.ToHtml()).Element("caption"));
    }

    [Fact]
    public void ToHtml_TextIsEscaped_AndHeadersAreTextNodes()
    {
        // A header is never put in an attribute: the one place a quote could break the markup.
        var table = new ChartDataTable("<Q1> & \"profit\"", [Col("a <b> & \"c\"", DataColumnKind.Text)],
            [[DataCell.FromText("x < y & \"z\"")]]);

        string html = table.ToHtml();
        var parsed = XElement.Parse(html);

        Assert.Equal("<Q1> & \"profit\"", parsed.Element("caption")?.Value);
        Assert.Equal("a <b> & \"c\"", parsed.Descendants("th").First().Value);
        Assert.Contains("&lt;", html);
        Assert.Contains("&amp;", html);
        Assert.DoesNotContain("<Q1>", html);
    }

    [Fact]
    public void ToHtml_EmptyInput_HasAHeadAndNoBodyRows()
    {
        var table = new ChartDataTable("t", [Col("a"), Col("b")], []);
        var parsed = XElement.Parse(table.ToHtml());

        Assert.Equal(2, parsed.Element("thead")!.Descendants("th").Count());
        Assert.Empty(parsed.Element("tbody")!.Elements());
    }

    [Fact]
    public void ToHtml_VeryLarge_CapsTheRowsAndSaysHowManyItLeftOut()
    {
        var rows = Enumerable.Range(0, 1200).Select(i => (IReadOnlyList<DataCell>)[DataCell.FromNumber(i)]).ToArray();
        var table = new ChartDataTable("t", [Col("i")], rows);

        var body = XElement.Parse(table.ToHtml()).Element("tbody")!.Elements("tr").ToArray();

        Assert.Equal(ChartDataTable.DefaultMaxRows + 1, body.Length);
        var trailer = body[^1].Element("td")!;
        Assert.Equal("1", trailer.Attribute("colspan")?.Value);
        Assert.Contains("200 more rows", trailer.Value);
    }

    [Fact]
    public void ToHtml_BoundaryValue_ExactlyTheCap_HasNoTrailer()
    {
        var rows = Enumerable.Range(0, 50).Select(i => (IReadOnlyList<DataCell>)[DataCell.FromNumber(i)]).ToArray();
        var table = new ChartDataTable("t", [Col("i")], rows);

        var body = XElement.Parse(table.ToHtml(maxRows: 50)).Element("tbody")!.Elements("tr").ToArray();

        Assert.Equal(50, body.Length);
        Assert.DoesNotContain("more rows", table.ToHtml(maxRows: 50));
    }

    [Fact]
    public void ToHtml_ACapBelowOne_IsRefused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Revenue().ToHtml(maxRows: 0));
    }

    // ---- cell spelling (shared by every form) ------------------------------------------------------------------

    [Theory]
    [InlineData(15.0, "15")]
    [InlineData(0.1, "0.1")]
    [InlineData(1234567.891, "1234567.891")]
    [InlineData(-0.0, "-0")]
    [InlineData(1e21, "1E+21")]
    public void ANumber_PrintsInFullAndInvariant(double value, string expected)
    {
        var table = new ChartDataTable("t", [Col("v")], [[DataCell.FromNumber(value)]]);

        Assert.Equal(expected, XElement.Parse(table.ToHtml()).Descendants("th").Last().Value);
    }

    [Fact]
    public void ANumber_NaN_PrintsNaN()
    {
        var table = new ChartDataTable("t", [Col("v")], [[DataCell.FromNumber(double.NaN)]]);

        Assert.Equal("NaN", XElement.Parse(table.ToHtml()).Descendants("th").Last().Value);
    }

    [Fact]
    public void ANumber_Infinity_PrintsTheWordNotAnOverflow()
    {
        var table = new ChartDataTable("t", [Col("v")],
            [[DataCell.FromNumber(double.PositiveInfinity)], [DataCell.FromNumber(double.NegativeInfinity)]]);

        var cells = XElement.Parse(table.ToHtml()).Descendants("th").Skip(1).Select(c => c.Value).ToArray();
        Assert.Equal(["Infinity", "-Infinity"], cells);
    }

    [Fact]
    public void ADateColumn_PrintsAnOleDateAsADate()
    {
        double midnight = new DateTime(2026, 3, 15).ToOADate();
        double afternoon = new DateTime(2026, 3, 15, 14, 30, 0).ToOADate();
        var table = new ChartDataTable("t", [Col("when", DataColumnKind.Date)],
            [[DataCell.FromNumber(midnight)], [DataCell.FromNumber(afternoon)]]);

        var cells = XElement.Parse(table.ToHtml()).Descendants("th").Skip(1).Select(c => c.Value).ToArray();
        Assert.Equal(["2026-03-15", "2026-03-15 14:30"], cells);
    }

    /// <summary>A value <see cref="DateTime.FromOADate"/> refuses (far below the OLE Automation range, either of its two edges, a
    /// missing value) prints as the number: the table prints what it has rather than dying on one cell.</summary>
    [Fact]
    public void ADateColumn_BoundaryValue_OutsideTheOleRange_PrintsTheNumber()
    {
        var table = new ChartDataTable("t", [Col("when", DataColumnKind.Date)],
            [[DataCell.FromNumber(-1e9)], [DataCell.FromNumber(-657435)], [DataCell.FromNumber(2958466)], [DataCell.FromNumber(double.NaN)]]);

        var cells = XElement.Parse(table.ToHtml()).Descendants("th").Skip(1).Select(c => c.Value).ToArray();
        Assert.Equal(["-1000000000", "-657435", "2958466", "NaN"], cells);
    }

    [Fact]
    public void AnEmptyCell_PrintsNothing()
    {
        var table = new ChartDataTable("t", [Col("a"), Col("b")], [[DataCell.FromNumber(1), DataCell.Empty]]);

        Assert.Equal("", XElement.Parse(table.ToHtml()).Descendants("td").Single().Value);
    }

    // ---- CSV ---------------------------------------------------------------------------------------------------

    [Fact]
    public void ToCsv_HasAHeaderRow_CrlfEndings_AndNoCap()
    {
        var rows = Enumerable.Range(0, 1200).Select(i => (IReadOnlyList<DataCell>)[DataCell.FromNumber(i)]).ToArray();
        var table = new ChartDataTable("t", [Col("i")], rows);

        string csv = table.ToCsv();
        var lines = SourceFile.CrlfLinesOf(csv);

        Assert.Equal("i", lines[0]);
        Assert.Equal("1199", lines[1200]);
        Assert.Equal("", lines[^1]);
        Assert.All(lines, line => Assert.DoesNotContain("\n", line));
    }

    [Fact]
    public void ToCsv_HeaderWithCommaAndQuote_IsRfc4180Quoted()
    {
        var table = new ChartDataTable("t", [Col("Q1, \"net\"", DataColumnKind.Text)], [[DataCell.FromText("a\r\nb")]]);

        var lines = SourceFile.CrlfLinesOf(table.ToCsv());

        Assert.Equal("\"Q1, \"\"net\"\"\"", lines[0]);
        // A field carrying a line break is quoted and keeps its break, so the record is one field over two lines.
        Assert.Equal("\"a", lines[1]);
        Assert.Equal("b\"", lines[2]);
    }

    [Fact]
    public void ToCsv_PlainFields_AreNotQuoted()
    {
        Assert.Equal("Quarter,2026\r\n1,12\r\n2,18\r\n3,15\r\n4,22\r\n", Revenue().ToCsv());
    }

    [Fact]
    public void ToCsv_EmptyInput_IsTheHeaderRowOnly()
    {
        Assert.Equal("a,b\r\n", new ChartDataTable("t", [Col("a"), Col("b")], []).ToCsv());
    }

    // ---- Markdown ----------------------------------------------------------------------------------------------

    [Fact]
    public void ToMarkdown_IsAPipeTableUnderABoldCaption()
    {
        string md = Revenue().ToMarkdown();
        var lines = SourceFile.LinesOf(md);

        Assert.Equal("**Revenue**", lines[0]);
        Assert.Equal("", lines[1]);
        Assert.Equal("| Quarter | 2026 |", lines[2]);
        Assert.Equal("|---|---|", lines[3]);
        Assert.Equal("| 1 | 12 |", lines[4]);
        Assert.Equal("| 4 | 22 |", lines[7]);
    }

    [Fact]
    public void ToMarkdown_WithoutACaption_StartsAtTheHeader()
    {
        var table = new ChartDataTable(null, [Col("a")], [[DataCell.FromNumber(1)]]);

        Assert.StartsWith("| a |", table.ToMarkdown());
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("ToMarkdown_BoundaryValue_APipeInACell_IsEscaped", () =>
        {
            var table = new ChartDataTable("t", [Col("a|b", DataColumnKind.Text)], [[DataCell.FromText("x|y")]]);
            return table.ToMarkdown();
        }, "| a\\|b |", "| x\\|y |"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    [Fact]
    public void ToMarkdown_VeryLarge_CapsTheRowsAndSaysHowManyItLeftOut()
    {
        var rows = Enumerable.Range(0, 1005).Select(i => (IReadOnlyList<DataCell>)[DataCell.FromNumber(i)]).ToArray();
        var table = new ChartDataTable("t", [Col("i")], rows);

        var lines = SourceFile.LinesOf(table.ToMarkdown(maxRows: 1000));

        Assert.Equal("| 999 |", lines[^2]);
        Assert.Equal("| … 5 more rows |", lines[^1]);
    }

    [Fact]
    public void FromIndexedValues_IsOneRowPerValue_ItsIndexThenTheValue()
    {
        var table = ChartDataTable.FromIndexedValues(new("value", Axis: DataAxis.X), [2.5, -1.0]);

        Assert.Equal(["index", "value"], table.Columns.Select(column => column.Header));
        Assert.Equal(DataAxis.X, table.Columns[1].Axis);
        Assert.Equal([[0.0, 2.5], [1.0, -1.0]], table.Rows.Select(row => row.Select(cell => cell.Number).ToArray()));
    }

    /// <summary>A series that stands on categories tabulates them as text under the one header <c>category</c>, then each value column
    /// under its own header, one row for each category the shortest column reaches.</summary>
    [Fact]
    public void ATableOfCategories_IsTheCategoryAsText_ThenEachColumnOfNumbers_ForTheShortestColumn()
    {
        var table = ChartDataTable.ByCategory(["a", "b", "c"], new NumberColumn("value", [1.5, 2.5, 3.5]), new NumberColumn("cells", [7.0, 8.0]));

        Assert.Equal(["category", "value", "cells"], table.Columns.Select(c => c.Header));
        Assert.Equal(DataColumnKind.Text, table.Columns[0].Kind);
        Assert.Equal(2, table.RowCount);
        Assert.Equal("b", table.Rows[1][0].Text);
        Assert.Equal([2.5, 8.0], table.Rows[1].Skip(1).Select(cell => cell.Number));
        Assert.Null(table.Caption);
    }

    /// <summary>Samples that belong to groups are tabulated in the long form: a row for each sample, the name of its group as text first
    /// (the name it was given, else its place counted from 1 behind the word the caller gives), then what the caller says of the sample.</summary>
    [Fact]
    public void ATableOfGroupedSamples_IsARowPerSample_UnderTheNameOfItsGroup()
    {
        double[][] groups = [[1.5, 2.5], [], [9.0]];

        var table = ChartDataTable.OfGroups([Col("group", DataColumnKind.Text), Col("value")], ["first"], "set ", groups, value => [DataCell.FromNumber(value)]);

        Assert.Equal(["group", "value"], table.Columns.Select(c => c.Header));
        Assert.Equal(3, table.RowCount);
        Assert.Equal(["first", "first", "set 3"], table.Rows.Select(row => row[0].Text));
        Assert.Equal([1.5, 2.5, 9.0], table.Rows.Select(row => row[1].Number));
        Assert.Null(table.Caption);
    }

    /// <summary>The long form of a grid is a row per cell, the rows of the grid in order and the cells of each row in order, each row
    /// what the caller says of its cell.</summary>
    [Fact]
    public void ATableOfTheCellsOfAGrid_HasARowPerCellInRowOrder()
    {
        var table = ChartDataTable.PerCell(2, 3, [Col("row"), Col("column")], (r, c) => [DataCell.FromNumber(r), DataCell.FromNumber(c)]);

        Assert.Equal([0.0, 0, 0, 1, 1, 1], table.Rows.Select(row => row[0].Number));
        Assert.Equal([0.0, 1, 2, 0, 1, 2], table.Rows.Select(row => row[1].Number));
        Assert.Null(table.Caption);
    }

    /// <summary>The values of a grid placed at x and y: each cell's x and y (the index of its column or row where the grid names no
    /// coordinate), then its value in each grid, under the headers the caller gives.</summary>
    [Fact]
    public void ATableOverAGrid_PlacesEachCellAtItsXAndYAndListsEachValue()
    {
        double[,] u = { { 1, 2 }, { 3, 4 } }, v = { { 5, 6 }, { 7, 8 } };

        var table = ChartDataTable.OverGrid([10.0], [20.0, 30.0], new("u", u), new("v", v));

        Assert.Equal(["x", "y", "u", "v"], table.Columns.Select(c => c.Header));
        Assert.Equal([DataAxis.X, DataAxis.Y, DataAxis.None, DataAxis.None], table.Columns.Select(c => c.Axis));
        Assert.Equal([10.0, 20, 1, 5], table.Rows[0].Select(cell => cell.Number));
        Assert.Equal([1.0, 30, 4, 8], table.Rows[3].Select(cell => cell.Number));
    }

    /// <summary>A series whose table opens with its point says so with the one pair of headers: x on the X axis, then y (or the series'
    /// own name for it) on the Y axis, then whatever else the series carries, one number column read by index each.</summary>
    [Fact]
    public void ATableOfAPoint_IsXThenY_ThenTheColumnsAfterIt()
    {
        var table = ChartDataTable.XY("height", [1.0, 2.0], [3.0, 4.0], new NumberColumn("size", [5.0, 6.0]), new NumberColumn("lag", [7.0, 8.0], DataAxis.X));

        Assert.Equal(["x", "height", "size", "lag"], table.Columns.Select(c => c.Header));
        Assert.Equal([DataAxis.X, DataAxis.Y, DataAxis.None, DataAxis.X], table.Columns.Select(c => c.Axis));
        Assert.Equal([2.0, 4, 6, 8], table.Rows[1].Select(cell => cell.Number));
    }

    /// <summary>Points that stand over a plane and carry a height are tabulated as x, y and the height under the name the series gives its
    /// heights, then the columns after it that are given.</summary>
    [Fact]
    public void ATableOfPointsWithAHeight_IsXAndYThenTheHeightUnderItsName_ThenTheColumnsAfterIt()
    {
        var table = ChartDataTable.XYZ([1.0, 2.0], [3.0, 4.0], [5.0, 6.0], "depth", new NumberColumn("u", [7.0, 8.0]), null);

        Assert.Equal(["x", "y", "depth", "u"], table.Columns.Select(c => c.Header));
        Assert.Equal([2.0, 4, 6, 8], table.Rows[1].Select(cell => cell.Number));
        Assert.Null(table.Caption);
    }

    /// <summary>A column given as none (an error arm the series was not given, or given as an empty array) adds no column, and the table
    /// is still the point.</summary>
    [Fact]
    public void AColumnThatHoldsNoNumber_AddsNoColumnToTheTableOfAPoint()
    {
        var table = ChartDataTable.XY("y", [1.0], [2.0], NumberColumn.IfAny("low", DataAxis.Y, null), NumberColumn.IfAny("high", DataAxis.Y, []), NumberColumn.IfAny("up", DataAxis.Y, [3.0]));

        Assert.Equal(["x", "y", "up"], table.Columns.Select(c => c.Header));
    }

    /// <summary>The row count of a table of numbers is the shortest column's, so a series whose arrays disagree in length reports the
    /// pairs it has.</summary>
    [Fact]
    public void ATableOfAPoint_HasTheRowsOfItsShortestColumn() =>
        Assert.Equal(1, ChartDataTable.XY("y", [1.0, 2.0], [3.0, 4.0], new NumberColumn("z", [5.0])).RowCount);

    [Fact]
    public void TheHeadersOfAPoint_AreXOnTheXAxis_AndYOnTheYAxisUnderTheNameGiven()
    {
        Assert.Equal(new ChartDataColumn("x", Axis: DataAxis.X), ChartDataColumn.Position(DataAxis.X));
        Assert.Equal(new ChartDataColumn("x1", Axis: DataAxis.X), ChartDataColumn.Position(DataAxis.X, "x1"));
        Assert.Equal(new ChartDataColumn("y", Axis: DataAxis.Y), ChartDataColumn.Position(DataAxis.Y));
        Assert.Equal(new ChartDataColumn("height", Axis: DataAxis.Y), ChartDataColumn.Position(DataAxis.Y, "height"));
    }

    [Fact]
    public void ThePositionOfAColumnNotOnAnAxis_IsRefused() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartDataColumn.Position(DataAxis.None));

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
