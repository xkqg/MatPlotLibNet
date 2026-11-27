// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>The marks of the SVG surface (<see cref="Rendering.Svg.SvgRenderContext.SvgMarkWriter"/>): a line, a polyline and a shape over its geometry, each one
/// element of the buffer's envelope, stroked and joined by the looks of the theme.</summary>
public class SvgMarkWriterTests
{
    private static readonly StrokeLook Strokes = ThemeLooks.Library.Strokes;

    private static readonly StrokeStyle RoundRed = new StrokeStyle(Colors.Red, 2, LineStyle.Solid) { Join = JoinStyle.Round };

    /// <summary>A buffer and the mark writer over it.</summary>
    private readonly record struct MarkSurface(SvgElementBuffer Buffer, SvgRenderContext.SvgMarkWriter Marks);

    private static MarkSurface Surface()
    {
        var buffer = new SvgElementBuffer(default);
        return new MarkSurface(buffer, new SvgRenderContext.SvgMarkWriter(buffer));
    }

    [Fact]
    public void ALine_IsOneElement_WithNoJoin()
    {
        var (buffer, marks) = Surface();

        marks.Line(new Point(0, 1), new Point(10, 5), RoundRed, Strokes);

        Assert.Equal("<line x1=\"0\" y1=\"1\" x2=\"10\" y2=\"5\" stroke=\"#FF0000\" stroke-width=\"2\" />\n", buffer.Markup.ToString());
    }

    [Fact]
    public void APolylineOfThreePoints_IsJoinedAsTheLooksJoinIt()
    {
        var (buffer, marks) = Surface();

        marks.Polyline([new Point(0, 0), new Point(5, 5), new Point(10, 0)], RoundRed, Strokes);

        Assert.Equal("<polyline points=\"0,0 5,5 10,0\" fill=\"none\" stroke=\"#FF0000\" stroke-width=\"2\" stroke-linejoin=\"round\" />\n",
            buffer.Markup.ToString());
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("APolylineOfTwoPoints_HasNoCornerToJoin", () =>
        {
            var (buffer, marks) = Surface();
            marks.Polyline([new Point(0, 0), new Point(5, 5)], RoundRed, Strokes);
            return buffer.Markup.ToString();
        }) { Absent = ["stroke-linejoin"] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    [Fact]
    public void APointThatCannotBePlaced_IsLeftOutOfThePolyline()
    {
        var (buffer, marks) = Surface();

        marks.Polyline([new Point(0, 0), new Point(double.NaN, 1), new Point(2, double.NaN), new Point(4, 4)], RoundRed, Strokes);

        Assert.StartsWith("<polyline points=\"0,0 4,4\"", buffer.Markup.ToString());
    }

    [Fact]
    public void APolylineOfOnePoint_WritesNothing_AndUsesNothingAskedForTheNextElement()
    {
        var (buffer, marks) = Surface();
        buffer.SetNextData("key", "v");

        marks.Polyline([new Point(0, 0)], RoundRed, Strokes);
        marks.Line(new Point(0, 0), new Point(1, 1), RoundRed, Strokes);

        Assert.StartsWith("<line ", buffer.Markup.ToString());
        Assert.Contains(" data-key=\"v\"", buffer.Markup.ToString());
    }

    [Theory]
    [InlineData("circle", false)]
    [InlineData("ellipse", false)]
    [InlineData("rect", true)]
    [InlineData("polygon", true)]
    [InlineData("path", true)]
    public void AShape_IsJoinedOnlyWhenItHasCorners(string tag, bool joined)
    {
        var (buffer, marks) = Surface();
        var shape = new ShapeStyle(Colors.Blue, Colors.Red, 1) { StrokeJoin = JoinStyle.Round };
        var box = new Rect(0, 0, 10, 10);

        switch (tag)
        {
            case "circle": marks.Shape(new CircleGeometry(new Point(5, 5), 5), shape, Strokes); break;
            case "ellipse": marks.Shape(new EllipseGeometry(box), shape, Strokes); break;
            case "rect": marks.Shape(new RectGeometry(box), shape, Strokes); break;
            case "polygon": marks.Shape(new PolygonGeometry([new Point(0, 0), new Point(10, 0), new Point(5, 5)]), shape, Strokes); break;
            default: marks.Shape(new PathGeometry([new MoveToSegment(new Point(0, 0)), new LineToSegment(new Point(10, 0))]), shape, Strokes); break;
        }

        string markup = buffer.Markup.ToString();
        Assert.StartsWith("<" + tag + " ", markup);
        Assert.Equal(joined, markup.Contains("stroke-linejoin=\"round\"", StringComparison.Ordinal));
    }

    [Fact]
    public void AnEllipse_StandsInItsBounds()
    {
        var (buffer, marks) = Surface();

        marks.Shape(new EllipseGeometry(new Rect(10, 20, 30, 40)), ShapeStyle.Filled(Colors.Red), Strokes);

        Assert.StartsWith("<ellipse cx=\"25\" cy=\"40\" rx=\"15\" ry=\"20\" fill=\"#FF0000\"", buffer.Markup.ToString());
    }

    [Fact]
    public void AShapeWithAGradientAndAHatch_IsTwoLayers_TheGradientUnderTheHatch()
    {
        var (buffer, marks) = Surface();
        var shape = ShapeStyle.Filled(Colors.Red) with
        {
            Gradient = new LinearGradient(new(0, 0), new(10, 0), Colors.Red, Colors.Blue),
            Hatch = HatchPattern.Cross,
        };

        marks.Shape(new RectGeometry(new Rect(0, 0, 10, 10)), shape, Strokes);

        string markup = buffer.Markup.ToString();
        Assert.Equal(2, markup.Split("<rect x=").Length - 1);
        Assert.True(markup.IndexOf("url(#grad-0)", StringComparison.Ordinal) < markup.IndexOf("url(#hatch-0)", StringComparison.Ordinal));
    }
}
