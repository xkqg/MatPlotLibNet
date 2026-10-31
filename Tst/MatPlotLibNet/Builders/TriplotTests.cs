// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The <c>Triplot</c> verb (matplotlib's <c>triplot</c>): the edges of a triangulated point set, drawn as ONE line series
/// (one colour slot, one legend entry) of unique edges, each a segment of its own parted from the next by a missing point. The
/// triangles are the caller's when given and the Delaunay triangulation of the points otherwise (<c>Delaunay.TrianglesOf</c>, the
/// one owner of that rule, held in <c>DelaunayTrianglesOfTests</c>); the segments are <c>SegmentPath</c>'s, held to their rules in
/// <c>SegmentPathTests</c>. This holds what the verb does with them: every edge once, drawn from the caller's own coordinates.</summary>
public class TriplotTests
{
    private static readonly Color SeriesColour = Theme.Default.CycleColors[0];

    private const double Gap = double.NaN;

    /// <summary>The corners of a square and its centre: five points whose triangulation is the four triangles meeting at the centre.</summary>
    private static readonly double[] SquareX = [0, 2, 2, 0, 1];

    private static readonly double[] SquareY = [0, 0, 2, 2, 1];

    /// <summary>The four triangles of the square, given: around the centre (point 4), each with the next corner.</summary>
    private static readonly int[] SquareTriangles = [0, 1, 4, 1, 2, 4, 2, 3, 4, 3, 0, 4];

    private static Figure Of(double[] x, double[] y, int[]? triangles = null, Action<LineSeries>? configure = null) =>
        Plt.Create().WithSize(800, 400).AddSubPlot(1, 1, 1, ax => ax.Triplot(x, y, triangles, configure)).Build();

    private static RecordingRenderContext Drawn(Figure figure)
    {
        var recording = new RecordingRenderContext();
        new ChartRenderer().Render(figure, recording);
        return recording;
    }

    /// <summary>The polylines the series is drawn with, each as its points in pixels, in the order drawn.</summary>
    private static List<Point[]> Polylines(RecordingRenderContext drawn) => [.. drawn.OfKind("DrawLines")
        .Select(call => (dynamic)call.Args!)
        .Where(args => (Color)args.color == SeriesColour && (double)args.thickness == 1.5)
        .Select(args => (Point[])args.points)];

    private static LineSeries OnlySeriesOf(Figure figure) => Assert.IsType<LineSeries>(Assert.Single(figure.SubPlots[0].Series));

    /// <summary>One edge as it is drawn: its two ends.</summary>
    private readonly record struct Edge(double X0, double Y0, double X1, double Y1)
    {
        /// <summary>The same edge whichever way round it runs: the end that comes first in (x, y) order first.</summary>
        public Edge Unordered() => X0 < X1 || (X0 == X1 && Y0 <= Y1) ? this : new Edge(X1, Y1, X0, Y0);
    }

    /// <summary>The edges of a line of segments, each as its two ends, with the missing point that parts each from the next.</summary>
    private static List<Edge> SegmentsOf(LineSeries line)
    {
        var segments = new List<Edge>();
        for (int i = 0; i + 2 < line.XData.Length; i += 3)
        {
            Assert.True(double.IsNaN(line.XData[i + 2]) && double.IsNaN(line.YData[i + 2]), "a segment is followed by a missing point");
            segments.Add(new Edge(line.XData[i], line.YData[i], line.XData[i + 1], line.YData[i + 1]));
        }

        return segments;
    }

    // ---- the shape the contracts hold ---------------------------------------------------------------------------------

    [Fact]
    public void TheVerb_DrawsExactlyWhatTheHandRecipeDraws()
    {
        Assert.Equal(ComposedChartShapes.TriplotByHand(false).ToSvg(), ComposedChartShapes.Named("triplot").Build(false).ToSvg());
        Assert.Equal(ComposedChartShapes.TriplotByHand(true).ToSvg(), ComposedChartShapes.Named("triplot").Build(true).ToSvg());
    }

    // ---- the edges ------------------------------------------------------------------------------------------------------

    [Fact]
    public void TheChart_IsOneLineSeries_OfTheEdges_EachWithItsEndsAndAGap()
    {
        var line = OnlySeriesOf(Of(SquareX, SquareY, SquareTriangles));

        // Eight edges in the order they are first met: the three of the first triangle, then the two new ones of each next.
        Assert.Equal([0.0, 2.0, Gap, 2.0, 1.0, Gap, 1.0, 0.0, Gap, 2.0, 2.0, Gap, 2.0, 1.0, Gap, 2.0, 0.0, Gap, 0.0, 1.0, Gap, 0.0, 0.0, Gap], line.XData);
        Assert.Equal([0.0, 0.0, Gap, 0.0, 1.0, Gap, 1.0, 0.0, Gap, 0.0, 2.0, Gap, 2.0, 1.0, Gap, 2.0, 2.0, Gap, 2.0, 1.0, Gap, 2.0, 0.0, Gap], line.YData);
    }

