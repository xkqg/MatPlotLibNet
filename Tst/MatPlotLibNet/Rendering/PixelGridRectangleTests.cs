// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A rectangle a renderer draws with its corners on the pixel grid of a surface that has one (a bar, a box, a cell of a
/// matrix) is drawn through one member of the surface, <c>DrawSnappedRectangle</c>: the renderer never asks where the grid puts the
/// rectangle and then draws what it was told, so the two halves of the rule cannot part.</summary>
public class PixelGridRectangleTests
{
    /// <summary>A surface with a pixel grid that puts a rectangle a quarter of a pixel right and down of where it stands, and
    /// records the width of the edge it was asked about.</summary>
    private sealed class QuarterPixelGrid : RecordingRenderContext, IPixelGridRenderContext
    {
        internal List<double> EdgeThicknesses { get; } = [];
        public bool FillsCellGrids => false;
        public void FillCellGrid(ReadOnlySpan<Color> colours, int columns, Rect area) { }
        public PixelSegment OnPixelGrid(PixelSegment line, double thickness, bool projectingEnds) => line;
        public Rect OnPixelGrid(Rect area, double edgeThickness)
        {
            EdgeThicknesses.Add(edgeThickness);
            return new Rect(area.X + 0.25, area.Y + 0.25, area.Width, area.Height);
        }
        public void DrawRectangleOnPixelGrid(Rect area, ShapeStyle style) => DrawRectangle(area, style);
        public PixelSegment? MarkerOnPixelGrid(Point anchor, PixelSegment mark, double thickness) => null;
        public IReadOnlyList<Point> OnPixelGrid(IReadOnlyList<Point> line, double thickness) => line;
    }

    private static readonly Rect Bar = new(10, 20, 30, 40);

    [Fact]
    public void OnASurfaceWithAGrid_TheRectangleIsDrawnWhereTheGridPutsIt()
    {
        var ctx = new QuarterPixelGrid();
        var style = new ShapeStyle(Colors.Blue, Colors.Black, 2);

        ctx.DrawSnappedRectangle(Bar, style);

        var call = Assert.Single(ctx.Of<DrawRectangleCall>());
        Assert.Equal(new Rect(10.25, 20.25, 30, 40), call.Rect);
        Assert.Equal([2.0], ctx.EdgeThicknesses);
    }

    [Fact]
    public void AStyleWithNoVisibleEdge_AsksTheGridForARectangleWithNoEdge()
    {
        var ctx = new QuarterPixelGrid();

        ctx.DrawSnappedRectangle(Bar, ShapeStyle.Filled(Colors.Blue));

        Assert.Equal([0.0], ctx.EdgeThicknesses);
        Assert.Single(ctx.Of<DrawRectangleCall>());
    }

    [Fact]
    public void OnASurfaceWithoutAGrid_TheRectangleIsDrawnWhereItStands()
    {
        var ctx = new RecordingRenderContext();

        ctx.DrawSnappedRectangle(Bar, ShapeStyle.Filled(Colors.Blue));

        var call = Assert.Single(ctx.Of<DrawRectangleCall>());
        Assert.Equal(Bar, call.Rect);
    }

}
