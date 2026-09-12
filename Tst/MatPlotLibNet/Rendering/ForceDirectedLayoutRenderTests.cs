// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>
/// A force-directed graph is laid out twice per render: once by <c>NetworkGraphSeries.ComputeDataRange</c> to find
/// the axis bounds, and once by the renderer to place the nodes. The range pass was given the caller's
/// <c>LayoutSeed</c>, <c>LayoutIterations</c> and <c>ConvergenceThreshold</c> and the drawing pass was not, so the
/// axes were scaled to one layout while the nodes came from another — and a node could land outside the plot area.
/// Measured before the fix on this twelve-node graph: two nodes outside at one iteration, five at five iterations,
/// and a seed that changed nothing at all about the picture.
/// </summary>
public class ForceDirectedLayoutRenderTests
{
    private static (IReadOnlyList<GraphNode> Nodes, IReadOnlyList<GraphEdge> Edges) Ring(int count)
    {
        var nodes = new List<GraphNode>();
        for (int i = 0; i < count; i++) nodes.Add(new GraphNode($"n{i}"));

        var edges = new List<GraphEdge>();
        for (int i = 0; i < count - 1; i++) edges.Add(new GraphEdge($"n{i}", $"n{i + 1}"));
        edges.Add(new GraphEdge($"n{count - 1}", "n0"));
        edges.Add(new GraphEdge("n3", "n8"));
        return (nodes, edges);
    }

    private static string Render(int seed = 0, int iterations = 50)
    {
        var (nodes, edges) = Ring(12);
        return Plt.Create().WithSize(800, 600)
            .AddSubPlot(1, 1, 1, ax => ax.NetworkGraph(nodes, edges, s =>
            {
                s.Layout = GraphLayout.ForceDirected;
                s.LayoutSeed = seed;
                s.LayoutIterations = iterations;
            }))
            .Build().ToSvg();
    }

    /// <summary>Every node marker, as (cx, cy) in pixels, in document order.</summary>
    private static (double X, double Y)[] Nodes(string svg) =>
        [.. Regex.Matches(svg, @"<circle[^>]*\bcx=""([0-9.eE+-]+)""[^>]*\bcy=""([0-9.eE+-]+)""")
            .Select(m => (double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                          double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture)))];

    /// <summary>The plot rectangle the axes reserved: the widest rectangle that is not the whole figure.</summary>
    private static (double X0, double X1, double Y0, double Y1) PlotArea(string svg)
    {
        double x0 = 0, y0 = 0, w = 0, h = 0;
        foreach (Match m in Regex.Matches(svg,
            @"<rect x=""([0-9.eE+-]+)"" y=""([0-9.eE+-]+)"" width=""([0-9.eE+-]+)"" height=""([0-9.eE+-]+)"""))
        {
            double mw = double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
            double mh = double.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture);
            if (mw < 799 && mw * mh > w * h)
            {
                x0 = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                y0 = double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                w = mw; h = mh;
            }
        }
        return (x0, x0 + w, y0, y0 + h);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(50)]
    public void EveryNode_IsDrawnInsideThePlotArea(int iterations)
    {
        string svg = Render(iterations: iterations);
        var area = PlotArea(svg);

        var outside = Nodes(svg)
            .Where(n => n.X < area.X0 - 0.5 || n.X > area.X1 + 0.5 || n.Y < area.Y0 - 0.5 || n.Y > area.Y1 + 0.5)
            .ToArray();

        Assert.True(outside.Length == 0,
            $"{outside.Length} node(s) outside the plot area {area.X0:F0}..{area.X1:F0} x {area.Y0:F0}..{area.Y1:F0}: " +
            string.Join(", ", outside.Select(n => $"({n.X:F0},{n.Y:F0})")));
    }

    [Fact]
    public void TheLayoutSeed_ChangesWhereTheNodesAreDrawn()
    {
        var a = Nodes(Render(seed: 0));
        var b = Nodes(Render(seed: 42));

        Assert.Equal(a.Length, b.Length);
        Assert.True(a.Zip(b).Any(p => Math.Abs(p.First.X - p.Second.X) > 1e-6 || Math.Abs(p.First.Y - p.Second.Y) > 1e-6),
            "every node landed on the same pixel for seed 0 and seed 42 — the seed never reached the drawing");
    }

    [Fact]
    public void TheSameSeed_DrawsTheSameGraphTwice()
    {
        Assert.Equal(Nodes(Render(seed: 7)), Nodes(Render(seed: 7)));
    }

    [Fact]
    public void MoreIterations_MoveTheNodes()
    {
        var few = Nodes(Render(iterations: 2));
        var many = Nodes(Render(iterations: 200));

        Assert.True(few.Zip(many).Any(p => Math.Abs(p.First.X - p.Second.X) > 1e-6 || Math.Abs(p.First.Y - p.Second.Y) > 1e-6),
            "two iterations and two hundred drew the same picture — the iteration count never reached the drawing");
    }
}
