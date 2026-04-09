// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>Verifies <see cref="SkiaRenderContext"/> behavior.</summary>
public class SkiaRenderContextTests : IDisposable
{
    private readonly SKBitmap _bitmap;
    private readonly SKCanvas _canvas;
    private readonly SkiaRenderContext _ctx;

    public SkiaRenderContextTests()
    {
        _bitmap = new SKBitmap(100, 100);
        _canvas = new SKCanvas(_bitmap);
        _canvas.Clear(SKColors.Transparent);
        _ctx = new SkiaRenderContext(_canvas);
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    /// <summary>Verifies that the constructor accepts an SKCanvas without throwing.</summary>
    [Fact]
    public void Constructor_AcceptsSKCanvas_WithoutThrowing()
    {
        using var bitmap = new SKBitmap(10, 10);
        using var canvas = new SKCanvas(bitmap);
        var ctx = new SkiaRenderContext(canvas);
        Assert.NotNull(ctx);
    }

    /// <summary>Verifies that DrawLine renders non-transparent pixels on the canvas.</summary>
    [Fact]
    public void DrawLine_RendersNonTransparentPixels()
    {
        _ctx.DrawLine(new Point(10, 50), new Point(90, 50), Colors.Red, 2, LineStyle.Solid);
        _canvas.Flush();

        Assert.True(HasNonTransparentPixels());
    }

    /// <summary>Verifies that DrawLines renders a polyline with non-transparent pixels.</summary>
    [Fact]
    public void DrawLines_RendersPolyline()
    {
        var points = new List<Point> { new(10, 10), new(50, 90), new(90, 10) };
        _ctx.DrawLines(points, Colors.Blue, 2, LineStyle.Solid);
        _canvas.Flush();

        Assert.True(HasNonTransparentPixels());
    }

    /// <summary>Verifies that DrawPolygon with a fill color renders filled pixels.</summary>
    [Fact]
    public void DrawPolygon_WithFill_RendersFilled()
    {
        var points = new List<Point> { new(10, 10), new(90, 10), new(90, 90), new(10, 90) };
        _ctx.DrawPolygon(points, fill: Colors.Green, stroke: null, strokeThickness: 0);
        _canvas.Flush();

        Assert.True(HasNonTransparentPixels());
    }

    /// <summary>Verifies that DrawPolygon with a stroke color renders an outline.</summary>
    [Fact]
    public void DrawPolygon_WithStroke_RendersOutline()
    {
        var points = new List<Point> { new(10, 10), new(90, 10), new(90, 90), new(10, 90) };
        _ctx.DrawPolygon(points, fill: null, stroke: Colors.Red, strokeThickness: 2);
        _canvas.Flush();

        Assert.True(HasNonTransparentPixels());
    }

    /// <summary>Verifies that DrawCircle with a fill color renders non-transparent pixels at the center.</summary>
    [Fact]
    public void DrawCircle_WithFill_RendersCenterArea()
    {
        _ctx.DrawCircle(new Point(50, 50), 30, fill: Colors.Blue, stroke: null, strokeThickness: 0);
        _canvas.Flush();

        // Verify the center area has non-transparent pixels
        var pixel = _bitmap.GetPixel(50, 50);
        Assert.NotEqual(SKColors.Transparent, pixel);
    }

    /// <summary>Verifies that DrawCircle with a stroke color renders an outline.</summary>
    [Fact]
    public void DrawCircle_WithStroke_RendersOutline()
    {
        _ctx.DrawCircle(new Point(50, 50), 30, fill: null, stroke: Colors.Red, strokeThickness: 3);
        _canvas.Flush();

        Assert.True(HasNonTransparentPixels());
    }

    /// <summary>Verifies that DrawRectangle with a fill color renders non-transparent pixels at the center.</summary>
    [Fact]
    public void DrawRectangle_WithFill_RendersFilled()
    {
        _ctx.DrawRectangle(new Rect(10, 10, 80, 80), fill: Colors.Green, stroke: null, strokeThickness: 0);
        _canvas.Flush();

        var pixel = _bitmap.GetPixel(50, 50);
        Assert.NotEqual(SKColors.Transparent, pixel);
    }

    /// <summary>Verifies that DrawRectangle with a stroke color renders an outline.</summary>
    [Fact]
    public void DrawRectangle_WithStroke_RendersOutline()
    {
        _ctx.DrawRectangle(new Rect(10, 10, 80, 80), fill: null, stroke: Colors.Black, strokeThickness: 2);
        _canvas.Flush();

        Assert.True(HasNonTransparentPixels());
    }

    /// <summary>Verifies that DrawEllipse with a fill color renders non-transparent pixels at the center.</summary>
    [Fact]
    public void DrawEllipse_WithFill_RendersFilled()
    {
        _ctx.DrawEllipse(new Rect(10, 10, 80, 80), fill: Colors.Magenta, stroke: null, strokeThickness: 0);
        _canvas.Flush();

        var pixel = _bitmap.GetPixel(50, 50);
        Assert.NotEqual(SKColors.Transparent, pixel);
    }

    /// <summary>Verifies that DrawText renders non-transparent pixels on the canvas.</summary>
    [Fact]
    public void DrawText_RendersPixels()
    {
        _ctx.DrawText("Hello", new Point(10, 50), new Font { Size = 20, Color = Colors.Black },
            TextAlignment.Left);
        _canvas.Flush();

        Assert.True(HasNonTransparentPixels());
    }

    /// <summary>Verifies that DrawPath with MoveTo and LineTo segments renders non-transparent pixels.</summary>
    [Fact]
    public void DrawPath_MoveToLineTo_RendersSegments()
    {
        var segments = new List<PathSegment>
        {
            new MoveToSegment(new Point(10, 10)),
            new LineToSegment(new Point(90, 50)),
            new LineToSegment(new Point(50, 90))
        };
        _ctx.DrawPath(segments, fill: null, stroke: Colors.Red, strokeThickness: 2);
        _canvas.Flush();

        Assert.True(HasNonTransparentPixels());
    }

    /// <summary>Verifies that DrawPath with a Close segment renders a filled closed shape.</summary>
    [Fact]
    public void DrawPath_WithClose_RendersClosedShape()
    {
        var segments = new List<PathSegment>
        {
            new MoveToSegment(new Point(10, 10)),
            new LineToSegment(new Point(90, 10)),
            new LineToSegment(new Point(50, 90)),
            new CloseSegment()
        };
        _ctx.DrawPath(segments, fill: Colors.Cyan, stroke: null, strokeThickness: 0);
        _canvas.Flush();

        Assert.True(HasNonTransparentPixels());
    }

    /// <summary>Verifies that PushClip prevents drawing outside the clipping rectangle.</summary>
    [Fact]
    public void PushClip_ClipsDrawingOutsideRect()
    {
        // Clip to a small region in the top-left
        _ctx.PushClip(new Rect(0, 0, 10, 10));
        // Draw a rectangle in the bottom-right, fully outside the clip
        _ctx.DrawRectangle(new Rect(50, 50, 40, 40), fill: Colors.Red, stroke: null, strokeThickness: 0);
        _canvas.Flush();

        // Verify the bottom-right area remains transparent (alpha == 0)
        var pixel = _bitmap.GetPixel(70, 70);
        Assert.Equal(0, pixel.Alpha);
    }

    /// <summary>Verifies that PopClip restores full-canvas drawing after removing the clip region.</summary>
    [Fact]
    public void PopClip_RestoresDrawingAfterClip()
    {
        _ctx.PushClip(new Rect(0, 0, 10, 10));
        _ctx.PopClip();
        // Drawing should now work across the full canvas
        _ctx.DrawRectangle(new Rect(50, 50, 40, 40), fill: Colors.Blue, stroke: null, strokeThickness: 0);
        _canvas.Flush();

        var pixel = _bitmap.GetPixel(70, 70);
        Assert.NotEqual(SKColors.Transparent, pixel);
    }

    /// <summary>Verifies that MeasureText returns a non-zero width and height for a text string.</summary>
    [Fact]
    public void MeasureText_ReturnsNonZeroSize()
    {
        var size = _ctx.MeasureText("Hello World", new Font { Size = 12 });

        Assert.True(size.Width > 0);
        Assert.True(size.Height > 0);
    }

    /// <summary>Verifies that MeasureText returns larger dimensions for a larger font size.</summary>
    [Fact]
    public void MeasureText_LargerFont_ReturnsLargerSize()
    {
        var smallSize = _ctx.MeasureText("Hello", new Font { Size = 10 });
        var largeSize = _ctx.MeasureText("Hello", new Font { Size = 24 });

        Assert.True(largeSize.Width > smallSize.Width);
        Assert.True(largeSize.Height > smallSize.Height);
    }

    /// <summary>Verifies that SetOpacity reduces the alpha channel of rendered pixels.</summary>
    [Fact]
    public void SetOpacity_ReducesPixelAlpha()
    {
        // Draw fully opaque rectangle first to compare
        using var opaqueBitmap = new SKBitmap(100, 100);
        using var opaqueCanvas = new SKCanvas(opaqueBitmap);
        opaqueCanvas.Clear(SKColors.Transparent);
        var opaqueCtx = new SkiaRenderContext(opaqueCanvas);
        opaqueCtx.DrawRectangle(new Rect(10, 10, 80, 80), fill: Colors.Red, stroke: null, strokeThickness: 0);
        opaqueCanvas.Flush();
        var opaquePixel = opaqueBitmap.GetPixel(50, 50);

        // Now draw with 50% opacity
        _ctx.SetOpacity(0.5);
        _ctx.DrawRectangle(new Rect(10, 10, 80, 80), fill: Colors.Red, stroke: null, strokeThickness: 0);
        _canvas.Flush();
        var semiPixel = _bitmap.GetPixel(50, 50);

        Assert.True(semiPixel.Alpha < opaquePixel.Alpha,
            $"Semi-transparent alpha ({semiPixel.Alpha}) should be less than opaque alpha ({opaquePixel.Alpha})");
    }

    private bool HasNonTransparentPixels()
    {
        for (int y = 0; y < _bitmap.Height; y++)
            for (int x = 0; x < _bitmap.Width; x++)
                if (_bitmap.GetPixel(x, y).Alpha > 0)
                    return true;
        return false;
    }
}
