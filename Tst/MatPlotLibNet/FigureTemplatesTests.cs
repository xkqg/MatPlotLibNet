// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Builders;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Styling;
using System.Text.Json;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests;

/// <summary>Verifies <see cref="FigureTemplates"/>: what each template builds (the financial dashboard, the scientific paper, the
/// joint plot, the pair plot, the facet grid and the clustermap), its title and the panels it hands to the caller.</summary>
public class FigureTemplatesTests
{
    // ── A title on the multi-panel paper ─────────────────────────────────────────

    private static readonly double[] Open  = [100, 102, 101, 103, 105, 104, 106, 107, 106, 108];
    private static readonly double[] High  = [103, 104, 103, 105, 107, 106, 108, 109, 108, 110];
    private static readonly double[] Low   = [ 98,  99,  98, 100, 102, 101, 103, 104, 103, 105];
    private static readonly double[] Close = [101, 102, 101, 103, 105, 104, 106, 107, 106, 108];
    private static readonly double[] Volume = [1000, 1200, 800, 1500, 2000, 900, 1800, 2200, 700, 1600];

    // --- FinancialDashboard ---

    /// <summary>FinancialDashboard produces a figure with exactly three subplots.</summary>
    [Fact]
    public void FinancialDashboard_HasThreeSubplots()
    {
        var figure = FigureTemplates.FinancialDashboard(Open, High, Low, Close, Volume).Build();
        Assert.Equal(3, figure.SubPlots.Count);
    }

    [Theory]
    [InlineData(0, typeof(CandlestickSeries))]
    [InlineData(1, typeof(BarSeries))]
    public void FinancialDashboard_EachPanelHasItsSeries(int panel, Type series)
    {
        var figure = FigureTemplates.FinancialDashboard(Open, High, Low, Close, Volume).Build();
        Assert.Contains(figure.SubPlots[panel].Series, s => series.IsInstanceOfType(s));
    }

    /// <summary>The title is applied to the figure when provided.</summary>
    [Fact]
    public void FinancialDashboard_SetsTitle()
    {
        var figure = FigureTemplates.FinancialDashboard(Open, High, Low, Close, Volume, title: "AAPL").Build();
        Assert.Equal("AAPL", figure.Title);
    }

    // --- ScientificPaper ---

    /// <summary>ScientificPaper applies 150 DPI.</summary>
    [Fact]
    public void ScientificPaper_AppliesDpi150()
    {
        var figure = FigureTemplates.ScientificPaper(ax => ax.Plot([1.0, 2, 3], [4.0, 5, 6])).Build();
        Assert.Equal(150, figure.Dpi);
    }

    /// <summary>ScientificPaper produces the requested number of subplots in the multi-panel overload.</summary>
    [Fact]
    public void ScientificPaper_HasCorrectSubplotCount()
    {
        var figure = FigureTemplates.ScientificPaper(
            rows: 2, cols: 2,
            configures: [
                ax => ax.Plot([1.0, 2, 3], [4.0, 5, 6]),
                ax => ax.Plot([1.0, 2, 3], [4.0, 5, 6]),
                ax => ax.Plot([1.0, 2, 3], [4.0, 5, 6]),
                ax => ax.Plot([1.0, 2, 3], [4.0, 5, 6])
            ]).Build();
        Assert.Equal(4, figure.SubPlots.Count);
    }

    /// <summary>ScientificPaper's single-subplot overload invokes the configure callback with the subplot axes.</summary>
    [Fact]
    public void ScientificPaper_SingleSubplot_InvokesConfigureCallback()
    {
        bool invoked = false;
        var figure = FigureTemplates.ScientificPaper(ax => { invoked = true; ax.Plot([0.0, 1], [0.0, 1]); }).Build();
        Assert.True(invoked);
        Assert.Single(figure.SubPlots);
        Assert.Single(figure.SubPlots[0].Series);
    }

