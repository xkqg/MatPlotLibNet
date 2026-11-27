// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>A network graph's nodes and edges come back from JSON with every field of theirs, and an empty graph comes back empty.
/// The graph's own settings are held by the drawn-property contract (<see cref="SeriesDrawnPropertiesRoundTripTests"/>); its nodes and
/// edges are records of data whose fields the contract's fixture leaves at rest, so they are held here.</summary>
public class NetworkGraphSerializationTests
{
    [Fact]
    public void TheNodesAndEdges_ComeBackWithEveryFieldTheyCarry()
    {
        GraphNode[] nodes = [new("a", X: 1, Y: 2, ColorScalar: 0.3, SizeScalar: 1.5, Label: "Alpha"), new("b", X: 3, Y: 4, ColorScalar: 0.7, SizeScalar: 2.0)];
        GraphEdge[] edges = [new("a", "b", Weight: 1.5, IsDirected: true)];

        var back = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.NetworkGraph(nodes, edges)).Build()
            .RoundTripped().SubPlots[0].Series.OfType<NetworkGraphSeries>().Single();

        Assert.Equal(nodes, back.Nodes);
        Assert.Equal(edges, back.Edges);
    }

    [Fact]
    public void AGraphWithNoNodesAndNoEdges_ComesBackEmpty()
    {
        var back = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.NetworkGraph([], [])).Build()
            .RoundTripped().SubPlots[0].Series.OfType<NetworkGraphSeries>().Single();

        Assert.Empty(back.Nodes);
        Assert.Empty(back.Edges);
    }
}
