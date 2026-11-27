// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series.Graph;

/// <summary>Verifies <see cref="NetworkGraphSeries"/> construction and its data range. What it starts from and the settings it
/// stores are held by the resting-value corpus and the drawn-property round trips.</summary>
public class NetworkGraphSeriesTests
{
    private static GraphNode N(string id) => new(id);

    // ── Construction ─────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_AcceptsEmptyEdges()
    {
        var s = new NetworkGraphSeries([N("a")], []);
        Assert.Single(s.Nodes);
        Assert.Empty(s.Edges);
    }

    // ── ComputeDataRange ─────────────────────────────────────────────────────

    [Fact]
    public void ComputeDataRange_NoEdgesNoExplicitCoords_ReturnsCircularBoundsForCircularLayout()
    {
        // With Circular layout the renderer positions every node on the unit circle —
        // axes range covers [-1, 1] in both dimensions.
        var s = new NetworkGraphSeries([N("a"), N("b"), N("c"), N("d")], []) { Layout = GraphLayout.Circular };
        var range = s.ComputeDataRange(null!);
        // We don't insist on exact -1/+1 (renderer may add margin); just the broad shape.
        Assert.True(range.XMin <= 0);
        Assert.True(range.XMax >= 0);
        Assert.True(range.YMin <= 0);
        Assert.True(range.YMax >= 0);
    }

    [Fact]
    public void ComputeDataRange_ManualLayout_UsesNodeCoords()
    {
        var nodes = new[]
        {
            new GraphNode("a", X: -10, Y: -5),
            new GraphNode("b", X: 10,  Y: 5),
        };
        var s = new NetworkGraphSeries(nodes, []) { Layout = GraphLayout.Manual };
        var range = s.ComputeDataRange(null!);
        Assert.True(range.XMin <= -10);
        Assert.True(range.XMax >= 10);
        Assert.True(range.YMin <= -5);
        Assert.True(range.YMax >= 5);
    }

    [Fact]
    public void ComputeDataRange_EmptyNodes_ReturnsNullRange()
    {
        var s = new NetworkGraphSeries([], []);
        var range = s.ComputeDataRange(null!);
        Assert.Null(range.XMin);
        Assert.Null(range.XMax);
    }

    /// <summary>A span a rounding error wide is flat: it opens by half a unit on each side, as the axis opens a flat range
    /// (<see cref="MatPlotLibNet.Rendering.Range1D.Normalized"/>), not only a span of exactly nothing.</summary>
    [Fact]
    public void ComputeDataRange_ASpanARoundingErrorWide_OpensByHalfAUnitAsTheAxisDoes()
    {
        var nodes = new[] { new GraphNode("a", X: 1, Y: 2), new GraphNode("b", X: 1 + 1e-12, Y: 3) };
        var range = new NetworkGraphSeries(nodes, []) { Layout = GraphLayout.Manual }.ComputeDataRange(null!);

        Assert.Equal(0.5, range.XMin);
        Assert.Equal(1 + 1e-12 + 0.5, range.XMax);
        Assert.Equal(2, range.YMin);
        Assert.Equal(3, range.YMax);
    }
}
