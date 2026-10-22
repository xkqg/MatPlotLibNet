// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.FluentApi;

/// <summary>Verifies <see cref="FigureTemplates"/> behavior.</summary>
public class FigureTemplateTests
{
    private static readonly double[] Open  = [100, 102, 101, 103, 105, 104, 106, 107, 106, 108];
    private static readonly double[] High  = [103, 104, 103, 105, 107, 106, 108, 109, 108, 110];
    private static readonly double[] Low   = [ 98,  99,  98, 100, 102, 101, 103, 104, 103, 105];
    private static readonly double[] Close = [101, 102, 101, 103, 105, 104, 106, 107, 106, 108];
    private static readonly double[] Volume = [1000, 1200, 800, 1500, 2000, 900, 1800, 2200, 700, 1600];

    // --- FinancialDashboard ---

    /// <summary>FinancialDashboard returns a FigureBuilder (not null).</summary>
    [Fact]
    public void FinancialDashboard_ReturnsFigureBuilder()
    {
        var builder = FigureTemplates.FinancialDashboard(Open, High, Low, Close, Volume);
        Assert.NotNull(builder);
    }

    /// <summary>FinancialDashboard produces a figure with exactly three subplots.</summary>
    [Fact]
    public void FinancialDashboard_HasThreeSubplots()
    {
        var figure = FigureTemplates.FinancialDashboard(Open, High, Low, Close, Volume).Build();
        Assert.Equal(3, figure.SubPlots.Count);
    }

    /// <summary>The first subplot (price panel) contains a CandlestickSeries.</summary>
    [Fact]
    public void FinancialDashboard_PricePanelHasCandlestick()
    {
        var figure = FigureTemplates.FinancialDashboard(Open, High, Low, Close, Volume).Build();
        Assert.Contains(figure.SubPlots[0].Series, s => s is CandlestickSeries);
    }

    /// <summary>The second subplot (volume panel) contains a BarSeries.</summary>
    [Fact]
    public void FinancialDashboard_VolumePanelHasBarSeries()
    {
        var figure = FigureTemplates.FinancialDashboard(Open, High, Low, Close, Volume).Build();
        Assert.Contains(figure.SubPlots[1].Series, s => s is BarSeries);
    }

    /// <summary>The title is applied to the figure when provided.</summary>
    [Fact]
    public void FinancialDashboard_SetsTitle()
    {
        var figure = FigureTemplates.FinancialDashboard(Open, High, Low, Close, Volume, title: "AAPL").Build();
        Assert.Equal("AAPL", figure.Title);
    }

    // --- ScientificPaper ---

    /// <summary>ScientificPaper returns a FigureBuilder.</summary>
    [Fact]
    public void ScientificPaper_ReturnsFigureBuilder()
    {
        var builder = FigureTemplates.ScientificPaper(ax => ax.Plot([1.0, 2, 3], [4.0, 5, 6]));
        Assert.NotNull(builder);
    }

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

    /// <summary>SparklineDashboard returns a FigureBuilder.</summary>
    [Fact]
    public void SparklineDashboard_ReturnsFigureBuilder()
    {
        var builder = FigureTemplates.SparklineDashboard(
            [("A", [1.0, 2, 3]), ("B", [4.0, 5, 6])]);
        Assert.NotNull(builder);
    }

    /// <summary>SparklineDashboard produces one subplot per series.</summary>
    [Fact]
    public void SparklineDashboard_HasOneAxisPerSeries()
    {
        var series = new (string Label, double[] Values)[]
        {
            ("Revenue", [10, 20, 30]),
            ("Costs",   [8, 15, 25]),
            ("Profit",  [2, 5, 5])
        };
        var figure = FigureTemplates.SparklineDashboard(series).Build();
        Assert.Equal(3, figure.SubPlots.Count);
    }

    private static readonly NamedSample[] Metrics =
    [
        new("Revenue", [10, 20, 30]),
        new("Costs",   [8, 15, 25]),
        new("Profit",  [2, 5, 5]),
    ];

    // The same three rows as the named samples, written the way the array of tuples has always been written.
    private static (string Label, double[] Values)[] MetricsAsTuples() =>
        [.. Metrics.Select(sample => (sample.Label, sample.Values))];

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

