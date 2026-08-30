// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
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
        new("Ait", ["24 %", "25 323", "10 404 KiB"]) { Expanded = true },
        new("Ait.Binance", ["10 %", "14 920", "6 972 KiB"]) { Depth = 1, Url = "/?process=Ait.Binance", Expanded = false },
        new("Ait.Bitvavo", ["2 %", "10 403", "3 432 KiB"]) { Depth = 1, Url = "/?process=Ait.Bitvavo", Expanded = false },
        new("BinanceKline", ["", "14 920", "6 972 KiB"]) { Depth = 2 },
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
        var m = Regex.Match(svg, "<text[^>]*x=\"([-0-9.]+)\"[^>]*>" + Regex.Escape(text) + "<");
        Assert.True(m.Success, $"'{text}' is drawn");
        return double.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static double Y(string svg, string text)
    {
        var m = Regex.Match(svg, "<text[^>]*y=\"([-0-9.]+)\"[^>]*>" + Regex.Escape(text) + "<");
        Assert.True(m.Success, $"'{text}' is drawn");
        return double.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
    }

    [Fact]
    public void EveryRowAndEveryCell_IsDrawn()
    {
        string svg = Render();

        Assert.Contains(">Ait<", svg);
        Assert.Contains(">Ait.Binance<", svg);
        Assert.Contains(">14 920<", svg);
        Assert.Contains(">6 972 KiB<", svg);
        Assert.Contains(">Messages<", svg);
    }

    [Fact]
    public void ADeeperRow_IsIndented()
    {
        string svg = Render();

        Assert.True(X(svg, "Ait.Binance") > X(svg, "Ait"), "a child sits right of its parent");
        Assert.True(X(svg, "BinanceKline") > X(svg, "Ait.Binance"), "and a grandchild right of that");
    }

    [Fact]
    public void RowsAreDrawnInOrder_TopToBottom()
    {
        string svg = Render();

        Assert.True(Y(svg, "Ait.Binance") > Y(svg, "Ait"));
        Assert.True(Y(svg, "BinanceKline") > Y(svg, "Ait.Bitvavo"));
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
    public void ARowWithAUrl_IsAnAnchorWithItsState()
    {
        string svg = Render();

        Assert.Contains("<a href=\"/?process=Ait.Binance\"", svg);
        Assert.Contains("aria-expanded=\"false\"", svg);
        Assert.Contains("cursor:pointer", svg);
    }

    [Fact]
    public void ARowWithoutAUrl_IsNotAnAnchor()
    {
        string svg = Render(rows: [new("BinanceKline", ["", "14 920", ""]) { Depth = 2 }]);

        Assert.DoesNotContain("<a href", svg);
    }

    /// <summary>Every row says how deep it sits, so the grid is navigable and not merely visible (ARIA treegrid).</summary>
    [Fact]
    public void EveryRow_CarriesItsLevel()
    {
        string svg = Render();

        Assert.Contains("aria-level=\"1\"", svg);
        Assert.Contains("aria-level=\"2\"", svg);
        Assert.Contains("aria-level=\"3\"", svg);
    }

    /// <summary>A row that leads somewhere shows the disclosure mark, ▸ closed and ▾ open — the same chevron
    /// the stat tile uses, so one wall has one idiom.</summary>
    [Fact]
    public void AnExpandableRow_CarriesTheChevron()
    {
        string svg = Render();

        Assert.Contains("mpl-treegrid-chevron", svg);
    }

    [Fact]
    public void ARowMayCarryAnAccent_AndTheRestStayNeutral()
    {
        string svg = Render(rows: [new("Ait.Binance", ["120 %"]) { Accent = Colors.Red }]);

        Assert.Contains(Colors.Red.ToHex(), svg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NoRows_DrawsNothingRatherThanAFrame()
    {
        string svg = Render(rows: []);

        Assert.DoesNotContain("<text", svg);
    }

    /// <summary>A headerless grid draws no rule and starts at the first row.</summary>
    [Fact]
    public void WithoutColumnHeaders_TheGridStartsAtItsFirstRow()
    {
        string svg = Plt.Create().WithSize(900, 300)
            .AddSubPlot(1, 1, 1, ax => ax.TreeGrid(Fleet))
            .ToSvg();

        Assert.DoesNotContain(">Messages<", svg);
        Assert.Contains(">Ait<", svg);
    }

    /// <summary>More rows than the region can hold: the overflow is DROPPED, never drawn over the panel below.</summary>
    [Fact]
    public void RowsPastTheRegionsEdge_AreNotDrawn()
    {
        TreeGridRow[] many = [.. Enumerable.Range(0, 200).Select(i => new TreeGridRow($"row{i}", ["1"]))];

        string svg = Plt.Create().WithSize(400, 120)
            .AddSubPlot(1, 1, 1, ax => ax.TreeGrid(many))
            .ToSvg();

        Assert.Contains(">row0<", svg);
        Assert.DoesNotContain(">row199<", svg);
    }

    [Fact]
    public void AnEmptyCell_DrawsNothingRatherThanABlank()
    {
        string svg = Plt.Create().WithSize(900, 300)
            .AddSubPlot(1, 1, 1, ax => ax.TreeGrid([new("BinanceKline", ["", "14 920", ""]) { Depth = 2 }]))
            .ToSvg();

        Assert.Equal(1, Regex.Matches(svg, "text-anchor=\"end\"").Count);
    }

    [Fact]
    public void ARowWithNoCellsAtAll_IsStillDrawn()
    {
        string svg = Render(rows: [new("Ait", [])]);

        Assert.Contains(">Ait<", svg);
    }

    /// <summary>More headers than any row has cells: the extra column is still titled, and nothing throws.</summary>
    [Fact]
    public void MoreHeadersThanCells_IsHarmless()
    {
        string svg = Plt.Create().WithSize(900, 300)
            .AddSubPlot(1, 1, 1, ax => ax.TreeGrid([new("Ait", ["1"])], s => s.ColumnHeaders = ["CPU", "Messages", "Traffic"]))
            .ToSvg();

        Assert.Contains(">Traffic<", svg);
    }

    /// <summary>The grid does not travel — the wall publishes SVG — and the DTO says so rather than pretending.</summary>
    [Fact]
    public void TheDto_CarriesTheDiscriminatorOnly()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.TreeGrid(Fleet)).Build();
        var serializer = new MatPlotLibNet.Serialization.ChartSerializer();

        var back = serializer.FromJson(serializer.ToJson(figure));

        Assert.Empty(back.SubPlots[0].Series.OfType<TreeGridSeries>().Single().Rows);
    }

    [Fact]
    public void TheIndentAndColumnWidth_AreKnobs()
    {
        string narrow = Render(s => { s.IndentWidth = 4; s.ColumnWidth = 60; });
        string wide = Render(s => { s.IndentWidth = 40; s.ColumnWidth = 200; });

        Assert.True(X(wide, "BinanceKline") > X(narrow, "BinanceKline"));
        Assert.True(X(wide, "14 920") < X(narrow, "14 920"), "a wider value column starts further left");
    }

    [Fact]
    public void TheRowHeight_IsAKnob()
    {
        string tight = Render(s => s.RowHeight = 14);
        string loose = Render(s => s.RowHeight = 40);

        Assert.True(Y(loose, "BinanceKline") > Y(tight, "BinanceKline"));
    }
}