    [Fact]
    public void FivePoints_ThatTriangulateToEightEdges_AreEightSegments()
    {
        var figure = Of(SquareX, SquareY);   // four triangles, twelve sides, eight edges: each spoke is shared by two triangles

        Assert.Equal(4, Delaunay.Triangulate(SquareX, SquareY).Triangles.Length / 3);
        Assert.Equal(8, SegmentsOf(OnlySeriesOf(figure)).Count);
        Assert.Equal(8, Polylines(Drawn(figure)).Count);
    }

    [Fact]
    public void AnEdgeTwoTrianglesShare_IsDrawnOnce_WhicheverWayEachRunsIt()
    {
        // The triangles (0, 1, 2) and (2, 1, 3) share the edge between points 1 and 2, the first running it 1 -> 2 and the
        // second 2 -> 1: six sides, five edges.
        double[] x = [0, 2, 1, 3];
        double[] y = [0, 0, 2, 2];

        var segments = SegmentsOf(OnlySeriesOf(Of(x, y, [0, 1, 2, 2, 1, 3])));

        Assert.Equal(5, segments.Count);
        Assert.Equal(5, segments.Select(edge => edge.Unordered()).Distinct().Count());
    }

    [Fact]
    public void AnEdge_IsDrawnWhereTheVerticesAre_InTheOrderTheTrianglesNameThem()
    {
        var segments = SegmentsOf(OnlySeriesOf(Of([0, 4, 0], [0, 0, 3], [0, 1, 2])));

        Assert.Equal([new Edge(0, 0, 4, 0), new Edge(4, 0, 0, 3), new Edge(0, 3, 0, 0)], segments);
    }

    [Fact]
    public void WithoutTriangles_TheEdgesAreThoseOfTheDelaunayTriangulation_ThatTripcolorFills()
    {
        var byThePoints = OnlySeriesOf(Of(SquareX, SquareY));
        var byTheMesh = OnlySeriesOf(Of(SquareX, SquareY, Delaunay.Triangulate(SquareX, SquareY).Triangles));

        Assert.Equal(byTheMesh.XData, byThePoints.XData);
        Assert.Equal(byTheMesh.YData, byThePoints.YData);
    }

    [Fact]
    public void GivenTriangles_Win_OverTheTriangulationOfThePoints()
    {
        var segments = SegmentsOf(OnlySeriesOf(Of(SquareX, SquareY, [0, 1, 2])));

        Assert.Equal(3, segments.Count);   // the points alone would have drawn eight
    }

    [Fact]
    public void TheEdges_AreDrawnFromTheCallersOwnCoordinates_NotFromTheMeshTheTriangulationReturns()
    {
        // The triangulation nudges the x of the points it is given (a deterministic jitter that keeps collinear points apart) and
        // returns the nudged copy; the triangles are all the verb takes from it.
        double[] x = [0, 7, 7, 0, 3.5];
        double[] y = [0, 0, 7, 7, 3.5];
        Assert.NotEqual(x, Delaunay.Triangulate(x, y).X);

        var line = OnlySeriesOf(Of(x, y));

        Assert.All(line.XData.Where(double.IsFinite), value => Assert.Contains(value, x));
        Assert.All(line.YData.Where(double.IsFinite), value => Assert.Contains(value, y));
    }

    [Fact]
    public void ADegenerateTriangle_ThatNamesOnePointTwice_DrawsNoEdgeFromThatPointToItself()
    {
        var segments = SegmentsOf(OnlySeriesOf(Of([0, 4, 0], [0, 0, 3], [0, 0, 1])));

        Assert.Equal([new Edge(0, 0, 4, 0)], segments);
    }

    [Fact]
    public void ASingleEdge_IsOneSegment_NotBroadcastIntoMore()
    {
        // One edge is arrays of one value in the segment path, which a path reads as "the same for every segment": still one.
        var line = OnlySeriesOf(Of([0, 4, 0], [0, 0, 3], [0, 1, 1]));

        Assert.Equal([0.0, 4.0, Gap], line.XData);
        Assert.Equal([0.0, 0.0, Gap], line.YData);
    }

