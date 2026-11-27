// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A TREE GRID — indented rows with numeric columns, the shape htop uses for process → thread and the
/// one the ARIA spec actually has a role for (<c>treegrid</c>; a treemap has none). It is what a treemap stops
/// being able to do: NN/g puts a treemap's useful depth at two or three levels and says a rectangle too small
/// for its label must fall back to a tooltip — while a fleet where two lanes of twenty-three carry every
/// message would draw two rectangles and twenty-one slivers. Rows compare exactly; areas do not.
/// <para>A row may LINK, so expanding is a URL and needs no script, and it carries <c>aria-level</c> /
/// <c>aria-expanded</c> so the grid is navigable rather than merely visible.</para></summary>
public class TreeGridSeriesTests
{
    private static readonly TreeGridRow[] Fleet =
    [
        new("Svc", ["24 %", "25 323", "10 404 KiB"]) { Expanded = true },
        new("Svc.Billing", ["10 %", "14 920", "6 972 KiB"]) { Depth = 1, Url = "/?process=Svc.Billing", Expanded = false },
        new("Svc.Orders", ["2 %", "10 403", "3 432 KiB"]) { Depth = 1, Url = "/?process=Svc.Orders", Expanded = false },
        new("BillingCycle", ["", "14 920", "6 972 KiB"]) { Depth = 2 },
    ];

    private static string Render(Action<TreeGridSeries>? configure = null, TreeGridRow[]? rows = null) =>
        Plt.Create().WithSize(900, 300)
            .AddSubPlot(1, 1, 1, ax => ax.TreeGrid(rows ?? Fleet, s =>
            {
                s.ColumnHeaders = ["CPU", "Messages", "Traffic"];
                configure?.Invoke(s);
            }))
            .ToSvg();

    private static double X(string svg, string text)
    {
        return svg.TextReading(text).Number("x");
    }

