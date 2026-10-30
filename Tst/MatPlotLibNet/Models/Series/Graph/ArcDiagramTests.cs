// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Models.Series.Graph;

/// <summary>An arc diagram (<see cref="GraphLayout.Arc"/>): the nodes in a row, node i at (i, 0) in the order they are stored, and
/// every edge a semicircle above the row from one of its nodes to the other, drawn as two cubic Bézier curves in data space. The
/// arc starts and ends on the nodes' pixels, its apex stands over the middle of the two nodes at half their distance (a half
/// circle under an aspect of one), a self-loop has no arc and keeps its row in the table, and a directed edge's head points
/// along the end of its arc.</summary>
public class ArcDiagramTests
{
    private static GraphNode N(string id) => new(id);
    private static GraphEdge E(string from, string to, bool directed = false) => new(from, to, 1.0, directed);

    private static readonly GraphNode[] Five = [N("a"), N("b"), N("c"), N("d"), N("e")];

    private static Figure Of(IReadOnlyList<GraphEdge> edges, Action<NetworkGraphSeries>? configure = null, GraphNode[]? nodes = null) =>
        Plt.Create().WithSize(800, 500).AddSubPlot(1, 1, 1, ax => ax
            .NetworkGraph(nodes ?? Five, edges, s => { s.Layout = GraphLayout.Arc; s.ShowNodeLabels = false; configure?.Invoke(s); })
            .SetAspect(1)).Build();

    private static RecordingRenderContext Drawn(Figure figure)
    {
        var recording = new RecordingRenderContext();
        new ChartRenderer().Render(figure, recording);
        return recording;
    }

    private static List<Point> NodeCentres(RecordingRenderContext drawn) =>
        [.. drawn.OfKind("DrawCircle").Select(call => (Point)((dynamic)call.Args!).center)];

    private static List<PathSegment[]> Arcs(RecordingRenderContext drawn) =>
        [.. drawn.OfKind("DrawPath").Select(call => (PathSegment[])((dynamic)call.Args!).segments)];

    [Fact]
    public void TheNodes_StandInARow_InTheOrderTheyAreStored_WhateverTheEdgesAndTheirOwnCoordinates()
    {
        GraphNode[] nodes = [new("a", 9, 9), new("b", -4, 2), new("c", 1, 1)];

        var positioned = NetworkGraphLayouts.Apply(GraphLayout.Arc, nodes, [E("c", "a")]);

        Assert.Equal([0.0, 1.0, 2.0], positioned.Select(node => node.X));
        Assert.All(positioned, node => Assert.Equal(0.0, node.Y));
        Assert.Equal(["a", "b", "c"], positioned.Select(node => node.Id));
    }

    [Fact]
    public void AnArcStartsAndEndsOnTheNodesPixels_ExactlyOnePerEdge()
    {
        var drawn = Drawn(Of([E("a", "d"), E("b", "c"), E("e", "b")]));
        var centres = NodeCentres(drawn);
        var arcs = Arcs(drawn);

        Assert.Equal(3, arcs.Count);
        (int From, int To)[] ends = [(0, 3), (1, 2), (4, 1)];
        for (int i = 0; i < arcs.Count; i++)
        {
            var start = ((MoveToSegment)arcs[i][0]).Point;
            var end = ((BezierSegment)arcs[i][^1]).End;
            Assert.Equal(centres[ends[i].From].X, start.X, 9);
            Assert.Equal(centres[ends[i].From].Y, start.Y, 9);
            Assert.Equal(centres[ends[i].To].X, end.X, 9);
            Assert.Equal(centres[ends[i].To].Y, end.Y, 9);
        }
    }

    [Fact]
    public void AnArc_IsTwoBezierCurves_MeetingAtAnApexOverTheMiddleOfTheTwoNodes()
    {
        var drawn = Drawn(Of([E("a", "e")]));
        var centres = NodeCentres(drawn);
        var arc = Assert.Single(Arcs(drawn));

        Assert.Equal(3, arc.Length);   // a move and two curves
        var apex = ((BezierSegment)arc[1]).End;
        Assert.Equal((centres[0].X + centres[4].X) / 2, apex.X, 9);
        Assert.True(apex.Y < centres[0].Y, "the arc rises above the row of nodes (up is toward smaller pixel rows)");
    }

    [Fact]
    public void UnderAnAspectOfOne_TheArcIsAHalfCircle_TheApexAtHalfTheDistanceOfItsNodes()
    {
        var drawn = Drawn(Of([E("a", "e"), E("b", "d")]));
        var centres = NodeCentres(drawn);

        foreach (var (arc, (from, to)) in Arcs(drawn).Zip(new[] { (0, 4), (1, 3) }))
        {
            var apex = ((BezierSegment)arc[1]).End;
            double radius = Math.Abs(centres[to].X - centres[from].X) / 2;
            Assert.Equal(centres[from].Y - radius, apex.Y, 6);
        }
    }

    [Fact]
    public void TheCurvesOfAnArc_FollowTheQuarterCircleConstant()
    {
        var drawn = Drawn(Of([E("a", "c")]));
        var centres = NodeCentres(drawn);
        var arc = Assert.Single(Arcs(drawn));
        const double k = 0.5522847498307936;
        double r = (centres[2].X - centres[0].X) / 2;
        var first = (BezierSegment)arc[1];
        var second = (BezierSegment)arc[2];
        double baseY = centres[0].Y, centreX = (centres[0].X + centres[2].X) / 2;

        Assert.Equal(centres[0].X, first.Control1.X, 6);
        Assert.Equal(baseY - k * r, first.Control1.Y, 6);
        Assert.Equal(centreX - k * r, first.Control2.X, 6);
        Assert.Equal(baseY - r, first.Control2.Y, 6);
        Assert.Equal(centreX + k * r, second.Control1.X, 6);
        Assert.Equal(baseY - r, second.Control1.Y, 6);
        Assert.Equal(centres[2].X, second.Control2.X, 6);
        Assert.Equal(baseY - k * r, second.Control2.Y, 6);
    }

