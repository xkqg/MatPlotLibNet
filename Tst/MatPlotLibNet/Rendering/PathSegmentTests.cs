// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies polymorphic <see cref="PathSegment.ToSvgPathData"/> implementations.</summary>
public class PathSegmentTests
{
    [Fact]
    public void MoveToSegment_ToSvgPathData_ReturnsM()
    {
        var seg = new MoveToSegment(new Point(10, 20));
        Assert.StartsWith("M ", seg.ToSvgPathData());
        Assert.Contains("10", seg.ToSvgPathData());
        Assert.Contains("20", seg.ToSvgPathData());
    }

    [Fact]
    public void LineToSegment_ToSvgPathData_ReturnsL()
    {
        var seg = new LineToSegment(new Point(30, 40));
        Assert.StartsWith("L ", seg.ToSvgPathData());
    }

    [Fact]
    public void BezierSegment_ToSvgPathData_ReturnsC()
    {
        var seg = new BezierSegment(new Point(1, 2), new Point(3, 4), new Point(5, 6));
        Assert.StartsWith("C ", seg.ToSvgPathData());
    }

    [Fact]
    public void ArcSegment_ToSvgPathData_ReturnsA()
    {
        var seg = new ArcSegment(new Point(50, 50), 25, 25, 0, 90);
        Assert.StartsWith("A ", seg.ToSvgPathData());
    }

    [Fact]
    public void CloseSegment_ToSvgPathData_ReturnsZ()
    {
        var seg = new CloseSegment();
        Assert.Equal("Z ", seg.ToSvgPathData());
    }

    [Fact]
    public void DrawPath_UsesPolymorphicToSvgPathData()
    {
        // A path with M, L, Z should produce valid SVG path data
        var segments = new PathSegment[]
        {
            new MoveToSegment(new Point(0, 0)),
            new LineToSegment(new Point(100, 0)),
            new LineToSegment(new Point(100, 100)),
            new CloseSegment()
        };

        var ctx = new SvgRenderContext();
        ctx.DrawPath(segments, Colors.Red, null, 0);
        string svg = ctx.GetOutput();

        Assert.Contains("M 0 0", svg);
        Assert.Contains("L 100 0", svg);
        Assert.Contains("Z", svg);
    }
}
