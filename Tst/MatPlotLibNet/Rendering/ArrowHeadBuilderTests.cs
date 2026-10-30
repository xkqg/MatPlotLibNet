// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies <see cref="ArrowHeadBuilder"/> polygon and path output for each arrow style.</summary>
public class ArrowHeadBuilderTests
{
    private static (double ux, double uy) Rightward() => (1.0, 0.0); // unit vector pointing right

    /// <summary>A head is asked for by the two ends of the line it finishes, the one way an annotation, a graph edge and an arc
    /// ask: the unit direction is worked out here, once, instead of by each caller.</summary>
    [Fact]
    public void AHeadAskedForByItsTwoEnds_IsTheHeadOfTheUnitDirectionFromTheFirstToTheSecond()
    {
        var from = new Point(10, 20);
        var tip = new Point(13, 24);   // 3 across, 4 down: length 5, direction (0.6, 0.8)

        var byEnds = ArrowHeadBuilder.BuildPolygon(from, tip, ArrowStyle.FancyArrow, 8);
        var byDirection = ArrowHeadBuilder.BuildPolygon(tip, 0.6, 0.8, ArrowStyle.FancyArrow, 8);

        Assert.Equal(byDirection.Count, byEnds.Count);
        for (int i = 0; i < byEnds.Count; i++)
        {
            Assert.Equal(byDirection[i].X, byEnds[i].X, 9);
            Assert.Equal(byDirection[i].Y, byEnds[i].Y, 9);
        }
    }

    [Fact]
    public void TheHeadAskedForByItsTwoEnds_StandsBehindTheTip_NotAheadOfIt()
    {
        var polygon = ArrowHeadBuilder.BuildPolygon(new Point(0, 0), new Point(100, 0), ArrowStyle.FancyArrow, 8);

        Assert.Equal(new Point(100, 0), polygon[0]);
        Assert.All(polygon.Skip(1), vertex => Assert.True(vertex.X < 100));
    }

    [Fact]
    public void APathHeadAskedForByItsTwoEnds_IsThePathHeadOfTheUnitDirection()
    {
        var byEnds = ArrowHeadBuilder.BuildPath(new Point(0, 0), new Point(0, 50), ArrowStyle.CurveB, 8);
        var byDirection = ArrowHeadBuilder.BuildPath(new Point(0, 50), 0, 1, ArrowStyle.CurveB, 8);

        Assert.NotNull(byEnds);
        Assert.Equal(byDirection!.Count, byEnds!.Count);
        Assert.Equal(byDirection.Select(s => s.ToSvgPathData()), byEnds.Select(s => s.ToSvgPathData()));
    }

    [Fact]
    public void AnArrowOfNoLength_HasNoHead()
    {
        var spot = new Point(5, 5);

        Assert.Empty(ArrowHeadBuilder.BuildPolygon(spot, spot, ArrowStyle.FancyArrow, 8));
        Assert.Null(ArrowHeadBuilder.BuildPath(spot, spot, ArrowStyle.CurveB, 8));
    }

    /// <summary>Verifies that FancyArrow returns a 3-point triangle polygon.</summary>
    [Fact]
    public void FancyArrow_ReturnsTrianglePolygon()
    {
        var tip = new Point(100, 50);
        var (ux, uy) = Rightward();

        var polygon = ArrowHeadBuilder.BuildPolygon(tip, ux, uy, ArrowStyle.FancyArrow);

        Assert.Equal(3, polygon.Count);
        Assert.Equal(tip, polygon[0]); // tip is first vertex
    }

    /// <summary>Verifies that Wedge returns a wider polygon than FancyArrow (same tip, wider base).</summary>
    [Fact]
    public void Wedge_ReturnsWiderPolygon()
    {
        var tip = new Point(100, 50);
        var (ux, uy) = Rightward();

        var fancy  = ArrowHeadBuilder.BuildPolygon(tip, ux, uy, ArrowStyle.FancyArrow);
        var wedge  = ArrowHeadBuilder.BuildPolygon(tip, ux, uy, ArrowStyle.Wedge);

        Assert.True(wedge.Count >= 3, "Wedge should have at least 3 vertices");
        // Wedge base should be wider: max perpendicular distance of left/right from tip > FancyArrow's
        double fancyWidth = Math.Abs(fancy[1].Y - fancy[2].Y);
        double wedgeWidth = Math.Abs(wedge[1].Y - wedge[2].Y);
        Assert.True(wedgeWidth > fancyWidth, $"Wedge ({wedgeWidth}) should be wider than FancyArrow ({fancyWidth})");
    }

    /// <summary>Verifies that Simple returns an empty polygon (line only, no head drawn).</summary>
    [Fact]
    public void Simple_ReturnsEmptyPoints()
    {
        var tip = new Point(100, 50);
        var (ux, uy) = Rightward();

        var polygon = ArrowHeadBuilder.BuildPolygon(tip, ux, uy, ArrowStyle.Simple);

        Assert.Empty(polygon);
    }

    /// <summary>Verifies that BracketA (source end) returns path segments for the bracket line.</summary>
    [Fact]
    public void BracketA_ReturnsBracketSegments()
    {
        var tip = new Point(100, 50);
        var (ux, uy) = Rightward();

        var path = ArrowHeadBuilder.BuildPath(tip, ux, uy, ArrowStyle.BracketA);

        Assert.NotNull(path);
        Assert.True(path!.Count >= 2, "BracketA should produce at least a MoveTo + LineTo");
    }

    /// <summary>Verifies that CurveAB returns path segments for both ends of the connection.</summary>
    [Fact]
    public void CurveAB_ReturnsTwoHeads()
    {
        var tip = new Point(100, 50);
        var (ux, uy) = Rightward();

        var path = ArrowHeadBuilder.BuildPath(tip, ux, uy, ArrowStyle.CurveAB);

        Assert.NotNull(path);
        Assert.True(path!.Count >= 4, "CurveAB should produce path segments for both ends");
    }

    /// <summary>Verifies that None returns an empty polygon.</summary>
    [Fact]
    public void None_ReturnsEmpty()
    {
        var tip = new Point(100, 50);
        var (ux, uy) = Rightward();

        var polygon = ArrowHeadBuilder.BuildPolygon(tip, ux, uy, ArrowStyle.None);

        Assert.Empty(polygon);
    }
}