    private static double Y(string svg, string text)
    {
        return svg.TextReading(text).Number("y");
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("EveryRowAndEveryCell_IsDrawn", () => Render(), ">Svc<", ">Svc.Billing<", ">14 920<", ">6 972 KiB<", ">Messages<"),
        new DrawnToken("ARowWithAUrl_IsAnAnchorWithItsState", () => Render(), "<a href=\"/?process=Svc.Billing\"", "aria-expanded=\"false\"", "cursor:pointer"),
        new DrawnToken("ARowWithoutAUrl_IsNotAnAnchor", () => Render(rows: [new("BillingCycle", ["", "14 920", ""]) { Depth = 2 }])) { Absent = ["<a href"] },
        // Every row says how deep it sits, so the grid is navigable and not merely visible (ARIA treegrid).
        new DrawnToken("EveryRow_CarriesItsLevel", () => Render(), "aria-level=\"1\"", "aria-level=\"2\"", "aria-level=\"3\""),
        // A row that leads somewhere shows the disclosure mark, ▸ closed and ▾ open — the same chevron the stat tile uses, so one wall has
        // one idiom.
        new DrawnToken("AnExpandableRow_CarriesTheChevron", () => Render(), "mpl-treegrid-chevron"),
        new DrawnToken("NoRows_DrawsNothingRatherThanAFrame", () => Render(rows: [])) { Absent = ["<text"] },
        // A headerless grid draws no rule and starts at the first row.
        new DrawnToken("WithoutColumnHeaders_TheGridStartsAtItsFirstRow", () => Plt.Create().WithSize(900, 300)
            .AddSubPlot(1, 1, 1, ax => ax.TreeGrid(Fleet))
            .ToSvg(), ">Svc<") { Absent = [">Messages<"] },
        new DrawnToken("ARowWithNoCellsAtAll_IsStillDrawn", () => Render(rows: [new("Svc", [])]), ">Svc<"),
        // More headers than any row has cells: the extra column is still titled, and nothing throws.
        new DrawnToken("MoreHeadersThanCells_IsHarmless", () => Plt.Create().WithSize(900, 300)
            .AddSubPlot(1, 1, 1, ax => ax.TreeGrid([new("Svc", ["1"])], s => s.ColumnHeaders = ["CPU", "Messages", "Traffic"]))
            .ToSvg(), ">Traffic<"),
        // More rows than the region can hold: the overflow is DROPPED, never drawn over the panel below.
        new DrawnToken("RowsPastTheRegionsEdge_AreNotDrawn", () =>
        {
            TreeGridRow[] many = [.. Enumerable.Range(0, 200).Select(i => new TreeGridRow($"row{i}", ["1"]))];
            return Plt.Create().WithSize(400, 120)
                .AddSubPlot(1, 1, 1, ax => ax.TreeGrid(many))
                .ToSvg();
        }, ">row0<") { Absent = [">row199<"] },
        new DrawnToken("ARowMayCarryAnAccent_AndTheRestStayNeutral", () => Render(rows: [new("Svc.Billing", ["120 %"]) { Accent = Colors.Red }]), Colors.Red.ToHex()) { Comparison = StringComparison.OrdinalIgnoreCase },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    [Fact]
    public void ADeeperRow_IsIndented()
    {
        string svg = Render();

        Assert.True(X(svg, "Svc.Billing") > X(svg, "Svc"), "a child sits right of its parent");
        Assert.True(X(svg, "BillingCycle") > X(svg, "Svc.Billing"), "and a grandchild right of that");
    }

    [Fact]
    public void RowsAreDrawnInOrder_TopToBottom()
    {
        string svg = Render();

        Assert.True(Y(svg, "Svc.Billing") > Y(svg, "Svc"));
        Assert.True(Y(svg, "BillingCycle") > Y(svg, "Svc.Orders"));
    }

    /// <summary>The numbers are RIGHT-aligned in their column — the whole reason a grid beats an area: digits
    /// line up and the eye compares them without arithmetic.</summary>
    [Fact]
    public void TheNumbers_AreRightAlignedInTheirColumn()
    {
        string svg = Render();

        Assert.Contains("text-anchor=\"end\"", svg);
        Assert.Equal(X(svg, "14 920"), X(svg, "10 403"));
    }

    [Fact]
    public void AnEmptyCell_DrawsNothingRatherThanABlank()
    {
        string svg = Plt.Create().WithSize(900, 300)
            .AddSubPlot(1, 1, 1, ax => ax.TreeGrid([new("BillingCycle", ["", "14 920", ""]) { Depth = 2 }]))
            .ToSvg();

        Assert.Equal(1, svg.CountOf("text-anchor=\"end\""));
    }

    /// <summary>The grid's rows round-trip through JSON (JSON-roundtrip plan, cluster A) — they used to be left
    /// out, same gap as the treemap/sunburst placeholders, because the wall that renders server-side publishes
    /// SVG and nobody had named this one among them.</summary>
    [Fact]
    public void TheDto_CarriesTheRowsAndTheirDepthUrlAndExpandedState()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.TreeGrid(Fleet)).Build();

        var back = figure.RoundTripped();

        var rows = back.SubPlots[0].Series.OfType<TreeGridSeries>().Single().Rows;
        Assert.Equal(Fleet.Length, rows.Count);
        for (int i = 0; i < Fleet.Length; i++)
        {
            Assert.Equal(Fleet[i].Label, rows[i].Label);
            Assert.Equal(Fleet[i].Cells, rows[i].Cells);
            Assert.Equal(Fleet[i].Depth, rows[i].Depth);
            Assert.Equal(Fleet[i].Url, rows[i].Url);
            Assert.Equal(Fleet[i].Expanded, rows[i].Expanded);
        }
    }

    [Fact]
    public void TheIndentAndColumnWidth_AreKnobs()
    {
        string narrow = Render(s => { s.IndentWidth = 4; s.ColumnWidth = 60; });
        string wide = Render(s => { s.IndentWidth = 40; s.ColumnWidth = 200; });

        Assert.True(X(wide, "BillingCycle") > X(narrow, "BillingCycle"));
        Assert.True(X(wide, "14 920") < X(narrow, "14 920"), "a wider value column starts further left");
    }

    [Fact]
    public void TheRowHeight_IsAKnob()
    {
        string tight = Render(s => s.RowHeight = 14);
        string loose = Render(s => s.RowHeight = 40);

        Assert.True(Y(loose, "BillingCycle") > Y(tight, "BillingCycle"));
    }
}
