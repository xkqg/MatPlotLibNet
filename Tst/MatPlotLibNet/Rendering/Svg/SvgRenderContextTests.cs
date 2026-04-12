// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>Verifies <see cref="SvgRenderContext"/> behavior.</summary>
public class SvgRenderContextTests
{
    /// <summary>Verifies that DrawLine produces an SVG line element with correct attributes and color.</summary>
    [Fact]
    public void DrawLine_ProducesSvgLineElement()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawLine(new Point(10, 20), new Point(30, 40),
            Colors.Red, 2.0, LineStyle.Solid);

        string svg = ctx.GetOutput();
        Assert.Contains("<line", svg);
        Assert.Contains("x1=", svg);
        Assert.Contains("x2=", svg);
        Assert.Contains("#FF0000", svg);
    }

    /// <summary>Verifies that DrawRectangle produces an SVG rect element with the specified fill color.</summary>
    [Fact]
    public void DrawRectangle_ProducesSvgRectElement()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawRectangle(new Rect(10, 20, 100, 50),
            fill: Colors.Blue, stroke: null, strokeThickness: 0);

        string svg = ctx.GetOutput();
        Assert.Contains("<rect", svg);
        Assert.Contains("#0000FF", svg);
    }

    /// <summary>Verifies that DrawText produces an SVG text element containing the specified string.</summary>
    [Fact]
    public void DrawText_ProducesSvgTextElement()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawText("Hello", new Point(50, 50), new Font(), TextAlignment.Center);

        string svg = ctx.GetOutput();
        Assert.Contains("<text", svg);
        Assert.Contains("Hello", svg);
    }

    /// <summary>Verifies that DrawCircle produces an SVG circle element.</summary>
    [Fact]
    public void DrawCircle_ProducesSvgCircleElement()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawCircle(new Point(50, 50), 10, Colors.Green, null, 0);

        string svg = ctx.GetOutput();
        Assert.Contains("<circle", svg);
    }

    /// <summary>Verifies that DrawLines produces an SVG polyline element from multiple points.</summary>
    [Fact]
    public void DrawLines_ProducesSvgPolylineElement()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawLines([new Point(0, 0), new Point(10, 10), new Point(20, 5)],
            Colors.Blue, 1.5, LineStyle.Solid);

        string svg = ctx.GetOutput();
        Assert.Contains("<polyline", svg);
    }

    /// <summary>Verifies that a dashed line includes a stroke-dasharray attribute.</summary>
    [Fact]
    public void DashedLine_HasStrokeDasharray()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawLine(new Point(0, 0), new Point(10, 10),
            Colors.Black, 1, LineStyle.Dashed);

        string svg = ctx.GetOutput();
        Assert.Contains("stroke-dasharray", svg);
    }

    // --- DrawPolygon ---

    /// <summary>Verifies that DrawPolygon with a fill color produces a polygon element.</summary>
    [Fact]
    public void DrawPolygon_WithFill_ContainsPolygonElement()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawPolygon([new Point(0, 0), new Point(10, 0), new Point(5, 10)],
            fill: Colors.Red, stroke: null, strokeThickness: 0);

        string svg = ctx.GetOutput();
        Assert.Contains("<polygon", svg);
    }

    /// <summary>Verifies that DrawPolygon with a stroke color includes a stroke attribute.</summary>
    [Fact]
    public void DrawPolygon_WithStroke_HasStrokeAttribute()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawPolygon([new Point(0, 0), new Point(10, 0), new Point(5, 10)],
            fill: null, stroke: Colors.Blue, strokeThickness: 2);

        string svg = ctx.GetOutput();
        Assert.Contains("stroke=", svg);
    }

    /// <summary>Verifies that DrawPolygon with both fill and stroke includes both attributes.</summary>
    [Fact]
    public void DrawPolygon_WithBothFillAndStroke()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawPolygon([new Point(0, 0), new Point(10, 0), new Point(5, 10)],
            fill: Colors.Green, stroke: Colors.Black, strokeThickness: 1);

        string svg = ctx.GetOutput();
        Assert.Contains("fill=", svg);
        Assert.Contains("stroke=", svg);
    }

    // --- DrawEllipse ---

    /// <summary>Verifies that DrawEllipse produces an SVG ellipse element with cx, cy, rx, and ry attributes.</summary>
    [Fact]
    public void DrawEllipse_ContainsEllipseElement()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawEllipse(new Rect(10, 20, 100, 50),
            fill: Colors.Red, stroke: null, strokeThickness: 0);

        string svg = ctx.GetOutput();
        Assert.Contains("<ellipse", svg);
        Assert.Contains("cx=", svg);
        Assert.Contains("cy=", svg);
        Assert.Contains("rx=", svg);
        Assert.Contains("ry=", svg);
    }

    /// <summary>Verifies that DrawEllipse with a fill color includes the hex color value.</summary>
    [Fact]
    public void DrawEllipse_WithFill_HasFillColor()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawEllipse(new Rect(0, 0, 80, 40),
            fill: Colors.Blue, stroke: null, strokeThickness: 0);

        string svg = ctx.GetOutput();
        Assert.Contains("#0000FF", svg);
    }

    // --- DrawPath ---

    /// <summary>Verifies that DrawPath with MoveTo and LineTo segments produces M and L commands in the path data.</summary>
    [Fact]
    public void DrawPath_MoveToLineTo_ContainsMAndLCommands()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawPath(
            [new MoveToSegment(new Point(0, 0)), new LineToSegment(new Point(10, 20))],
            fill: null, stroke: Colors.Black, strokeThickness: 1);

        string svg = ctx.GetOutput();
        Assert.Contains("<path d=\"", svg);
        Assert.Contains("M ", svg);
        Assert.Contains("L ", svg);
    }

    /// <summary>Verifies that DrawPath with a BezierSegment produces a C command in the path data.</summary>
    [Fact]
    public void DrawPath_BezierSegment_ContainsCCommand()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawPath(
            [new MoveToSegment(new Point(0, 0)),
             new BezierSegment(new Point(5, 10), new Point(15, 10), new Point(20, 0))],
            fill: null, stroke: Colors.Black, strokeThickness: 1);

        string svg = ctx.GetOutput();
        Assert.Contains("C ", svg);
    }

    /// <summary>Verifies that DrawPath with a CloseSegment produces a Z command to close the path.</summary>
    [Fact]
    public void DrawPath_CloseSegment_ContainsZCommand()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawPath(
            [new MoveToSegment(new Point(0, 0)),
             new LineToSegment(new Point(10, 0)),
             new LineToSegment(new Point(5, 10)),
             new CloseSegment()],
            fill: Colors.Red, stroke: null, strokeThickness: 0);

        string svg = ctx.GetOutput();
        Assert.Contains("Z", svg);
    }

    // --- Clipping ---

    /// <summary>Verifies that PushClip creates a clipPath definition in the SVG output.</summary>
    [Fact]
    public void PushClip_CreatesClipPathDef()
    {
        var ctx = new SvgRenderContext();
        ctx.PushClip(new Rect(0, 0, 100, 100));

        string svg = ctx.GetOutput();
        Assert.Contains("<clipPath", svg);
    }

    /// <summary>Verifies that PushClip opens a group element referencing the clip path URL.</summary>
    [Fact]
    public void PushClip_OpensGroupWithClipPathUrl()
    {
        var ctx = new SvgRenderContext();
        ctx.PushClip(new Rect(0, 0, 100, 100));

        string svg = ctx.GetOutput();
        Assert.Contains("clip-path=\"url(#clip-", svg);
    }

    /// <summary>Verifies that PopClip closes the clipping group element.</summary>
    [Fact]
    public void PopClip_ClosesGroup()
    {
        var ctx = new SvgRenderContext();
        ctx.PushClip(new Rect(0, 0, 100, 100));
        ctx.PopClip();

        string svg = ctx.GetOutput();
        Assert.Contains("</g>", svg);
    }

    // --- Opacity ---

    /// <summary>Verifies that SetOpacity creates a group element with an opacity attribute.</summary>
    [Fact]
    public void SetOpacity_CreatesGroupWithOpacityAttribute()
    {
        var ctx = new SvgRenderContext();
        ctx.SetOpacity(0.5);

        string svg = ctx.GetOutput();
        Assert.Contains("<g opacity=\"", svg);
    }

    // --- MeasureText ---

    /// <summary>Verifies that MeasureText returns a non-zero width and height for non-empty text.</summary>
    [Fact]
    public void MeasureText_ReturnsNonZeroSize()
    {
        var ctx = new SvgRenderContext();
        var size = ctx.MeasureText("Hello", new Font());

        Assert.True(size.Width > 0);
        Assert.True(size.Height > 0);
    }

    /// <summary>Verifies that MeasureText returns zero width for an empty string.</summary>
    [Fact]
    public void MeasureText_EmptyString_ReturnsZeroWidth()
    {
        var ctx = new SvgRenderContext();
        var size = ctx.MeasureText("", new Font());

        Assert.Equal(0, size.Width);
    }

    // --- DrawText variants ---

    /// <summary>Verifies that center-aligned text uses the "middle" text-anchor attribute.</summary>
    [Fact]
    public void DrawText_CenterAlignment_HasMiddleAnchor()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawText("Center", new Point(50, 50), new Font(), TextAlignment.Center);

        string svg = ctx.GetOutput();
        Assert.Contains("text-anchor=\"middle\"", svg);
    }

    /// <summary>Verifies that right-aligned text uses the "end" text-anchor attribute.</summary>
    [Fact]
    public void DrawText_RightAlignment_HasEndAnchor()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawText("Right", new Point(50, 50), new Font(), TextAlignment.Right);

        string svg = ctx.GetOutput();
        Assert.Contains("text-anchor=\"end\"", svg);
    }

    /// <summary>Verifies that bold font weight produces a font-weight="bold" attribute.</summary>
    [Fact]
    public void DrawText_BoldFont_HasBoldWeight()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawText("Bold", new Point(50, 50),
            new Font { Weight = FontWeight.Bold }, TextAlignment.Left);

        string svg = ctx.GetOutput();
        Assert.Contains("font-weight=\"bold\"", svg);
    }

    /// <summary>Verifies that italic font slant produces a font-style="italic" attribute.</summary>
    [Fact]
    public void DrawText_ItalicFont_HasItalicStyle()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawText("Italic", new Point(50, 50),
            new Font { Slant = FontSlant.Italic }, TextAlignment.Left);

        string svg = ctx.GetOutput();
        Assert.Contains("font-style=\"italic\"", svg);
    }

    /// <summary>Verifies that special characters in text are properly XML-escaped in the SVG output.</summary>
    [Fact]
    public void DrawText_SpecialChars_AreEscaped()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawText("<Test&>", new Point(50, 50), new Font(), TextAlignment.Left);

        string svg = ctx.GetOutput();
        Assert.Contains("&lt;Test&amp;&gt;", svg);
    }

}