    [Fact]
    public void AnEdgesWeight_StandsOverTheApexOfItsArc_WhenWeightsAreShown()
    {
        GraphEdge[] edges = [new("a", "e", 2.5), new("b", "d", 0.75)];

        var shown = Drawn(Of(edges, s => s.ShowEdgeWeights = true));
        var hidden = Drawn(Of(edges));

        // The axes draw their tick labels either way; the weights are the two texts that only the first figure has.
        Assert.Equal(hidden.OfKind("DrawText").Count() + 2, shown.OfKind("DrawText").Count());
        var texts = shown.OfKind("DrawText").Select(call => (dynamic)call.Args!).ToList();
        var apexes = Arcs(shown).Select(arc => ((BezierSegment)arc[1]).End).ToList();
        string[] weights = ["2.5", "0.75"];
        for (int i = 0; i < apexes.Count; i++)
        {
            var label = Assert.Single(texts, text => Math.Abs(((Point)text.position).X - apexes[i].X) < 1e-9 && Math.Abs(((Point)text.position).Y - apexes[i].Y) < 1e-9);
            Assert.Equal(weights[i], (string)label.text);
            Assert.Equal(TextAlignment.Center, (TextAlignment)label.alignment);
        }
    }

    [Fact]
    public void AnEdgeBackwards_IsTheSameArcAboveTheRow_ItJustRunsTheOtherWay()
    {
        var drawn = Drawn(Of([E("d", "a")]));
        var centres = NodeCentres(drawn);
        var arc = Assert.Single(Arcs(drawn));

        Assert.Equal(centres[3].X, ((MoveToSegment)arc[0]).Point.X, 9);
        Assert.Equal(centres[0].X, ((BezierSegment)arc[^1]).End.X, 9);
        Assert.True(((BezierSegment)arc[1]).End.Y < centres[0].Y);
    }

    [Fact]
    public void TheArcs_AlwaysFitTheRange_ZeroToNMinusOneAlongXAndZeroToHalfOfThatAlongY()
    {
        var figure = Of([E("a", "e"), E("a", "c")]);
        var plot = new ChartRenderer().ComputeLayout(figure, new MatPlotLibNet.Rendering.Svg.SvgRenderContext()).PlotAreas[0];
        var arcs = Arcs(Drawn(figure));

        foreach (var arc in arcs)
        {
            foreach (var point in arc.OfType<BezierSegment>().SelectMany(b => new[] { b.Control1, b.Control2, b.End }))
            {
                Assert.InRange(point.X, plot.X - 1e-6, plot.X + plot.Width + 1e-6);
                Assert.InRange(point.Y, plot.Y - 1e-6, plot.Y + plot.Height + 1e-6);
            }
        }
    }

    [Fact]
    public void ASelfLoop_DrawsNoArc_AndIsStillInTheTable()
    {
        var series = new NetworkGraphSeries(Five, [E("a", "a"), E("a", "b")]) { Layout = GraphLayout.Arc };
        var figure = Plt.Create().WithSize(800, 500).AddSubPlot(1, 1, 1, ax => ax.NetworkGraph(Five, [E("a", "a"), E("a", "b")], s => s.Layout = GraphLayout.Arc)).Build();

        Assert.Single(Arcs(Drawn(figure)));
        Assert.Equal(2, series.ToDataTable()!.Rows.Count);
    }

    [Fact]
    public void ADirectedEdge_HasItsHeadOnTheEndNode_PointingAlongTheEndOfTheArc()
    {
        var drawn = Drawn(Of([E("a", "e", directed: true)]));
        var centres = NodeCentres(drawn);

        var head = Assert.Single(drawn.OfKind("DrawPolygon").Select(call => (Point[])((dynamic)call.Args!).points));

        // The arc comes down straight onto the node: the head's tip is the node's pixel and its base is above it, at its own x.
        Assert.Contains(head, p => Math.Abs(p.X - centres[4].X) < 1e-6 && Math.Abs(p.Y - centres[4].Y) < 1e-6);
        Assert.All(head.Where(p => !(Math.Abs(p.X - centres[4].X) < 1e-6 && Math.Abs(p.Y - centres[4].Y) < 1e-6)), p => Assert.True(p.Y < centres[4].Y));
    }

    [Fact]
    public void AnUndirectedEdge_HasNoHead()
    {
        Assert.Empty(Drawn(Of([E("a", "e")])).OfKind("DrawPolygon"));
    }

    [Fact]
    public void TheArcLayout_SurvivesTheJson_AndDrawsTheSamePicture()
    {
        var figure = Of([E("a", "d"), E("b", "c", directed: true)]);

        var back = ChartSerializerDefault.Current.FromJson(figure.ToJson());

        Assert.Equal(GraphLayout.Arc, Assert.IsType<NetworkGraphSeries>(Assert.Single(back.SubPlots[0].Series)).Layout);
        Assert.Equal(figure.ToSvg(), back.ToSvg());
    }

    [Fact]
    public void TheOtherLayouts_StillDrawTheirEdgesAsStraightLines()
    {
        var figure = Plt.Create().WithSize(800, 500).AddSubPlot(1, 1, 1, ax => ax
            .NetworkGraph(Five, [E("a", "d")], s => s.Layout = GraphLayout.Circular)).Build();

        var drawn = Drawn(figure);

        Assert.Empty(drawn.OfKind("DrawPath"));
        Assert.NotEmpty(drawn.OfKind("DrawLine"));
    }
}
