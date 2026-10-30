// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>A point of a chart is a row of the data it was drawn from, and a reader who selects points on one chart wants the
/// same rows lit on another chart of the same data. That needs an identity for a row that is the author's own, not its position:
/// <c>RowKeys</c>, one string for each point, on the series that owns the per-point data. The keys travel in the JSON, name the
/// rows of the table, and are written on every mark that is one row as <c>data-row</c>, so a script can find the same row
/// wherever it is drawn.</summary>
public class RowKeysTests
{
    private static readonly double[] X = [1, 2, 3, 4];
    private static readonly double[] Y = [10, 20, 30, 40];
    private static readonly string[] Keys = ["a", "b", "c", "d"];

    // ── the property ────────────────────────────────────────────────────────────

    [Fact]
    public void ASeriesHasNoKeys_UntilItIsGivenSome()
    {
        Assert.Null(new Axes().Scatter(X, Y).RowKeys);
        Assert.Null(new Axes().Plot(X, Y).RowKeys);
    }

    [Fact]
    public void Keys_OfTheLengthOfTheData_AreAccepted_AndNullClearsThem()
    {
        var series = new Axes().Scatter(X, Y);

        series.RowKeys = Keys;
        Assert.Equal(Keys, series.RowKeys);

        series.RowKeys = null;
        Assert.Null(series.RowKeys);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(0)]
    public void KeysOfAnotherLength_AreRefusedByName_AndChangeNothing(int length)
    {
        var series = new Axes().Scatter(X, Y);
        series.RowKeys = Keys;

        var refusal = Assert.Throws<ArgumentException>(() => series.RowKeys = [.. Enumerable.Range(0, length).Select(i => "k" + i)]);

        Assert.Equal("RowKeys", refusal.ParamName);
        Assert.Contains($"{length} keys", refusal.Message, StringComparison.Ordinal);
        Assert.Contains("4 points", refusal.Message, StringComparison.Ordinal);
        Assert.Equal(Keys, series.RowKeys);
    }

    [Fact]
    public void AKeyMustBeSomething_ABlankOrRepeatedKeyIsRefused()
    {
        var series = new Axes().Scatter(X, Y);

        Assert.Throws<ArgumentException>(() => series.RowKeys = ["a", "", "c", "d"]);
        Assert.Throws<ArgumentException>(() => series.RowKeys = ["a", "b", "a", "d"]);
        Assert.Null(series.RowKeys);
    }

    // ── the table ───────────────────────────────────────────────────────────────

    [Fact]
    public void TheTable_GainsAColumnOfKeys_WhenTheyAreSet_AndNoneWhenTheyAreNot()
    {
        var scatter = new Axes().Scatter(X, Y);
        Assert.Equal(["x", "y"], scatter.ToDataTable()!.Columns.Select(c => c.Header));

        scatter.RowKeys = Keys;
        var table = scatter.ToDataTable()!;

        Assert.Equal(["row", "x", "y"], table.Columns.Select(c => c.Header));
        Assert.Equal(DataColumnKind.Text, table.Columns[0].Kind);
        Assert.Equal(Keys, table.Rows.Select(r => r[0].Text));
        Assert.Equal([1.0, 2.0, 3.0, 4.0], table.Rows.Select(r => r[1].Number));
    }

    [Fact]
    public void EveryXySeriesThatTablesItself_PutsTheKeysFirst()
    {
        var axes = new Axes();
        var line = axes.Plot(X, Y);
        var bubble = axes.Bubble(X, Y, [1.0, 2, 3, 4]);
        var errorBars = axes.ErrorBar(X, Y, [0.5, 0.5, 0.5, 0.5], [0.5, 0.5, 0.5, 0.5]);
        var area = axes.FillBetween(X, Y, [1.0, 2, 3, 4]);

        foreach (XYSeries series in new XYSeries[] { line, bubble, errorBars, area })
        {
            series.RowKeys = Keys;

            var table = series.ToDataTable()!;

            Assert.Equal("row", table.Columns[0].Header);
            Assert.Equal(Keys, table.Rows.Select(r => r[0].Text));
        }
    }

    // ── the JSON ────────────────────────────────────────────────────────────────

    [Fact]
    public void TheKeys_RoundTripThroughTheJson_ForEverySeriesThatHasThem()
    {
        var figure = new Figure { Width = 400, Height = 300 };
        var axes = figure.AddSubPlot(1, 1, 1);
        axes.Scatter(X, Y).RowKeys = Keys;
        axes.Plot(X, Y).RowKeys = Keys;
        axes.Step(X, Y).RowKeys = Keys;

        var read = new ChartSerializer().FromJson(figure.ToJson());

        Assert.All(read.SubPlots[0].Series.Cast<XYSeries>(), series => Assert.Equal(Keys, series.RowKeys));
    }

