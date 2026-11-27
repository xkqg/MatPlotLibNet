// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A closed outline drawn with a stroke, in one place (<see cref="ClosedStrokeExtensions"/>): a solid one is the shape (a circle,
/// a polygon), whose outline takes no dash; a dotted or dashed one is a closed polyline through the line drawing that dashes, a circle
/// walked as matplotlib walks the path of a ring (from theta 0, the east, counter-clockwise on the screen) in 180 segments.</summary>
public class ClosedStrokeExtensionsTests
{
    private static readonly Point Centre = new(100, 80);

    [Fact]
    public void ASolidRing_IsTheCircle()
    {
        var ctx = new RecordingRenderContext();

        ctx.DrawCircleStroke(Centre, 40, new StrokeStyle(Colors.Red, 1.5, LineStyle.Solid));

        var call = ctx.Of<DrawCircleCall>().Single();
        Assert.Equal(Centre, call.Center);
        Assert.Equal(40, call.Radius);
        Assert.Equal(Colors.Red, call.Stroke);
        Assert.Equal(1.5, call.StrokeThickness);
        Assert.Null(call.Fill);
        Assert.Equal(0, ctx.Count<DrawLinesCall>());
    }

    [Fact]
    public void ADashedRing_IsAClosedPolylineOf180Segments_FromTheEastCounterClockwise()
    {
        var ctx = new RecordingRenderContext();
        var dashed = new StrokeStyle(Colors.Red, 1.5, LineStyle.Dashed);

        ctx.DrawCircleStroke(Centre, 40, dashed);

        var call = ctx.Of<DrawLinesCall>().Single();
        var points = ((IEnumerable<Point>)call.Points).ToArray();
        Assert.Equal(181, points.Length);
        Assert.Equal(new Point(140, 80), points[0]);
        Assert.Equal(points[0], points[^1]);
        Assert.True(points[1].Y < Centre.Y, "the walk runs counter-clockwise on the screen");
        Assert.Equal(LineStyle.Dashed, call.Style);
        Assert.Equal(1.5, call.Thickness);
        Assert.Equal(0, ctx.Count<DrawCircleCall>());
    }

    [Fact]
    public void ASolidPolygonRing_IsThePolygon_AndADashedOneIsItsClosedPolyline()
    {
        var solid = new RecordingRenderContext();
        var dashed = new RecordingRenderContext();
        List<Point> ring = [new(0, 0), new(10, 0), new(5, 8)];

        solid.DrawPolygonStroke([.. ring], new StrokeStyle(Colors.Blue, 2, LineStyle.Solid));
        dashed.DrawPolygonStroke([.. ring], new StrokeStyle(Colors.Blue, 2, LineStyle.Dotted));

        Assert.Equal(ShapeStyle.Outlined(Colors.Blue, 2), solid.Of<DrawPolygonCall>().Single().Shape);
        var points = ((IEnumerable<Point>)dashed.Of<DrawLinesCall>().Single().Points).ToArray();
        Assert.Equal([.. ring, ring[0]], points);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // A solid ring of a polar grid is the circle its spine is, drawn by the one primitive: the grid of a polar axes draws no ellipse.
        // matplotlib 3.11.2 (printed): the polar spine's path and a solid ring's path through PolarTransform are both a MOVETO and Bezier
        // arcs (codes 1 and 4), 49 vertices each.
        new DrawnToken("ASolidRingOfAPolarGrid_IsDrawnAsTheCircleItsSpineIs", () => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.PolarPlot([1.0, 2, 3], [0.0, 1, 2]).WithGrid(g => g with { Visible = true }))
            .Build().ToSvg(), "<circle") { Absent = ["<ellipse"] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