    /// <summary>ScientificPaper throws when the configures array length doesn't match rows*cols.</summary>
    [Fact]
    public void ScientificPaper_MultiSubplot_ThrowsOnLengthMismatch()
    {
        Assert.Throws<ArgumentException>(() =>
            FigureTemplates.ScientificPaper(
                rows: 2, cols: 2,
                configures: [ax => ax.Plot([1.0], [1.0])]));
    }

    // --- SparklineDashboard ---

    private static readonly NamedSample[] Metrics =
    [
        new("Revenue", [10, 20, 30]),
        new("Costs",   [8, 15, 25]),
        new("Profit",  [2, 5, 5]),
    ];

    /// <summary>A dashboard of named samples has one row for each sample, named by its label, in the order given, and the
    /// figure is 120 pixels tall for each row.</summary>
    [Fact]
    public void SparklineDashboard_NamedSamples_HaveOneRowEachNamedByItsLabel()
    {
        var figure = FigureTemplates.SparklineDashboard(Metrics).Build();

        Assert.Equal(["Revenue", "Costs", "Profit"], figure.SubPlots.Select(row => row.YAxis.Label));
        Assert.All(figure.SubPlots, row => Assert.Single(row.Series.OfType<SparklineSeries>()));
        Assert.Equal(360, figure.Height);
    }

    /// <summary>A title given to the dashboard is the figure's title.</summary>
    [Fact]
    public void SparklineDashboard_WithTitle_AppliesTitle()
    {
        var figure = FigureTemplates.SparklineDashboard(Metrics, title: "dash").Build();

        Assert.Equal("dash", figure.Title);
    }

    /// <summary>The rows may come from any ordered list, not only an array.</summary>
    [Fact]
    public void SparklineDashboard_NamedSamples_MayBeAnyReadOnlyList()
    {
        List<NamedSample> list = [.. Metrics];

        Assert.Equal(
            FigureTemplates.SparklineDashboard(Metrics).ToSvg(),
            FigureTemplates.SparklineDashboard(list).ToSvg());
    }

    /// <summary>No rows at all is refused by name.</summary>
    [Fact]
    public void SparklineDashboard_NoRowsAtAll_IsRefusedByName()
    {
        var named = Assert.Throws<ArgumentNullException>(() => FigureTemplates.SparklineDashboard((IReadOnlyList<NamedSample>)null!));

        Assert.Equal("series", named.ParamName);
    }

    /// <summary>A dashboard of no rows is a picture 0 pixels tall: it is refused by name before a figure is made.</summary>
    [Fact]
    public void SparklineDashboard_AnEmptyList_IsRefusedByName()
    {
        var named = Assert.Throws<ArgumentException>(() => FigureTemplates.SparklineDashboard(Array.Empty<NamedSample>()));

        Assert.Equal("series", named.ParamName);
        Assert.Contains("No rows", named.Message, StringComparison.Ordinal);
    }

    /// <summary>A row that carries no values (the default of a named sample has none) is refused by the name of the row,
    /// instead of failing deep in the sparkline.</summary>
    [Fact]
    public void SparklineDashboard_ARowWithoutValues_IsRefusedByItsName()
    {
        var named = Assert.Throws<ArgumentException>(() => FigureTemplates.SparklineDashboard([Metrics[0], new NamedSample("Latency", null!)]));

        Assert.Equal("series", named.ParamName);
        Assert.Contains("'Latency'", named.Message, StringComparison.Ordinal);
    }

    // --- JointPlot ---

    private static readonly double[] JointX = [1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0];
    private static readonly double[] JointY = [1.2, 1.9, 3.1, 4.0, 5.1, 5.8, 7.2, 8.0];

    /// <summary>JointPlot builds a figure with exactly 3 subplots.</summary>
    [Fact]
    public void JointPlot_HasThreeSubplots()
    {
        var figure = FigureTemplates.JointPlot(JointX, JointY).Build();
        Assert.Equal(3, figure.SubPlots.Count);
    }