    [Fact]
    public void TheSvg_HoldsOnePolylineForEachEdge()
    {
        string svg = Of(SquareX, SquareY, SquareTriangles).ToSvg();

        Assert.Equal(8, Regex.Matches(svg, "<polyline").Count);
        Assert.DoesNotContain("NaN", svg);
    }

    // ---- nothing to draw ----------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void FewerThanThreePoints_AddOneEmptyLineSeries_AndDoNotBreakTheFigure(int points)
    {
        double[] x = [.. Enumerable.Range(0, points).Select(i => (double)i)];
        double[] y = [.. Enumerable.Range(0, points).Select(i => (double)(2 * i))];

        var figure = Of(x, y);

        Assert.Empty(OnlySeriesOf(figure).XData);
        Assert.Empty(OnlySeriesOf(figure).YData);
        Assert.NotEmpty(figure.ToSvg());
    }

    [Fact]
    public void FewerThanThreePoints_DrawNothingEvenWithGivenTriangles_AsTheTrianglePlotDrawsNothing()
    {
        // Two points cannot make a triangle: a list that names one corner twice is degenerate and draws no edge here either.
        var figure = Of([0.0, 1.0], [0.0, 1.0], [0, 1, 1]);

        Assert.Empty(OnlySeriesOf(figure).XData);
        Assert.NotEmpty(figure.ToSvg());
    }

    [Fact]
    public void NoTriangles_AddOneEmptyLineSeries_AndDoNotBreakTheFigure()
    {
        var figure = Of(SquareX, SquareY, []);

        Assert.Empty(OnlySeriesOf(figure).XData);
        Assert.NotEmpty(figure.ToSvg());
    }

    // ---- a vertex that cannot be placed -------------------------------------------------------------------------------

    [Fact]
    public void AVertexWithAMissingCoordinate_DropsItsEdgesWhole_AndTheOthersStayParted()
    {
        double[] x = [0, 2, double.NaN, 0, 1];   // the corner the first two triangles and the last two meet at

        var line = OnlySeriesOf(Of(x, SquareY, SquareTriangles));

        // The eight edges are the four sides of the square and the four spokes; point 2 is on two sides and one spoke.
        var segments = SegmentsOf(line);
        Assert.Equal(5, segments.Count);
        Assert.All(segments, s => Assert.True(double.IsFinite(s.X0) && double.IsFinite(s.X1) && double.IsFinite(s.Y0) && double.IsFinite(s.Y1)));
        Assert.Equal(5, Polylines(Drawn(Of(x, SquareY, SquareTriangles))).Count);
    }

    [Fact]
    public void AnInfiniteCoordinate_IsNoVertexEither()
    {
        double[] y = [0, 0, 2, double.PositiveInfinity, 1];

        var segments = SegmentsOf(OnlySeriesOf(Of(SquareX, y, SquareTriangles)));

        Assert.Equal(5, segments.Count);
    }

    [Fact]
    public void WithoutTriangles_APointThatCannotBePlaced_IsNoVertex_AndTheOthersAreTriangulatedWithoutIt()
    {
        double[] x = [0, 2, 2, 0, 1, double.NaN];
        double[] y = [0, 0, 2, 2, 1, 5];

        var line = OnlySeriesOf(Of(x, y));

        Assert.Equal(8, SegmentsOf(line).Count);
        Assert.DoesNotContain(5.0, line.YData);
    }

    // ---- the series -----------------------------------------------------------------------------------------------------

    [Fact]
    public void TheEdges_AreOneLegendEntry()
    {
        var figure = Plt.Create().WithSize(800, 400).AddSubPlot(1, 1, 1, ax => ax
            .Triplot(SquareX, SquareY, SquareTriangles, line => line.Label = "mesh")
            .WithLegend()).Build();
        figure.EnableLegendToggle = true;

        string svg = figure.ToSvg();

        Assert.Single(Regex.Matches(svg, "data-legend-index="));
        Assert.Contains("mesh", svg);
    }

    [Fact]
    public void TheEdges_AreOneColourSlot_TheNextSeriesTakesTheNextColour()
    {
        var figure = Plt.Create().WithSize(800, 400).AddSubPlot(1, 1, 1, ax => ax
            .Triplot(SquareX, SquareY, SquareTriangles)
            .Plot([0.0, 4.0], [1.0, 2.0])).Build();

        var drawn = Drawn(figure);

        Assert.Equal(8, Polylines(drawn).Count);
        Assert.Contains(drawn.OfKind("DrawLines").Select(call => (Color)((dynamic)call.Args!).color), colour => colour == Theme.Default.CycleColors[1]);
    }

