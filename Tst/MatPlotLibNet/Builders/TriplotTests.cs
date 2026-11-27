// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
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
        TestFigures.OfOneVerb(ax => ax.Triplot(x, y, triangles, configure));

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
        for (int i = 0; i + 1 < line.XData.Length; i += 3)
        {
            Assert.True(i + 2 == line.XData.Length || (double.IsNaN(line.XData[i + 2]) && double.IsNaN(line.YData[i + 2])),
                "a segment is followed by a missing point, unless it is the last");
            segments.Add(new Edge(line.XData[i], line.YData[i], line.XData[i + 1], line.YData[i + 1]));
        }

        return segments;
    }

    // ---- the edges ------------------------------------------------------------------------------------------------------

    [Fact]
    public void TheChart_IsOneLineSeries_OfTheEdges_EachWithItsEnds_PartedByAGap()
    {
        var line = Of(SquareX, SquareY, SquareTriangles).OnlySeries<LineSeries>();

        // Eight edges in the order they are first met: the three of the first triangle, then the two new ones of each next.
        Assert.Equal([0.0, 2.0, Gap, 2.0, 1.0, Gap, 1.0, 0.0, Gap, 2.0, 2.0, Gap, 2.0, 1.0, Gap, 2.0, 0.0, Gap, 0.0, 1.0, Gap, 0.0, 0.0], line.XData);
        Assert.Equal([0.0, 0.0, Gap, 0.0, 1.0, Gap, 1.0, 0.0, Gap, 0.0, 2.0, Gap, 2.0, 1.0, Gap, 2.0, 2.0, Gap, 2.0, 1.0, Gap, 2.0, 0.0], line.YData);
    }

    [Fact]
    public void FivePoints_ThatTriangulateToEightEdges_AreEightSegments()
    {
        var figure = Of(SquareX, SquareY);   // four triangles, twelve sides, eight edges: each spoke is shared by two triangles

        Assert.Equal(4, Delaunay.Triangulate(SquareX, SquareY).Triangles.Length / 3);
        Assert.Equal(8, SegmentsOf(figure.OnlySeries<LineSeries>()).Count);
        Assert.Equal(8, figure.Drawn().Polylines(SeriesColour, Theme.MatplotlibV2.Looks.Lines.Width).Count);
    }

    [Fact]
    public void AnEdgeTwoTrianglesShare_IsDrawnOnce_WhicheverWayEachRunsIt()
    {
        // The triangles (0, 1, 2) and (2, 1, 3) share the edge between points 1 and 2, the first running it 1 -> 2 and the
        // second 2 -> 1: six sides, five edges.
        double[] x = [0, 2, 1, 3];
        double[] y = [0, 0, 2, 2];

        var segments = SegmentsOf(Of(x, y, [0, 1, 2, 2, 1, 3]).OnlySeries<LineSeries>());

        Assert.Equal(5, segments.Count);
        Assert.Equal(5, segments.Select(edge => edge.Unordered()).Distinct().Count());
    }

    [Fact]
    public void AnEdge_IsDrawnWhereTheVerticesAre_InTheOrderTheTrianglesNameThem()
    {
        var segments = SegmentsOf(Of([0, 4, 0], [0, 0, 3], [0, 1, 2]).OnlySeries<LineSeries>());

        Assert.Equal([new Edge(0, 0, 4, 0), new Edge(4, 0, 0, 3), new Edge(0, 3, 0, 0)], segments);
    }

    [Fact]
    public void WithoutTriangles_TheEdgesAreThoseOfTheDelaunayTriangulation_ThatTripcolorFills()
    {
        var byThePoints = Of(SquareX, SquareY).OnlySeries<LineSeries>();
        var byTheMesh = Of(SquareX, SquareY, Delaunay.Triangulate(SquareX, SquareY).Triangles).OnlySeries<LineSeries>();

        Assert.Equal(byTheMesh.XData, byThePoints.XData);
        Assert.Equal(byTheMesh.YData, byThePoints.YData);
    }

    [Fact]
    public void GivenTriangles_Win_OverTheTriangulationOfThePoints()
    {
        var segments = SegmentsOf(Of(SquareX, SquareY, [0, 1, 2]).OnlySeries<LineSeries>());

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

        var line = Of(x, y).OnlySeries<LineSeries>();

        Assert.All(line.XData.Where(double.IsFinite), value => Assert.Contains(value, x));
        Assert.All(line.YData.Where(double.IsFinite), value => Assert.Contains(value, y));
    }

    [Fact]
    public void ADegenerateTriangle_ThatNamesOnePointTwice_DrawsNoEdgeFromThatPointToItself()
    {
        var segments = SegmentsOf(Of([0, 4, 0], [0, 0, 3], [0, 0, 1]).OnlySeries<LineSeries>());

        Assert.Equal([new Edge(0, 0, 4, 0)], segments);
    }

    [Fact]
    public void ASingleEdge_IsOneSegment_NotBroadcastIntoMore()
    {
        // One edge is arrays of one value in the segment path, which a path reads as "the same for every segment": still one.
        var line = Of([0, 4, 0], [0, 0, 3], [0, 1, 1]).OnlySeries<LineSeries>();

        Assert.Equal([0.0, 4.0], line.XData);
        Assert.Equal([0.0, 0.0], line.YData);
    }

    [Fact]
    public void TheSvg_HoldsOnePolylineForEachEdge()
    {
        string svg = Of(SquareX, SquareY, SquareTriangles).ToSvg();

        Assert.Equal(8, svg.Elements("polyline").Count);
        svg.AssertNoNonFiniteNumber();
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

        Assert.Empty(figure.OnlySeries<LineSeries>().XData);
        Assert.Empty(figure.OnlySeries<LineSeries>().YData);
        Assert.NotEmpty(figure.ToSvg());
    }

    [Fact]
    public void FewerThanThreePoints_DrawNothingEvenWithGivenTriangles_AsTheTrianglePlotDrawsNothing()
    {
        // Two points cannot make a triangle: a list that names one corner twice is degenerate and draws no edge here either.
        var figure = Of([0.0, 1.0], [0.0, 1.0], [0, 1, 1]);

        Assert.Empty(figure.OnlySeries<LineSeries>().XData);
        Assert.NotEmpty(figure.ToSvg());
    }

    [Fact]
    public void NoTriangles_AddOneEmptyLineSeries_AndDoNotBreakTheFigure()
    {
        var figure = Of(SquareX, SquareY, []);

        Assert.Empty(figure.OnlySeries<LineSeries>().XData);
        Assert.NotEmpty(figure.ToSvg());
    }

    // ---- a vertex that cannot be placed -------------------------------------------------------------------------------

    [Fact]
    public void AVertexWithAMissingCoordinate_DropsItsEdgesWhole_AndTheOthersStayParted()
    {
        double[] x = [0, 2, double.NaN, 0, 1];   // the corner the first two triangles and the last two meet at

        var line = Of(x, SquareY, SquareTriangles).OnlySeries<LineSeries>();

        // The eight edges are the four sides of the square and the four spokes; point 2 is on two sides and one spoke.
        var segments = SegmentsOf(line);
        Assert.Equal(5, segments.Count);
        Assert.All(segments, s => new[] { s.X0, s.X1, s.Y0, s.Y1 }.AssertAllFinite());
        Assert.Equal(5, Of(x, SquareY, SquareTriangles).Drawn().Polylines(SeriesColour, Theme.MatplotlibV2.Looks.Lines.Width).Count);
    }

    [Fact]
    public void AnInfiniteCoordinate_IsNoVertexEither()
    {
        double[] y = [0, 0, 2, double.PositiveInfinity, 1];

        var segments = SegmentsOf(Of(SquareX, y, SquareTriangles).OnlySeries<LineSeries>());

        Assert.Equal(5, segments.Count);
    }

    [Fact]
    public void WithoutTriangles_APointThatCannotBePlaced_IsNoVertex_AndTheOthersAreTriangulatedWithoutIt()
    {
        double[] x = [0, 2, 2, 0, 1, double.NaN];
        double[] y = [0, 0, 2, 2, 1, 5];

        var line = Of(x, y).OnlySeries<LineSeries>();

        Assert.Equal(8, SegmentsOf(line).Count);
        Assert.DoesNotContain(5.0, line.YData);
    }

    // ---- what the verb refuses ----------------------------------------------------------------------------------------

    [Theory]
    [InlineData(5)]
    [InlineData(100)]
    [InlineData(-1)]
    public void ATriangleIndex_OutsideThePoints_IsRefusedByTheNameOfTheTriangles(int index)
    {
        var refused = Assert.Throws<ArgumentOutOfRangeException>(() => Of(SquareX, SquareY, [0, 1, 4, 1, 2, index]));

        Assert.Equal("triangles", refused.ParamName);
        Assert.Contains("triangles[5]", refused.Message, StringComparison.Ordinal);
        Assert.Contains(index.ToString(System.Globalization.CultureInfo.InvariantCulture), refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ATriangleIndex_IsRefusedWhenThereAreFewerThanThreePointsToo()
    {
        Assert.Equal("triangles", Assert.Throws<ArgumentOutOfRangeException>(() => Of([0.0, 1.0], [0.0, 1.0], [0, 1, 2])).ParamName);
    }

    [Fact]
    public void ATriangle_WhenThereAreNoPointsAtAll_IsRefusedAndTheMessageSaysSo()
    {
        var refused = Assert.Throws<ArgumentOutOfRangeException>(() => Of([], [], [0, 1, 2]));

        Assert.Equal("triangles", refused.ParamName);
        Assert.Contains("and there is none", refused.Message, StringComparison.Ordinal);
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
    public void ANullArgument_IsRefusedByName()
    {
        Assert.Equal("x", Assert.Throws<ArgumentNullException>(() => Of(null!, [0.0])).ParamName);
        Assert.Equal("y", Assert.Throws<ArgumentNullException>(() => Of([0.0], null!)).ParamName);
    }
}