    [Fact]
    public void WithoutKeys_TheJsonSaysNothingOfThem()
    {
        var figure = new Figure { Width = 400, Height = 300 };
        figure.AddSubPlot(1, 1, 1).Scatter(X, Y);

        Assert.DoesNotContain("rowKeys", figure.ToJson(), StringComparison.Ordinal);
    }

    [Fact]
    public void AFigureWhoseKeysDoNotMatchItsPoints_IsRefusedWhenItIsRead()
    {
        var figure = new Figure { Width = 400, Height = 300 };
        figure.AddSubPlot(1, 1, 1).Scatter(X, Y).RowKeys = Keys;
        string json = figure.ToJson().Replace("\"rowKeys\":[\"a\",\"b\",\"c\",\"d\"]", "\"rowKeys\":[\"a\",\"b\"]", StringComparison.Ordinal);

        Assert.Throws<ArgumentException>(() => new ChartSerializer().FromJson(json));
    }

    // ── the marks ───────────────────────────────────────────────────────────────

    private static string[] RowsOf(string svg) =>
        [.. Regex.Matches(svg, "data-row=\"([^\"]*)\"").Select(m => m.Groups[1].Value)];

    [Fact]
    public void EveryMarkOfAScatter_CarriesTheKeyOfItsRow_InTheOrderOfTheData()
    {
        var figure = new Figure { Width = 400, Height = 300 };
        figure.AddSubPlot(1, 1, 1).Scatter(X, Y).RowKeys = Keys;

        Assert.Equal(Keys, RowsOf(figure.ToSvg()));
    }

    [Fact]
    public void AMarkThatIsNotDrawn_TakesNoKey_AndTheOthersKeepTheirOwn()
    {
        var figure = new Figure { Width = 400, Height = 300 };
        figure.AddSubPlot(1, 1, 1).Scatter([1.0, 2.0, double.NaN, 4.0], Y).RowKeys = Keys;

        Assert.Equal(["a", "b", "d"], RowsOf(figure.ToSvg()));   // the row of the missing point has no mark, and no key
    }

    [Fact]
    public void EveryBubble_CarriesTheKeyOfItsRow()
    {
        var figure = new Figure { Width = 400, Height = 300 };
        figure.AddSubPlot(1, 1, 1).Bubble(X, Y, [4.0, 9, 16, 25]).RowKeys = Keys;

        Assert.Equal(Keys, RowsOf(figure.ToSvg()));
    }

    [Fact]
    public void ScatterWithKeys_IsNotCulledToTheViewport_SoNoMarkTakesAnotherRowsKey()
    {
        // Culling re-bases the arrays to the points in view; a key is tied to the position in the data like a colour or a size.
        double[] x = [.. Enumerable.Range(0, 200).Select(i => (double)i)];
        double[] y = [.. Enumerable.Range(0, 200).Select(i => (double)(i % 17))];
        string[] keys = [.. Enumerable.Range(0, 200).Select(i => "row" + i)];
        var figure = new Figure { Width = 400, Height = 300 };
        var axes = figure.AddSubPlot(1, 1, 1);
        axes.XAxis.Min = 100;
        axes.XAxis.Max = 150;
        var scatter = axes.Scatter(x, y);
        scatter.MaxDisplayPoints = 20;
        scatter.RowKeys = keys;

        Assert.Equal(keys, RowsOf(figure.ToSvg()));   // every point is drawn, whole, each with its own key
    }

    [Fact]
    public void AKeyWithAQuoteOrAnAmpersand_IsEscaped_SoTheSvgStaysWellFormed()
    {
        var figure = new Figure { Width = 400, Height = 300 };
        figure.AddSubPlot(1, 1, 1).Scatter([1.0, 2.0], [3.0, 4.0]).RowKeys = ["say \"hi\"", "A&B <c>"];

        string svg = figure.ToSvg();

        Assert.Contains("data-row=\"say &quot;hi&quot;\"", svg, StringComparison.Ordinal);
        Assert.Contains("data-row=\"A&amp;B &lt;c&gt;\"", svg, StringComparison.Ordinal);
        var parsed = System.Xml.Linq.XDocument.Parse(svg);   // throws when the markup is not well-formed
        Assert.NotNull(parsed.Root);
    }

    [Fact]
    public void AnyDataAttribute_IsEscaped_NotOnlyTheRowOne()
    {
        var ctx = new SvgRenderContext();

        ctx.SetNextElementData("label", "R&D \"core\"");
        ctx.DrawRectangle(new MatPlotLibNet.Rendering.Rect(0, 0, 10, 10), new MatPlotLibNet.Styling.ShapeStyle(null, null, 0));

        Assert.Contains("data-label=\"R&amp;D &quot;core&quot;\"", ctx.GetOutput(), StringComparison.Ordinal);
    }
}
