// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.Data.Analysis;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.DataFrame;

/// <summary>Verifies <see cref="DataFrameFigureExtensions"/> Line, Scatter, Hist, and Clustermap methods.</summary>
public class DataFrameFigureExtensionsTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Microsoft.Data.Analysis.DataFrame MakeXYDf(
        int[] xs, double[] ys, string[]? hues = null)
    {
        var cols = new List<DataFrameColumn>
        {
            new PrimitiveDataFrameColumn<int>("x", xs),
            new PrimitiveDataFrameColumn<double>("y", ys)
        };
        if (hues is not null)
            cols.Add(new StringDataFrameColumn("group", hues));
        return new Microsoft.Data.Analysis.DataFrame(cols);
    }

    // ── Line — no hue ─────────────────────────────────────────────────────────

    [Fact]
    public void Line_NoHue_ProducesOneLineSeries()
    {
        var df = MakeXYDf([1, 2, 3, 4], [10.0, 20.0, 30.0, 40.0]);
        var fig = df.Line("x", "y").Build();
        Assert.Single(fig.SubPlots[0].Series);
        Assert.IsType<LineSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact]
    public void Line_NoHue_PreservesRowCount()
    {
        var df = MakeXYDf([1, 2, 3, 4], [10.0, 20.0, 30.0, 40.0]);
        var fig = df.Line("x", "y").Build();
        var s = (LineSeries)fig.SubPlots[0].Series[0];
        Assert.Equal(4, s.XData.Length);
    }

    [Fact]
    public void Line_WithHue_OneSeriesPerGroup()
    {
        var df = MakeXYDf([1, 2, 3, 4], [1.0, 2.0, 3.0, 4.0], ["A", "A", "B", "B"]);
        var fig = df.Line("x", "y", hue: "group").Build();
        Assert.Equal(2, fig.SubPlots[0].Series.Count);
    }

    [Fact]
    public void Line_WithHue_LabelsMatchGroupKeys()
    {
        var df = MakeXYDf([1, 2, 3, 4], [1.0, 2.0, 3.0, 4.0], ["A", "A", "B", "B"]);
        var fig = df.Line("x", "y", hue: "group").Build();
        var labels = fig.SubPlots[0].Series.Select(s => s.Label).ToHashSet();
        Assert.Contains("A", labels);
        Assert.Contains("B", labels);
    }

    [Fact]
    public void Line_WithHue_CustomPalette_FirstGroupUsesFirstColor()
    {
        var red = new Color(255, 0, 0);
        var blue = new Color(0, 0, 255);
        var df = MakeXYDf([1, 2, 3, 4], [1.0, 2.0, 3.0, 4.0], ["A", "A", "B", "B"]);
        var fig = df.Line("x", "y", hue: "group", palette: [red, blue]).Build();
        var firstSeries = (LineSeries)fig.SubPlots[0].Series[0];
        Assert.Equal(red, firstSeries.Color);
    }

    [Fact]
    public void Line_UnknownColumn_ThrowsArgumentException()
    {
        var df = MakeXYDf([1, 2], [1.0, 2.0]);
        var ex = Assert.Throws<ArgumentException>(() => df.Line("x", "missing"));
        Assert.Contains("missing", ex.Message);
    }

    [Fact]
    public void Line_ReturnsChainableFigureBuilder()
    {
        var df = MakeXYDf([1, 2, 3], [1.0, 2.0, 3.0]);
        var fig = df.Line("x", "y").WithTitle("My Chart").Build();
        Assert.Equal("My Chart", fig.Title);
    }

    // ── Scatter ───────────────────────────────────────────────────────────────

    [Fact]
    public void Scatter_NoHue_ProducesOneScatterSeries()
    {
        var df = MakeXYDf([1, 2, 3], [1.0, 2.0, 3.0]);
        var fig = df.Scatter("x", "y").Build();
        Assert.Single(fig.SubPlots[0].Series);
        Assert.IsType<ScatterSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact]
    public void Scatter_WithHue_OneSeriesPerGroup()
    {
        var df = MakeXYDf([1, 2, 3, 4], [1.0, 2.0, 3.0, 4.0], ["A", "A", "B", "B"]);
        var fig = df.Scatter("x", "y", hue: "group").Build();
        Assert.Equal(2, fig.SubPlots[0].Series.Count);
    }

    // ── Hist ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Hist_NoHue_ProducesOneHistogramSeries()
    {
        var df = new Microsoft.Data.Analysis.DataFrame(
            new PrimitiveDataFrameColumn<double>("val", [1.0, 2.0, 3.0, 4.0, 5.0]));
        var fig = df.Hist("val").Build();
        Assert.Single(fig.SubPlots[0].Series);
        Assert.IsType<HistogramSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact]
    public void Hist_DefaultBins_Is30()
    {
        var df = new Microsoft.Data.Analysis.DataFrame(
            new PrimitiveDataFrameColumn<double>("val", Enumerable.Range(0, 50).Select(i => (double)i).ToArray()));
        var fig = df.Hist("val").Build();
        var s = (HistogramSeries)fig.SubPlots[0].Series[0];
        Assert.Equal(30, s.Bins);
    }

    [Fact]
    public void Hist_ExplicitBins_Used()
    {
        var df = new Microsoft.Data.Analysis.DataFrame(
            new PrimitiveDataFrameColumn<double>("val", Enumerable.Range(0, 50).Select(i => (double)i).ToArray()));
        var fig = df.Hist("val", bins: 10).Build();
        var s = (HistogramSeries)fig.SubPlots[0].Series[0];
        Assert.Equal(10, s.Bins);
    }

    [Fact]
    public void Hist_WithHue_OneSeriesPerGroup()
    {
        var df = new Microsoft.Data.Analysis.DataFrame(
            new PrimitiveDataFrameColumn<double>("val", [1.0, 2.0, 3.0, 4.0]),
            new StringDataFrameColumn("grp", ["A", "A", "B", "B"]));
        var fig = df.Hist("val", hue: "grp").Build();
        Assert.Equal(2, fig.SubPlots[0].Series.Count);
    }

    [Fact]
    public void Hist_WithHue_OverlappingAlpha()
    {
        var df = new Microsoft.Data.Analysis.DataFrame(
            new PrimitiveDataFrameColumn<double>("val", [1.0, 2.0, 3.0, 4.0]),
            new StringDataFrameColumn("grp", ["A", "A", "B", "B"]));
        var fig = df.Hist("val", hue: "grp").Build();
        foreach (var s in fig.SubPlots[0].Series.Cast<HistogramSeries>())
            Assert.Equal(0.7, s.Alpha, 5);
    }

    // ── Clustermap ────────────────────────────────────────────────────────────

    private static Microsoft.Data.Analysis.DataFrame MakeMatrixDf()
    {
        // 3-row × 2-column frame; columns named "A" and "B"
        return new Microsoft.Data.Analysis.DataFrame(
            new PrimitiveDataFrameColumn<double>("A", [1.0, 3.0, 5.0]),
            new PrimitiveDataFrameColumn<double>("B", [2.0, 4.0, 6.0]));
    }

    [Fact]
    public void Clustermap_ProducesOneClustermapSeries()
    {
        var fig = MakeMatrixDf().Clustermap(["A", "B"]).Build();
        Assert.Single(fig.SubPlots[0].Series);
        Assert.IsType<MatPlotLibNet.Models.Series.ClustermapSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact]
    public void Clustermap_DataDimensions_MatchRowsAndColumns()
    {
        var fig = MakeMatrixDf().Clustermap(["A", "B"]).Build();
        var s = (MatPlotLibNet.Models.Series.ClustermapSeries)fig.SubPlots[0].Series[0];
        Assert.Equal(3, s.Data.GetLength(0)); // 3 rows
        Assert.Equal(2, s.Data.GetLength(1)); // 2 columns
    }

    [Fact]
    public void Clustermap_CellValues_MatchSourceColumns()
    {
        var fig = MakeMatrixDf().Clustermap(["A", "B"]).Build();
        var s = (MatPlotLibNet.Models.Series.ClustermapSeries)fig.SubPlots[0].Series[0];
        Assert.Equal(1.0, s.Data[0, 0]); // row 0, col A
        Assert.Equal(2.0, s.Data[0, 1]); // row 0, col B
        Assert.Equal(5.0, s.Data[2, 0]); // row 2, col A
    }

    [Fact]
    public void Clustermap_Configure_AppliesOptions()
    {
        var fig = MakeMatrixDf()
            .Clustermap(["A", "B"], s => s.ShowLabels = true)
            .Build();
        var s = (MatPlotLibNet.Models.Series.ClustermapSeries)fig.SubPlots[0].Series[0];
        Assert.True(s.ShowLabels);
    }

    [Fact]
    public void Clustermap_UnknownColumn_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => MakeMatrixDf().Clustermap(["A", "missing"]).Build());
        Assert.Contains("missing", ex.Message);
    }

    [Fact]
    public void Clustermap_EmptyColumns_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => MakeMatrixDf().Clustermap([]).Build());
    }

    [Fact]
    public void Clustermap_ReturnsChainableFigureBuilder()
    {
        var fig = MakeMatrixDf().Clustermap(["A", "B"]).WithTitle("Heat").Build();
        Assert.Equal("Heat", fig.Title);
    }

    // ── PairGrid ──────────────────────────────────────────────────────────────

    private static Microsoft.Data.Analysis.DataFrame MakePairDf(string[]? hues = null)
    {
        var cols = new List<DataFrameColumn>
        {
            new PrimitiveDataFrameColumn<double>("petal_l", [1.0, 2.0, 3.0, 4.0]),
            new PrimitiveDataFrameColumn<double>("petal_w", [0.5, 1.5, 2.5, 3.5]),
            new PrimitiveDataFrameColumn<double>("sepal_l", [9.0, 8.0, 7.0, 6.0]),
        };
        if (hues is not null)
            cols.Add(new StringDataFrameColumn("species", hues));
        return new Microsoft.Data.Analysis.DataFrame(cols);
    }

    [Fact]
    public void PairGrid_ProducesOnePairGridSeries()
    {
        var fig = MakePairDf().PairGrid(["petal_l", "petal_w", "sepal_l"]).Build();
        Assert.Single(fig.SubPlots[0].Series);
        Assert.IsType<MatPlotLibNet.Models.Series.PairGridSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact]
    public void PairGrid_VariablesShape_MatchesColumnsAndRows()
    {
        var fig = MakePairDf().PairGrid(["petal_l", "petal_w", "sepal_l"]).Build();
        var s = (MatPlotLibNet.Models.Series.PairGridSeries)fig.SubPlots[0].Series[0];
        Assert.Equal(3, s.Variables.Length);
        Assert.Equal(4, s.Variables[0].Length);
    }

    [Fact]
    public void PairGrid_LabelsMatchColumnNames()
    {
        var fig = MakePairDf().PairGrid(["petal_l", "petal_w", "sepal_l"]).Build();
        var s = (MatPlotLibNet.Models.Series.PairGridSeries)fig.SubPlots[0].Series[0];
        Assert.NotNull(s.Labels);
        Assert.Equal(["petal_l", "petal_w", "sepal_l"], s.Labels);
    }

    [Fact]
    public void PairGrid_WithHue_PopulatesHueGroupsAndLabels()
    {
        var df = MakePairDf(["A", "B", "A", "B"]);
        var fig = df.PairGrid(["petal_l", "petal_w"], hue: "species").Build();
        var s = (MatPlotLibNet.Models.Series.PairGridSeries)fig.SubPlots[0].Series[0];
        Assert.NotNull(s.HueGroups);
        Assert.NotNull(s.HueLabels);
        Assert.Equal(4, s.HueGroups.Length);
        Assert.Equal(2, s.HueLabels.Length);
        Assert.Contains("A", s.HueLabels);
        Assert.Contains("B", s.HueLabels);
    }

    [Fact]
    public void PairGrid_UnknownColumn_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => MakePairDf().PairGrid(["petal_l", "missing"]).Build());
        Assert.Contains("missing", ex.Message);
    }

    [Fact]
    public void PairGrid_EmptyColumns_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => MakePairDf().PairGrid([]).Build());
    }

    [Fact]
    public void PairGrid_ReturnsChainableFigureBuilder()
    {
        var fig = MakePairDf().PairGrid(["petal_l", "petal_w"]).WithTitle("Pairs").Build();
        Assert.Equal("Pairs", fig.Title);
    }

    [Fact]
    public void PairGrid_HueSingleDistinctValue_ProducesOneGroup()
    {
        // All 4 rows have the same species → 1-element HueLabels, 4 zeros in HueGroups.
        var df = MakePairDf(["A", "A", "A", "A"]);
        var fig = df.PairGrid(["petal_l", "petal_w"], hue: "species").Build();
        var s = (MatPlotLibNet.Models.Series.PairGridSeries)fig.SubPlots[0].Series[0];
        Assert.NotNull(s.HueGroups);
        Assert.NotNull(s.HueLabels);
        Assert.Equal(4, s.HueGroups.Length);
        Assert.All(s.HueGroups, g => Assert.Equal(0, g));
        Assert.Single(s.HueLabels);
        Assert.Equal("A", s.HueLabels[0]);
    }

    [Fact]
    public void PairGrid_WithPaletteAndConfigure_BothAreApplied()
    {
        // Exercises the (palette is not null) and configure?.Invoke(s) branches in
        // the inner lambda — the two halves that the no-arg call-sites short-circuit.
        var palette = new[] { Colors.Red, Colors.Blue };
        bool invoked = false;
        var fig = MakePairDf().PairGrid(["petal_l", "petal_w"],
            palette: palette,
            configure: _ => invoked = true).Build();
        var s = (MatPlotLibNet.Models.Series.PairGridSeries)fig.SubPlots[0].Series[0];
        Assert.True(invoked);
        Assert.Equal(palette, s.HuePalette);
    }

    // ── NetworkGraph ──────────────────────────────────────────────────────────

    private static Microsoft.Data.Analysis.DataFrame MakeEdgeListDf(
        string[]? from = null,
        string[]? to   = null,
        double[]? weight = null,
        bool[]?   directed = null)
    {
        from ??= ["a", "a", "b"];
        to   ??= ["b", "c", "c"];
        var cols = new List<DataFrameColumn>
        {
            new StringDataFrameColumn("source", from),
            new StringDataFrameColumn("target", to),
        };
        if (weight is not null)
            cols.Add(new PrimitiveDataFrameColumn<double>("weight", weight));
        if (directed is not null)
            cols.Add(new PrimitiveDataFrameColumn<bool>("directed", directed));
        return new Microsoft.Data.Analysis.DataFrame(cols);
    }

    [Fact]
    public void NetworkGraph_ProducesOneNetworkGraphSeries()
    {
        var fig = MakeEdgeListDf().NetworkGraph("source", "target").Build();
        Assert.Single(fig.SubPlots[0].Series);
        Assert.IsType<MatPlotLibNet.Models.Series.NetworkGraphSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact]
    public void NetworkGraph_DerivesNodesFromUnionOfFromAndToColumns()
    {
        var fig = MakeEdgeListDf().NetworkGraph("source", "target").Build();
        var s = (MatPlotLibNet.Models.Series.NetworkGraphSeries)fig.SubPlots[0].Series[0];
        // Distinct node IDs = {a, b, c}
        Assert.Equal(3, s.Nodes.Count);
        var ids = s.Nodes.Select(n => n.Id).OrderBy(x => x).ToArray();
        Assert.Equal(new[] { "a", "b", "c" }, ids);
    }

    [Fact]
    public void NetworkGraph_PreservesEdgeCount()
    {
        var fig = MakeEdgeListDf().NetworkGraph("source", "target").Build();
        var s = (MatPlotLibNet.Models.Series.NetworkGraphSeries)fig.SubPlots[0].Series[0];
        Assert.Equal(3, s.Edges.Count);
    }

    [Fact]
    public void NetworkGraph_WithWeightColumn_AppliesWeights()
    {
        var df = MakeEdgeListDf(weight: [1.5, 2.5, 3.5]);
        var fig = df.NetworkGraph("source", "target", weightCol: "weight").Build();
        var s = (MatPlotLibNet.Models.Series.NetworkGraphSeries)fig.SubPlots[0].Series[0];
        Assert.Equal(1.5, s.Edges[0].Weight);
        Assert.Equal(2.5, s.Edges[1].Weight);
        Assert.Equal(3.5, s.Edges[2].Weight);
    }

    [Fact]
    public void NetworkGraph_WithoutWeightColumn_DefaultsToOne()
    {
        var fig = MakeEdgeListDf().NetworkGraph("source", "target").Build();
        var s = (MatPlotLibNet.Models.Series.NetworkGraphSeries)fig.SubPlots[0].Series[0];
        Assert.All(s.Edges, e => Assert.Equal(1.0, e.Weight));
    }

    [Fact]
    public void NetworkGraph_WithDirectedColumn_AppliesDirection()
    {
        var df = MakeEdgeListDf(directed: [true, false, true]);
        var fig = df.NetworkGraph("source", "target", directedCol: "directed").Build();
        var s = (MatPlotLibNet.Models.Series.NetworkGraphSeries)fig.SubPlots[0].Series[0];
        Assert.True( s.Edges[0].IsDirected);
        Assert.False(s.Edges[1].IsDirected);
        Assert.True( s.Edges[2].IsDirected);
    }

    [Fact]
    public void NetworkGraph_UnknownColumn_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => MakeEdgeListDf().NetworkGraph("source", "missing").Build());
    }

    [Fact]
    public void NetworkGraph_ReturnsChainableFigureBuilder()
    {
        var fig = MakeEdgeListDf().NetworkGraph("source", "target").WithTitle("My Graph").Build();
        Assert.Equal("My Graph", fig.Title);
    }

    [Fact]
    public void NetworkGraph_WithConfigure_InvokesCallback()
    {
        bool invoked = false;
        MakeEdgeListDf().NetworkGraph("source", "target", configure: _ => invoked = true).Build();
        Assert.True(invoked);
    }

    [Fact]
    public void NetworkGraph_DirectedColumn_NullValue_TreatedAsFalse()
    {
        // Boolean directed column with a null entry → `col[i] ?? false` fallback
        // path. This exercises the null-coalescing branch in the directed loop.
        var col = new PrimitiveDataFrameColumn<bool>("directed", new bool?[] { true, null, false });
        var df  = new Microsoft.Data.Analysis.DataFrame(
            new StringDataFrameColumn("source", new[] { "a", "a", "b" }),
            new StringDataFrameColumn("target", new[] { "b", "c", "c" }),
            col);
        var fig = df.NetworkGraph("source", "target", directedCol: "directed").Build();
        var s = (MatPlotLibNet.Models.Series.NetworkGraphSeries)fig.SubPlots[0].Series[0];
        Assert.True( s.Edges[0].IsDirected);
        Assert.False(s.Edges[1].IsDirected); // null → false fallback
        Assert.False(s.Edges[2].IsDirected);
    }
}