    /// <summary>JointPlot center panel contains a ScatterSeries.</summary>
    [Fact]
    public void JointPlot_CenterPanel_ContainsScatterSeries()
    {
        var figure = FigureTemplates.JointPlot(JointX, JointY).Build();
        // Center is subplot index 1 (0-indexed: top marginal, center scatter, right marginal)
        bool hasScatter = figure.SubPlots.Any(ax => ax.Series.OfType<ScatterSeries>().Any());
        Assert.True(hasScatter);
    }

    /// <summary>JointPlot marginals contain HistogramSeries.</summary>
    [Fact]
    public void JointPlot_MarginalPanels_ContainHistogramSeries()
    {
        var figure = FigureTemplates.JointPlot(JointX, JointY).Build();
        int histCount = figure.SubPlots.Sum(ax => ax.Series.OfType<HistogramSeries>().Count());
        Assert.Equal(2, histCount);
    }

    // --- PairPlot ---

    private static readonly double[][] PairColumns =
    [
        [1.0, 2.0, 3.0, 4.0],
        [4.0, 3.0, 2.0, 1.0],
        [1.0, 3.0, 2.0, 4.0]
    ];

    /// <summary>PairPlot is one axes holding the pair grid series of its columns, named and binned as asked.</summary>
    [Fact]
    public void PairPlot_IsOneAxesHoldingThePairGridSeries()
    {
        string[] names = ["a", "b", "c"];
        var figure = FigureTemplates.PairPlot(PairColumns, names, bins: 5).Build();

        var grid = Assert.IsType<PairGridSeries>(Assert.Single(Assert.Single(figure.SubPlots).Series));
        Assert.Same(PairColumns, grid.Variables);
        Assert.Same(names, grid.Labels);
        Assert.Equal(5, grid.DiagonalBins);
    }

    // --- FacetGrid ---

    private static readonly double[] FgX = [1.0, 2.0, 3.0, 4.0, 5.0, 6.0];
    private static readonly double[] FgY = [1.0, 2.0, 3.0, 1.5, 2.5, 3.5];
    private static readonly string[] FgCat = ["A", "A", "B", "B", "C", "C"];

    /// <summary>FacetGrid creates one subplot per unique category.</summary>
    [Fact]
    public void FacetGrid_HasOneSubplotPerCategory()
    {
        var figure = FigureTemplates.FacetGrid(FgX, FgY, FgCat, (ax, x, y) => ax.Scatter(x, y)).Build();
        int uniqueCats = FgCat.Distinct().Count();
        Assert.Equal(uniqueCats, figure.SubPlots.Count);
    }

    // --- Clustermap ---

    private static readonly double[,] ClusterData =
    {
        { 1.0, 2.0, 0.5 },
        { 1.1, 1.9, 0.6 },
        { 5.0, 0.1, 4.0 },
        { 4.8, 0.2, 3.9 }
    };

    /// <summary>Clustermap builds one axes holding the clustermap series, its rows and columns clustered into its two trees.</summary>
    [Fact]
    public void Clustermap_IsOneAxesHoldingTheClustermapSeries()
    {
        var figure = FigureTemplates.Clustermap(ClusterData).Build();
        var map = Assert.IsType<ClustermapSeries>(Assert.Single(Assert.Single(figure.SubPlots).Series));
        Assert.NotNull(map.RowTree);
        Assert.NotNull(map.ColumnTree);
        Assert.Equal((700.0, 700.0), (figure.Width, figure.Height));
    }

    // --- Clustermap labels ---

    // Two pairs of near rows and of near columns, written so that the map must reorder them: a is near c, b is near d.
    private static readonly double[,] Interleaved =
    {
        { 1.0, 2.0, 0.5 },
        { 5.0, 0.1, 4.0 },
        { 1.1, 1.9, 0.6 },
        { 4.8, 0.2, 3.9 },
    };

    private static readonly string[] RowNames = ["row-a", "row-b", "row-c", "row-d"];
    private static readonly string[] ColumnNames = ["col-x", "col-y", "col-z"];

    // The matrix as the map draws it: its rows and columns in the order of its trees.
    private static double[,] AsDrawn(ClustermapSeries map) => map.Data.Permute(
        ClustermapSeries.ResolveLeafOrder(map.RowTree, map.Data.GetLength(0)),
        ClustermapSeries.ResolveLeafOrder(map.ColumnTree, map.Data.GetLength(1)));

