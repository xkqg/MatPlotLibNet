// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>The rule "the triangles the caller gave win; otherwise the Delaunay triangulation of the points" has ONE owner,
/// <c>Delaunay.TrianglesOf</c>, which the triangle renderer and the <c>Triplot</c> verb both ask, so the two cannot disagree
/// about which triangles a point set has.</summary>
public class DelaunayTrianglesOfTests
{
    /// <summary>The corners of a square and its centre: the Delaunay triangulation is the four triangles that meet at the centre.</summary>
    private static readonly double[] SquareX = [0, 2, 2, 0, 1];

    private static readonly double[] SquareY = [0, 0, 2, 2, 1];

    [Fact]
    public void GivenTriangles_Win_AndComeBackAsTheyWere()
    {
        int[] given = [0, 1, 2];

        var triangles = Delaunay.TrianglesOf(SquareX, SquareY, given);

        Assert.Same(given, triangles);
        Assert.NotEqual(Delaunay.Triangulate(SquareX, SquareY).Triangles.Length, triangles.Length);   // the points would have given four
    }

    [Fact]
    public void AnEmptyGivenList_IsStillGiven_AndWins()
    {
        int[] given = [];

        var triangles = Delaunay.TrianglesOf(SquareX, SquareY, given);

        Assert.Same(given, triangles);
        Assert.NotEmpty(Delaunay.Triangulate(SquareX, SquareY).Triangles);
    }

    [Fact]
    public void WithoutGivenTriangles_TheyAreTheDelaunayTrianglesOfThePoints()
    {
        var triangles = Delaunay.TrianglesOf(SquareX, SquareY, null);

        Assert.Equal(Delaunay.Triangulate(SquareX, SquareY).Triangles, triangles);
        Assert.Equal(12, triangles.Length);   // four triangles of three corners
    }

    [Fact]
    public void WithoutGivenTriangles_FewerThanThreePoints_HaveNoTriangle()
    {
        Assert.Empty(Delaunay.TrianglesOf([0.0, 1.0], [0.0, 1.0], null));
        Assert.Empty(Delaunay.TrianglesOf([], [], null));
    }

    [Fact]
    public void WithoutGivenTriangles_APointThatCannotBePlaced_IsInNoTriangle()
    {
        double[] x = [0, double.NaN, 2, 2, 0, 1];
        double[] y = [0, 5, 0, 2, 2, 1];

        var triangles = Delaunay.TrianglesOf(x, y, null);

        Assert.NotEmpty(triangles);
        Assert.DoesNotContain(1, triangles);
    }
}