    [Fact]
    public void WhatTheCallbackSets_WinsOverWhatTheVerbDid()
    {
        var figure = Of(SquareX, SquareY, SquareTriangles, line => { line.Color = Colors.Red; line.LineWidth = 4; line.LineStyle = LineStyle.Dashed; });

        var line = OnlySeriesOf(figure);

        Assert.Equal(Colors.Red, line.Color);
        Assert.Equal(4.0, line.LineWidth);
        Assert.Contains(Drawn(figure).OfKind("DrawLines"), call => (Color)((dynamic)call.Args!).color == Colors.Red);
    }

    [Fact]
    public void TheCallback_RunsOnceOnTheSeriesTheVerbAdded()
    {
        var seen = new List<LineSeries>();

        var figure = Of(SquareX, SquareY, SquareTriangles, seen.Add);

        Assert.Same(OnlySeriesOf(figure), Assert.Single(seen));
    }

    [Fact]
    public void TheVerb_ChainsWithTheRestOfTheBuilder()
    {
        var builder = Plt.Create().AddSubPlot(1, 1, 1, ax =>
        {
            var same = ax.Triplot(SquareX, SquareY);
            Assert.Same(ax, same);
        });

        Assert.Single(builder.Build().SubPlots[0].Series);
    }

    [Fact]
    public void TheChart_SurvivesTheJson_AndDrawsTheSamePicture()
    {
        double[] x = [0, 2, double.NaN, 0, 1];
        var figure = Of(x, SquareY, SquareTriangles, line => line.Label = "mesh");

        string json = figure.ToJson();
        var back = ChartSerializerDefault.Current.FromJson(json);

        Assert.Equal(figure.ToSvg(), back.ToSvg());
        Assert.Single(back.SubPlots[0].Series);
        Assert.Contains("\"NaN\"", json);
    }

    // ---- what the verb refuses ----------------------------------------------------------------------------------------

    [Theory]
    [InlineData(5)]
    [InlineData(100)]
    [InlineData(-1)]
    public void ATriangleIndex_OutsideThePoints_IsRefusedByTheNameOfTheTriangles(int index)
    {
        var refused = Assert.Throws<ArgumentException>(() => Of(SquareX, SquareY, [0, 1, 4, 1, 2, index]));

        Assert.Equal("triangles", refused.ParamName);
        Assert.Contains("triangles[5]", refused.Message, StringComparison.Ordinal);
        Assert.Contains(index.ToString(System.Globalization.CultureInfo.InvariantCulture), refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ATriangleIndex_IsRefusedWhenThereAreFewerThanThreePointsToo()
    {
        Assert.Equal("triangles", Assert.Throws<ArgumentException>(() => Of([0.0, 1.0], [0.0, 1.0], [0, 1, 2])).ParamName);
    }

    [Fact]
    public void ATriangle_WhenThereAreNoPointsAtAll_IsRefusedAndTheMessageSaysSo()
    {
        var refused = Assert.Throws<ArgumentException>(() => Of([], [], [0, 1, 2]));

        Assert.Equal("triangles", refused.ParamName);
        Assert.Contains("no points", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TrianglesThatAreNotWholeTriangles_AreRefusedByName()
    {
        var refused = Assert.Throws<ArgumentException>(() => Of(SquareX, SquareY, [0, 1, 4, 1]));

        Assert.Equal("triangles", refused.ParamName);
        Assert.Contains("three", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void YOfAnotherLengthThanX_IsRefusedByName()
    {
        Assert.Equal("y", Assert.Throws<ArgumentException>(() => Of([0, 1, 2], [0, 1])).ParamName);
    }

    [Fact]
    public void ARefusedCall_LeavesTheAxesAsItWas()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax =>
        {
            Assert.Throws<ArgumentException>(() => ax.Triplot(SquareX, SquareY, [0, 1, 9]));
            Assert.Throws<ArgumentException>(() => ax.Triplot(SquareX, [0.0, 1.0]));
        }).Build().SubPlots[0];

        Assert.Empty(axes.Series);
    }

    [Fact]
    public void ANullArgument_IsRefusedByName()
    {
        Assert.Equal("x", Assert.Throws<ArgumentNullException>(() => Of(null!, [0.0])).ParamName);
        Assert.Equal("y", Assert.Throws<ArgumentNullException>(() => Of([0.0], null!)).ParamName);
    }

    [Fact]
    public void ANullBuilder_IsRefused()
    {
        AxesBuilder axes = null!;

        Assert.Equal("axes", Assert.Throws<ArgumentNullException>(() => axes.Triplot([0.0], [0.0])).ParamName);
    }
}
