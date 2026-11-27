// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>A flat list of triangles, three indices to a triangle: refused when it is not whole triangles or names a point there
/// is not, and drawn as its edges, each once.</summary>
public class TriangleListExtensionsTests
{
    private const double NaN = double.NaN;

    private static readonly double[] SquareX = [0, 1, 0, 1];
    private static readonly double[] SquareY = [0, 0, 1, 1];

    [Fact]
    public void WholeTrianglesOverPointsThatAreThere_Pass()
    {
        Assert.Null(Record.Exception(() => new[] { 0, 1, 2, 1, 3, 2 }.ThrowIfNotCornersOf(4)));
        Assert.Null(Record.Exception(() => Array.Empty<int>().ThrowIfNotCornersOf(0)));
    }

    [Fact]
    public void ACountThatIsNotAMultipleOfThree_IsRefusedWithHowManyItHolds()
    {
        var error = Assert.Throws<ArgumentException>(() => new[] { 0, 1 }.ThrowIfNotCornersOf(4));

        Assert.Equal("triangles", error.ParamName);
        Assert.StartsWith("triangles holds 2 indices; a triangle has three corners, so give a multiple of three.", error.Message);
    }

    [Theory]
    [InlineData(new[] { 0, 1, 5 }, 3, "A corner, triangles[2], must be from 0 to 2 (got 5).")]
    [InlineData(new[] { -1, 0, 1 }, 3, "A corner, triangles[0], must be from 0 to 2 (got -1).")]
    [InlineData(new[] { 0, 0, 0 }, 0, "A corner, triangles[0], must name an item, and there is none (got 0).")]
    public void AnIndexThatNamesNoPoint_IsRefusedByItsPlaceInTheList(int[] triangles, int points, string message)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => triangles.ThrowIfNotCornersOf(points));

        Assert.Equal("triangles", error.ParamName);
        Assert.StartsWith(message, error.Message);
    }

    [Fact]
    public void TheEdges_AreEachDrawnOnce_InTheOrderTheTrianglesFirstMeetThem()
    {
        // Two triangles of a square share the edge from point 1 to point 2, which the second one walks the other way round.
        var path = new[] { 0, 1, 2, 1, 3, 2 }.EdgesAmong(SquareX, SquareY);

        Assert.Equal([0, 1, NaN, 1, 0, NaN, 0, 0, NaN, 1, 1, NaN, 1, 0], path.X);
        Assert.Equal([0, 0, NaN, 0, 1, NaN, 1, 0, NaN, 0, 1, NaN, 1, 1], path.Y);
    }

    [Fact]
    public void ACornerNamedTwice_HasNoEdgeToItself()
    {
        var path = new[] { 0, 0, 1 }.EdgesAmong(SquareX, SquareY);

        // 0 to 0 is no edge, 0 to 1 is one, and 1 to 0 is the same edge again.
        Assert.Equal([0, 1], path.X);
        Assert.Equal([0, 0], path.Y);
    }

    [Fact]
    public void NoTriangles_HaveNoEdges()
    {
        var path = Array.Empty<int>().EdgesAmong(SquareX, SquareY);

        Assert.Empty(path.X);
        Assert.Empty(path.Y);
    }

    [Fact]
    public void ALeftOverIndexThatIsNoWholeTriangle_IsLeftOut()
    {
        var path = new[] { 0, 1, 2, 3 }.EdgesAmong(SquareX, SquareY);

        Assert.Equal(8, path.X.Length);
    }
}
