// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Models.Series.Graph;

/// <summary>The layout a network graph is placed by: one <see cref="GraphLayoutAlgorithm"/> for each <see cref="GraphLayout"/>,
/// chosen in one place, and the positions the series works out once for each change instead of once for its data range and
/// again for its drawing.</summary>
public class GraphLayoutAlgorithmTests
{
    private static GraphNode N(string id, double x = 0, double y = 0) => new(id, x, y);
    private static GraphEdge E(string from, string to) => new(from, to, 1.0, false);

    private static NetworkGraphSeries Series(GraphLayout layout) =>
        new([N("a", 1, 2), N("b", 3, 4), N("c", 5, 6)], [E("a", "b"), E("b", "c")]) { Layout = layout };

    // ── The one mapping ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(GraphLayout.Manual, typeof(ManualGraphLayout))]
    [InlineData(GraphLayout.Circular, typeof(CircularGraphLayout))]
    [InlineData(GraphLayout.Hierarchical, typeof(HierarchicalGraphLayout))]
    [InlineData(GraphLayout.ForceDirected, typeof(ForceDirectedGraphLayout))]
    [InlineData(GraphLayout.Arc, typeof(ArcGraphLayout))]
    public void EveryLayout_IsPlacedByItsOwnAlgorithm(GraphLayout layout, Type algorithm) =>
        Assert.IsType(algorithm, GraphLayoutAlgorithm.For(Series(layout)));

    [Fact]
    public void ALayoutTheEnumDoesNotDeclare_KeepsTheCoordinatesOfTheNodes() =>
        Assert.IsType<ManualGraphLayout>(GraphLayoutAlgorithm.For(Series((GraphLayout)99)));

    [Fact]
    public void TheSpringEmbedder_TakesTheSeedTheIterationsAndTheThresholdOfTheSeries()
    {
        var series = Series(GraphLayout.ForceDirected);
        series.LayoutSeed = 7;
        series.LayoutIterations = 12;
        series.ConvergenceThreshold = 0.25;

        var algorithm = Assert.IsType<ForceDirectedGraphLayout>(GraphLayoutAlgorithm.For(series));

        // The embedder the series hands out places the nodes as one built from those three values does, and as another seed does not.
        var placed = algorithm.Place(series.Nodes, series.Edges);
        Assert.Equal(new ForceDirectedGraphLayout(7, 12, 0.25).Place(series.Nodes, series.Edges), placed);
        Assert.NotEqual(new ForceDirectedGraphLayout(8, 12, 0.25).Place(series.Nodes, series.Edges), placed);
        Assert.NotEqual(new ForceDirectedGraphLayout(7, 3, 0.25).Place(series.Nodes, series.Edges), placed);
        Assert.NotEqual(new ForceDirectedGraphLayout(7, 12, null).Place(series.Nodes, series.Edges), placed);
    }

    [Fact]
    public void OnlyTheArcDiagram_DrawsItsEdgesAsArcs_AndReachesAboveItsNodes()
    {
        var positioned = new[] { N("a", 0, 0), N("b", 1, 0), N("c", 4, 0) };
        GraphEdge[] edges = [E("a", "c")];

        foreach (GraphLayout layout in Enum.GetValues<GraphLayout>())
        {
            var algorithm = GraphLayoutAlgorithm.For(Series(layout));
            bool arc = layout == GraphLayout.Arc;
            Assert.Equal(arc, algorithm.DrawsEdgesAsArcs);
            Assert.Equal(arc ? 2.0 : 0.5, algorithm.Top(positioned, edges, 0.5));
        }
    }

    // ── Once per change ──────────────────────────────────────────────────────

    [Fact]
    public void ThePositions_AreWorkedOutOnce_AndHandedOutAgainWhileNothingChanges()
    {
        var series = Series(GraphLayout.ForceDirected);

        var first = series.Placement;

        Assert.Same(first, series.Placement);
        Assert.Same(first.Nodes, series.Placement.Nodes);
    }

    [Fact]
    public void ThePositions_AreWorkedOutAgain_AfterAChangeToTheSeries()
    {
        var series = Series(GraphLayout.ForceDirected);
        var before = series.Placement;

        series.LayoutSeed = 5;

        Assert.NotSame(before, series.Placement);
        Assert.Equal(GraphLayoutAlgorithm.For(series).Place(series.Nodes, series.Edges).Select(n => n.X),
            series.Placement.Nodes.Select(n => n.X));
    }

    [Fact]
    public void SettingAPropertyToTheValueItHas_KeepsThePositions()
    {
        var series = Series(GraphLayout.Circular);
        var before = series.Placement;

        series.Layout = GraphLayout.Circular;

        Assert.Same(before, series.Placement);
    }

    [Fact]
    public void ChangingTheLayout_PlacesTheNodesByTheNewOne()
    {
        var series = Series(GraphLayout.Manual);
        Assert.Equal(1, series.Placement.Nodes[0].X);

        series.Layout = GraphLayout.Arc;

        Assert.IsType<ArcGraphLayout>(series.Placement.Algorithm);
        Assert.Equal([0.0, 1.0, 2.0], series.Placement.Nodes.Select(n => n.X));
    }

    [Fact]
    public void DrawingTheFigure_UsesThePositionsTheDataRangeWorkedOut()
    {
        NetworkGraphSeries? series = null;
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.NetworkGraph([N("a"), N("b"), N("c")], [E("a", "b")], s =>
            {
                s.Layout = GraphLayout.ForceDirected;
                series = s;
            }))
            .Build();
        var placed = series!.Placement;

        _ = figure.ToSvg();

        Assert.Same(placed, series.Placement);
    }
}
