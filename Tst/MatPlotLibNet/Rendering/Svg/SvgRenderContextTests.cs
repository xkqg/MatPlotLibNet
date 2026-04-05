// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Svg;

public class SvgRenderContextTests
{
    [Fact]
    public void DrawLine_ProducesSvgLineElement()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawLine(new Point(10, 20), new Point(30, 40),
            Color.Red, 2.0, LineStyle.Solid);

        string svg = ctx.GetOutput();
        Assert.Contains("<line", svg);
        Assert.Contains("x1=", svg);
        Assert.Contains("x2=", svg);
        Assert.Contains("#FF0000", svg);
    }

    [Fact]
    public void DrawRectangle_ProducesSvgRectElement()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawRectangle(new Rect(10, 20, 100, 50),
            fill: Color.Blue, stroke: null, strokeThickness: 0);

        string svg = ctx.GetOutput();
        Assert.Contains("<rect", svg);
        Assert.Contains("#0000FF", svg);
    }

    [Fact]
    public void DrawText_ProducesSvgTextElement()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawText("Hello", new Point(50, 50), new Font(), TextAlignment.Center);

        string svg = ctx.GetOutput();
        Assert.Contains("<text", svg);
        Assert.Contains("Hello", svg);
    }

    [Fact]
    public void DrawCircle_ProducesSvgCircleElement()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawCircle(new Point(50, 50), 10, Color.Green, null, 0);

        string svg = ctx.GetOutput();
        Assert.Contains("<circle", svg);
    }

    [Fact]
    public void DrawLines_ProducesSvgPolylineElement()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawLines([new Point(0, 0), new Point(10, 10), new Point(20, 5)],
            Color.Blue, 1.5, LineStyle.Solid);

        string svg = ctx.GetOutput();
        Assert.Contains("<polyline", svg);
    }

    [Fact]
    public void DashedLine_HasStrokeDasharray()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawLine(new Point(0, 0), new Point(10, 10),
            Color.Black, 1, LineStyle.Dashed);

        string svg = ctx.GetOutput();
        Assert.Contains("stroke-dasharray", svg);
    }

    // --- DrawPolygon ---

    [Fact]
    public void DrawPolygon_WithFill_ContainsPolygonElement()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawPolygon([new Point(0, 0), new Point(10, 0), new Point(5, 10)],
            fill: Color.Red, stroke: null, strokeThickness: 0);

        string svg = ctx.GetOutput();
        Assert.Contains("<polygon", svg);
    }

    [Fact]
    public void DrawPolygon_WithStroke_HasStrokeAttribute()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawPolygon([new Point(0, 0), new Point(10, 0), new Point(5, 10)],
            fill: null, stroke: Color.Blue, strokeThickness: 2);

        string svg = ctx.GetOutput();
        Assert.Contains("stroke=", svg);
    }

    [Fact]
    public void DrawPolygon_WithBothFillAndStroke()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawPolygon([new Point(0, 0), new Point(10, 0), new Point(5, 10)],
            fill: Color.Green, stroke: Color.Black, strokeThickness: 1);

        string svg = ctx.GetOutput();
        Assert.Contains("fill=", svg);
        Assert.Contains("stroke=", svg);
    }

    // --- DrawEllipse ---

    [Fact]
    public void DrawEllipse_ContainsEllipseElement()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawEllipse(new Rect(10, 20, 100, 50),
            fill: Color.Red, stroke: null, strokeThickness: 0);

        string svg = ctx.GetOutput();
        Assert.Contains("<ellipse", svg);
        Assert.Contains("cx=", svg);
        Assert.Contains("cy=", svg);
        Assert.Contains("rx=", svg);
        Assert.Contains("ry=", svg);
    }

    [Fact]
    public void DrawEllipse_WithFill_HasFillColor()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawEllipse(new Rect(0, 0, 80, 40),
            fill: Color.Blue, stroke: null, strokeThickness: 0);

        string svg = ctx.GetOutput();
        Assert.Contains("#0000FF", svg);
    }

    // --- DrawPath ---

    [Fact]
    public void DrawPath_MoveToLineTo_ContainsMAndLCommands()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawPath(
            [new MoveToSegment(new Point(0, 0)), new LineToSegment(new Point(10, 20))],
            fill: null, stroke: Color.Black, strokeThickness: 1);

        string svg = ctx.GetOutput();
        Assert.Contains("<path d=\"", svg);
        Assert.Contains("M ", svg);
        Assert.Contains("L ", svg);
    }

    [Fact]
    public void DrawPath_BezierSegment_ContainsCCommand()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawPath(
            [new MoveToSegment(new Point(0, 0)),
             new BezierSegment(new Point(5, 10), new Point(15, 10), new Point(20, 0))],
            fill: null, stroke: Color.Black, strokeThickness: 1);

        string svg = ctx.GetOutput();
        Assert.Contains("C ", svg);
    }

    [Fact]
    public void DrawPath_CloseSegment_ContainsZCommand()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawPath(
            [new MoveToSegment(new Point(0, 0)),
             new LineToSegment(new Point(10, 0)),
             new LineToSegment(new Point(5, 10)),
             new CloseSegment()],
            fill: Color.Red, stroke: null, strokeThickness: 0);

        string svg = ctx.GetOutput();
        Assert.Contains("Z", svg);
    }

    // --- Clipping ---

    [Fact]
    public void PushClip_CreatesClipPathDef()
    {
        var ctx = new SvgRenderContext();
        ctx.PushClip(new Rect(0, 0, 100, 100));

        string svg = ctx.GetOutput();
        Assert.Contains("<clipPath", svg);
    }

    [Fact]
    public void PushClip_OpensGroupWithClipPathUrl()
    {
        var ctx = new SvgRenderContext();
        ctx.PushClip(new Rect(0, 0, 100, 100));

        string svg = ctx.GetOutput();
        Assert.Contains("clip-path=\"url(#clip-", svg);
    }

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

    [Fact]
    public void SetOpacity_CreatesGroupWithOpacityAttribute()
    {
        var ctx = new SvgRenderContext();
        ctx.SetOpacity(0.5);

        string svg = ctx.GetOutput();
        Assert.Contains("<g opacity=\"", svg);
    }

    // --- MeasureText ---

    [Fact]
    public void MeasureText_ReturnsNonZeroSize()
    {
        var ctx = new SvgRenderContext();
        var size = ctx.MeasureText("Hello", new Font());

        Assert.True(size.Width > 0);
        Assert.True(size.Height > 0);
    }

    [Fact]
    public void MeasureText_EmptyString_ReturnsZeroWidth()
    {
        var ctx = new SvgRenderContext();
        var size = ctx.MeasureText("", new Font());

        Assert.Equal(0, size.Width);
    }

    // --- DrawText variants ---

    [Fact]
    public void DrawText_CenterAlignment_HasMiddleAnchor()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawText("Center", new Point(50, 50), new Font(), TextAlignment.Center);

        string svg = ctx.GetOutput();
        Assert.Contains("text-anchor=\"middle\"", svg);
    }

    [Fact]
    public void DrawText_RightAlignment_HasEndAnchor()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawText("Right", new Point(50, 50), new Font(), TextAlignment.Right);

        string svg = ctx.GetOutput();
        Assert.Contains("text-anchor=\"end\"", svg);
    }

    [Fact]
    public void DrawText_BoldFont_HasBoldWeight()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawText("Bold", new Point(50, 50),
            new Font { Weight = FontWeight.Bold }, TextAlignment.Left);

        string svg = ctx.GetOutput();
        Assert.Contains("font-weight=\"bold\"", svg);
    }

    [Fact]
    public void DrawText_ItalicFont_HasItalicStyle()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawText("Italic", new Point(50, 50),
            new Font { Slant = FontSlant.Italic }, TextAlignment.Left);

        string svg = ctx.GetOutput();
        Assert.Contains("font-style=\"italic\"", svg);
    }

    [Fact]
    public void DrawText_SpecialChars_AreEscaped()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawText("<Test&>", new Point(50, 50), new Font(), TextAlignment.Left);

        string svg = ctx.GetOutput();
        Assert.Contains("&lt;Test&amp;&gt;", svg);
    }

}