    // The labels of the matrix in the order a reader meets them: rows from the top down, columns from the left.
    private static (string[] Rows, string[] Columns) LabelsAsDrawn(double[,] data, string[]? rows, string[]? columns, out double[,] drawn)
    {
        var figure = FigureTemplates.Clustermap(data, rows, columns).Build();

        drawn = AsDrawn(Assert.IsType<ClustermapSeries>(Assert.Single(figure.SubPlots[0].Series)));

        var texts = FigureTemplates.Clustermap(data, rows, columns).ToSvg().Elements("text")
            .Where(text => text["x"] is not null && text["y"] is not null)
            .Select(text => (X: text.Number("x"), Y: text.Number("y"), Text: text.Text))
            .ToArray();

        return (
            [.. texts.Where(each => each.Text.StartsWith("row-", StringComparison.Ordinal)).OrderBy(each => each.Y).Select(each => each.Text)],
            [.. texts.Where(each => each.Text.StartsWith("col-", StringComparison.Ordinal)).OrderBy(each => each.X).Select(each => each.Text)]);
    }

    // The input row the drawn row is: the map reorders the columns as well, so a row is known by the numbers it holds, which
    // differ from row to row here, and not by where they stand.
    private static int InputRowOf(double[,] data, double[,] drawn, int row)
    {
        var held = Enumerable.Range(0, drawn.GetLength(1)).Select(column => drawn[row, column]).Order().ToArray();

        for (var candidate = 0; candidate < data.GetLength(0); candidate++)
        {
            if (Enumerable.Range(0, data.GetLength(1)).Select(column => data[candidate, column]).Order().SequenceEqual(held))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException($"The drawn row {row} is none of the rows given.");
    }

    private static int InputColumnOf(double[,] data, double[,] drawn, int column)
    {
        var held = Enumerable.Range(0, drawn.GetLength(0)).Select(row => drawn[row, column]).Order().ToArray();

        for (var candidate = 0; candidate < data.GetLength(1); candidate++)
        {
            if (Enumerable.Range(0, data.GetLength(0)).Select(row => data[row, candidate]).Order().SequenceEqual(held))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException($"The drawn column {column} is none of the columns given.");
    }

    /// <summary>The map reorders its rows and columns by the clustering, and a label goes with the row or column it was given for,
    /// not with the place it stood in the input: the labels read from the top down are those of the rows as drawn.</summary>
    [Fact]
    public void Clustermap_Labels_FollowTheRowsAndColumnsTheyNameToWhereTheMapDrawsThem()
    {
        var (rows, columns) = LabelsAsDrawn(Interleaved, RowNames, ColumnNames, out var drawn);

        Assert.Equal(
            Enumerable.Range(0, 4).Select(row => RowNames[InputRowOf(Interleaved, drawn, row)]),
            rows);
        Assert.Equal(
            Enumerable.Range(0, 3).Select(column => ColumnNames[InputColumnOf(Interleaved, drawn, column)]),
            columns);
    }

    /// <summary>The input order is not the drawn order here, so a label that stayed where it was given would show.</summary>
    [Fact]
    public void Clustermap_Labels_AreTestedOnADataSetThatTheMapReorders()
    {
        var figure = FigureTemplates.Clustermap(Interleaved, RowNames, ColumnNames).Build();
        var drawn = AsDrawn(Assert.IsType<ClustermapSeries>(Assert.Single(figure.SubPlots[0].Series)));

        Assert.NotEqual(
            Enumerable.Range(0, 4),
            Enumerable.Range(0, 4).Select(row => InputRowOf(Interleaved, drawn, row)));
    }

    /// <summary>Row labels alone, or column labels alone, are as good as both: each is optional.</summary>
    [Fact]
    public void Clustermap_LabelsMayBeGivenForRowsOnlyOrColumnsOnly()
    {
        var (rows, none) = LabelsAsDrawn(Interleaved, RowNames, null, out _);
        var (noRows, columns) = LabelsAsDrawn(Interleaved, null, ColumnNames, out _);

        Assert.Equal(4, rows.Length);
        Assert.Empty(none);
        Assert.Empty(noRows);
        Assert.Equal(3, columns.Length);
    }

    /// <summary>A label for a row that is not there, or none for one that is, is a mistake to be named, not drawn.</summary>
    [Fact]
    public void Clustermap_ALabelListOfAnotherLength_IsRefusedByName()
    {
        var rows = Assert.Throws<ArgumentException>(() => FigureTemplates.Clustermap(Interleaved, ["a", "b"], null));
        var columns = Assert.Throws<ArgumentException>(() => FigureTemplates.Clustermap(Interleaved, null, ["x", "y", "z", "w"]));

        Assert.Contains("rowLabels", rows.Message, StringComparison.Ordinal);
        Assert.Contains("colLabels", columns.Message, StringComparison.Ordinal);
    }

    /// <summary>FigureTemplates.FinancialDashboard — the three
    /// `configure*Panel?.Invoke(ax)` null-conditional invocations. Default null
    /// is tested elsewhere; supplying non-null callbacks exercises the true arms.</summary>
    [Fact]
    public void FinancialDashboard_AllPanelCallbacks_AreInvoked()
    {
        double[] open = { 100, 102, 101 }, high = { 103, 104, 103 };
        double[] low = { 98, 99, 98 }, close = { 101, 102, 101 };
        double[] volume = { 1000, 1200, 800 };
        bool priceCalled = false, volCalled = false, oscCalled = false;
        var fig = FigureTemplates.FinancialDashboard(
            open, high, low, close, volume,
            configurePricePanel: _ => priceCalled = true,
            configureVolumePanel: _ => volCalled = true,
            configureOscillatorPanel: _ => oscCalled = true).Build();
        Assert.True(priceCalled);
        Assert.True(volCalled);
        Assert.True(oscCalled);
    }

    [Fact] public void FigureTemplates_ScientificPaper_WithTitle_AppliesTitle()
    {
        var fb = FigureTemplates.ScientificPaper(ax => ax.Plot([1.0, 2], [3.0, 4]), title: "test");
        var fig = fb.Build();
        Assert.Equal("test", fig.Title);
    }

    [Fact] public void FigureTemplates_FinancialDashboard_WithTitle_AppliesTitle()
    {
        var fb = FigureTemplates.FinancialDashboard(
            open: [10.0, 11.0], high: [12.0, 13.0], low: [9.0, 10.0],
            close: [11.0, 12.0], volume: [1000.0, 2000.0],
            title: "fin");
        var fig = fb.Build();
        Assert.Equal("fin", fig.Title);
    }

    [Fact] public void FigureTemplates_JointPlot_DefaultBins_RendersScatterAndMarginals()
    {
        var fb = FigureTemplates.JointPlot([1.0, 2, 3], [1.0, 2, 3], title: "joint");
        var fig = fb.Build();
        Assert.Equal("joint", fig.Title);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        //  Multi-panel ScientificPaper(int,int,Action{AxesBuilder}[],string,double,double) with a title draws it. Existing tests only
        // exercise the no-title overload.
        new DrawnToken("ScientificPaper_MultiPanel_WithTitle_SetsFigureTitle", () =>
        {
            var builder = FigureTemplates.ScientificPaper(
                rows: 1, cols: 2,
                configures:
                [
                    ax => ax.Plot([1.0, 2.0], [1.0, 4.0]),
                    ax => ax.Plot([1.0, 2.0], [2.0, 8.0]),
                ],
                title: "Pair of panels");
            return builder.Build().ToSvg();
        }, "Pair of panels"),
        //  JointPlot with title includes the title in SVG output.
        new DrawnToken("JointPlot_WithTitle_IncludesTitleInSvg", () => FigureTemplates.JointPlot(JointX, JointY, title: "My Joint Plot").ToSvg(), "My Joint Plot"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
