// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;
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

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("DrawPath_UsesPolymorphicToSvgPathData", () =>
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
            ctx.DrawPath(segments, new ShapeStyle(Colors.Red, null, 0));
            return ctx.GetOutput();
        }, "M 0 0", "L 100 0", "Z"),
        new DrawnToken("DrawPath_WritesASegmentTheLibraryDoesNotKnow_ThroughItsOwnToSvgPathData", () =>
        {
            var ctx = new SvgRenderContext();
            ctx.DrawPath([new MoveToSegment(new Point(1, 2)), new HorizontalToSegment(7.5), new CloseSegment()], new ShapeStyle(Colors.Red, null, 0));
            return ctx.GetOutput();
        }, "<path d=\"M 1 2 H 7.5 Z \""),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    // --- One formatting rule: the segment writes its own path data into the document's builder ---

    public static TheoryData<PathSegment, string> Leaves => new()
    {
        { new MoveToSegment(new Point(10, 20)), "M 10 20 " },
        { new MoveToSegment(new Point(-1.5, 0.25)), "M -1.5 0.25 " },
        { new MoveToSegment(new Point(1e20, double.NaN)), "M 1E+20 NaN " },
        { new LineToSegment(new Point(0.1 + 0.2, -0.0)), "L 0.3 0 " },
        { new BezierSegment(new Point(1, 2), new Point(3.5, 4), new Point(5, -6.125)), "C 1 2 3.5 4 5 -6.13 " },
        { new CloseSegment(), "Z " },
        // A quarter turn clockwise from 3 o'clock ends straight below the centre; the sweep flag is 1 and the arc is small.
        { new ArcSegment(new Point(50, 50), 25, 25, 0, 90), "A 25 25 0 0 1 50 75 " },
        // More than half a turn sets the large-arc flag.
        { new ArcSegment(new Point(100, 100), 10, 20, 0, 270), "A 10 20 0 1 1 100 80 " },
        // The inner edge of a wedge runs back: the sweep flag is 0.
        { new ArcSegment(new Point(0, 0), 10, 10, 90, 0), "A 10 10 0 0 0 10 0 " },
    };

    [Theory]
    [MemberData(nameof(Leaves))]
    public void AppendSvgPathData_WritesTheBytesToSvgPathDataReturns(PathSegment segment, string expected)
    {
        var sb = new StringBuilder("d:");
        segment.AppendSvgPathData(sb);

        Assert.Equal("d:" + expected, sb.ToString());
        Assert.Equal(expected, segment.ToSvgPathData());
    }

    [Fact]
    public void AppendSvgPathData_OfASegmentTheLibraryDoesNotKnow_AppendsItsToSvgPathData()
    {
        var sb = new StringBuilder();
        new HorizontalToSegment(7.5).AppendSvgPathData(sb);

        Assert.Equal("H 7.5 ", sb.ToString());
    }

    [Fact]
    public void AppendSvgPath_WritesEverySegmentInOrder()
    {
        var sb = new StringBuilder();
        sb.AppendSvgPath([new MoveToSegment(new Point(0, 0)), new LineToSegment(new Point(100, 0.5)), new CloseSegment()]);

        Assert.Equal("M 0 0 L 100 0.5 Z ", sb.ToString());
    }

    /// <summary>A segment a user derives: the library only knows its <c>ToSvgPathData</c>.</summary>
    private sealed record HorizontalToSegment(double X) : PathSegment
    {
        public override string ToSvgPathData() => $"H {F(X)} ";
    }
}
