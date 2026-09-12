// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Models;

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

    [Fact]
    public void ADateColumn_BoundaryValue_OutsideTheOleRange_PrintsTheNumber()
    {
        // FromOADate throws below -657435; the table prints what it has rather than dying on one cell.
        var table = new ChartDataTable("t", [Col("when", DataColumnKind.Date)],
            [[DataCell.FromNumber(-1e9)], [DataCell.FromNumber(double.NaN)]]);

        var cells = XElement.Parse(table.ToHtml()).Descendants("th").Skip(1).Select(c => c.Value).ToArray();
        Assert.Equal(["-1000000000", "NaN"], cells);
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
        var lines = csv.Split("\r\n");

        Assert.Equal("i", lines[0]);
        Assert.Equal("1199", lines[1200]);
        Assert.Equal("", lines[^1]);
        Assert.DoesNotContain("\n", csv.Replace("\r\n", ""));
    }

    [Fact]
    public void ToCsv_HeaderWithCommaAndQuote_IsRfc4180Quoted()
    {
        var table = new ChartDataTable("t", [Col("Q1, \"net\"", DataColumnKind.Text)], [[DataCell.FromText("a\r\nb")]]);

        var lines = table.ToCsv().Split("\r\n");

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
        var lines = md.Split('\n');

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

    [Fact]
    public void ToMarkdown_BoundaryValue_APipeInACell_IsEscaped()
    {
        var table = new ChartDataTable("t", [Col("a|b", DataColumnKind.Text)], [[DataCell.FromText("x|y")]]);

        string md = table.ToMarkdown();

        Assert.Contains("| a\\|b |", md);
        Assert.Contains("| x\\|y |", md);
    }

    [Fact]
    public void ToMarkdown_VeryLarge_CapsTheRowsAndSaysHowManyItLeftOut()
    {
        var rows = Enumerable.Range(0, 1005).Select(i => (IReadOnlyList<DataCell>)[DataCell.FromNumber(i)]).ToArray();
        var table = new ChartDataTable("t", [Col("i")], rows);

        var lines = table.ToMarkdown(maxRows: 1000).Split('\n');

        Assert.Equal("| 999 |", lines[^2]);
        Assert.Equal("| … 5 more rows |", lines[^1]);
    }
}