    /// <summary>The named samples and the array of tuples are two ways to hand over the same rows: the picture is the same
    /// bytes, with a title and without one.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("Server Metrics")]
    public void SparklineDashboard_NamedSamplesAndTuples_DrawTheSameSvg(string? title)
    {
        string named = FigureTemplates.SparklineDashboard(Metrics, title).ToSvg();
        string tuples = FigureTemplates.SparklineDashboard(MetricsAsTuples(), title).ToSvg();

        Assert.Equal(tuples, named);
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

    /// <summary>No rows at all is refused by name in both ways of handing them over, and with the same words.</summary>
    [Fact]
    public void SparklineDashboard_NoRowsAtAll_IsRefusedByNameInBothOverloads()
    {
        var named = Assert.Throws<ArgumentNullException>(() => FigureTemplates.SparklineDashboard((IReadOnlyList<NamedSample>)null!));
        var tuples = Assert.Throws<ArgumentNullException>(() => FigureTemplates.SparklineDashboard(((string, double[])[])null!));

        Assert.Equal("series", named.ParamName);
        Assert.Equal(named.ParamName, tuples.ParamName);
        Assert.Equal(named.Message, tuples.Message);
    }

    /// <summary>A dashboard of no rows is a picture 0 pixels tall: it is refused by name, in both ways of handing the rows
    /// over and in the same words, before a figure is made.</summary>
    [Fact]
    public void SparklineDashboard_AnEmptyList_IsRefusedByNameInBothOverloads()
    {
        var named = Assert.Throws<ArgumentException>(() => FigureTemplates.SparklineDashboard(Array.Empty<NamedSample>()));
        var tuples = Assert.Throws<ArgumentException>(() => FigureTemplates.SparklineDashboard(Array.Empty<(string Label, double[] Values)>()));

        Assert.Equal("series", named.ParamName);
        Assert.Equal(named.ParamName, tuples.ParamName);
        Assert.Equal(named.Message, tuples.Message);
        Assert.Contains("No rows", named.Message, StringComparison.Ordinal);
    }

    /// <summary>A row that carries no values (the default of a named sample has none) is refused by the name of the row, in both
    /// ways of handing the rows over and in the same words, instead of failing deep in the sparkline.</summary>
    [Fact]
    public void SparklineDashboard_ARowWithoutValues_IsRefusedByItsNameInBothOverloads()
    {
        var named = Assert.Throws<ArgumentException>(() => FigureTemplates.SparklineDashboard([Metrics[0], new NamedSample("Latency", null!)]));
        var tuples = Assert.Throws<ArgumentException>(() => FigureTemplates.SparklineDashboard([("Revenue", Metrics[0].Values), ("Latency", (double[])null!)]));

        Assert.Equal("series", named.ParamName);
        Assert.Equal(named.ParamName, tuples.ParamName);
        Assert.Equal(named.Message, tuples.Message);
        Assert.Contains("'Latency'", named.Message, StringComparison.Ordinal);
    }

    // --- JointPlot ---

    private static readonly double[] JointX = [1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0];
    private static readonly double[] JointY = [1.2, 1.9, 3.1, 4.0, 5.1, 5.8, 7.2, 8.0];

    /// <summary>JointPlot returns a non-null FigureBuilder.</summary>
    [Fact]
    public void JointPlot_ReturnsFigureBuilder()
    {
        var builder = FigureTemplates.JointPlot(JointX, JointY);
        Assert.NotNull(builder);
    }

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

    /// <summary>JointPlot renders to valid SVG.</summary>
    [Fact]
    public void JointPlot_RendersToValidSvg()
    {
        string svg = FigureTemplates.JointPlot(JointX, JointY).ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>JointPlot with title includes the title in SVG output.</summary>
    [Fact]
    public void JointPlot_WithTitle_IncludesTitleInSvg()
    {
        string svg = FigureTemplates.JointPlot(JointX, JointY, title: "My Joint Plot").ToSvg();
        Assert.Contains("My Joint Plot", svg);
    }

    // --- PairPlot ---

    private static readonly double[][] PairColumns =
    [
        [1.0, 2.0, 3.0, 4.0],
        [4.0, 3.0, 2.0, 1.0],
        [1.0, 3.0, 2.0, 4.0]
    ];

    /// <summary>PairPlot returns a non-null FigureBuilder.</summary>
    [Fact]
    public void PairPlot_ReturnsFigureBuilder()
        => Assert.NotNull(FigureTemplates.PairPlot(PairColumns));

    /// <summary>PairPlot creates N×N subplots.</summary>
    [Fact]
    public void PairPlot_HasNxNSubplots()
    {
        int n = PairColumns.Length;
        var figure = FigureTemplates.PairPlot(PairColumns).Build();
        Assert.Equal(n * n, figure.SubPlots.Count);
    }

    /// <summary>PairPlot diagonal panels contain HistogramSeries.</summary>
    [Fact]
    public void PairPlot_DiagonalHasHistograms()
    {
        var figure = FigureTemplates.PairPlot(PairColumns).Build();
        bool hasHist = figure.SubPlots.Any(ax => ax.Series.OfType<HistogramSeries>().Any());
        Assert.True(hasHist);
    }

    /// <summary>PairPlot renders to valid SVG.</summary>
    [Fact]
    public void PairPlot_RendersToValidSvg()
    {
        string svg = FigureTemplates.PairPlot(PairColumns).ToSvg();
        Assert.Contains("<svg", svg);
    }

    // --- FacetGrid ---

    private static readonly double[] FgX = [1.0, 2.0, 3.0, 4.0, 5.0, 6.0];
    private static readonly double[] FgY = [1.0, 2.0, 3.0, 1.5, 2.5, 3.5];
    private static readonly string[] FgCat = ["A", "A", "B", "B", "C", "C"];

    /// <summary>FacetGrid returns a non-null FigureBuilder.</summary>
    [Fact]
    public void FacetGrid_ReturnsFigureBuilder()
        => Assert.NotNull(FigureTemplates.FacetGrid(FgX, FgY, FgCat, (ax, x, y) => ax.Scatter(x, y)));

    /// <summary>FacetGrid creates one subplot per unique category.</summary>
    [Fact]
    public void FacetGrid_HasOneSubplotPerCategory()
    {
        var figure = FigureTemplates.FacetGrid(FgX, FgY, FgCat, (ax, x, y) => ax.Scatter(x, y)).Build();
        int uniqueCats = FgCat.Distinct().Count();
        Assert.Equal(uniqueCats, figure.SubPlots.Count);
    }

    /// <summary>FacetGrid renders to valid SVG.</summary>
    [Fact]
    public void FacetGrid_RendersToValidSvg()
    {
        string svg = FigureTemplates.FacetGrid(FgX, FgY, FgCat, (ax, x, y) => ax.Scatter(x, y)).ToSvg();
        Assert.Contains("<svg", svg);
    }

    // --- Clustermap ---

    private static readonly double[,] ClusterData =
    {
        { 1.0, 2.0, 0.5 },
        { 1.1, 1.9, 0.6 },
        { 5.0, 0.1, 4.0 },
        { 4.8, 0.2, 3.9 }
    };

    /// <summary>Clustermap returns a non-null FigureBuilder.</summary>
    [Fact]
    public void Clustermap_ReturnsFigureBuilder()
        => Assert.NotNull(FigureTemplates.Clustermap(ClusterData));

    /// <summary>Clustermap builds a figure with exactly 4 subplots (2×2 grid).</summary>
    [Fact]
    public void Clustermap_HasFourSubplots()
    {
        var figure = FigureTemplates.Clustermap(ClusterData).Build();
        Assert.Equal(4, figure.SubPlots.Count);
    }

    /// <summary>Clustermap renders to valid SVG.</summary>
    [Fact]
    public void Clustermap_RendersToValidSvg()
    {
        string svg = FigureTemplates.Clustermap(ClusterData).ToSvg();
        Assert.Contains("<svg", svg);
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

    // The labels of the heatmap panel in the order a reader meets them: rows from the top down, columns from the left.
    private static (string[] Rows, string[] Columns) LabelsAsDrawn(double[,] data, string[]? rows, string[]? columns, out HeatmapSeries drawn)
    {
        var figure = FigureTemplates.Clustermap(data, rows, columns).Build();

        drawn = Assert.Single(figure.SubPlots[3].Series.OfType<HeatmapSeries>());

        var texts = System.Text.RegularExpressions.Regex
            .Matches(FigureTemplates.Clustermap(data, rows, columns).ToSvg(), "<text[^>]*?x=\"([-0-9.]+)\"[^>]*?y=\"([-0-9.]+)\"[^>]*>([^<]*)</text>")
            .Select(match => (X: double.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), Y: double.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture), Text: match.Groups[3].Value))
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
            Enumerable.Range(0, 4).Select(row => RowNames[InputRowOf(Interleaved, drawn.Data, row)]),
            rows);
        Assert.Equal(
            Enumerable.Range(0, 3).Select(column => ColumnNames[InputColumnOf(Interleaved, drawn.Data, column)]),
            columns);
    }

    /// <summary>The input order is not the drawn order here, so a label that stayed where it was given would show.</summary>
    [Fact]
    public void Clustermap_Labels_AreTestedOnADataSetThatTheMapReorders()
    {
        var figure = FigureTemplates.Clustermap(Interleaved, RowNames, ColumnNames).Build();
        var drawn = Assert.Single(figure.SubPlots[3].Series.OfType<HeatmapSeries>()).Data;

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
}
