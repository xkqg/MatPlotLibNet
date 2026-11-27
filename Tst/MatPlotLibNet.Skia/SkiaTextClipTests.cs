// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>On the canvas a clip cuts the marks drawn under it, never a text: a series' label stands whole past the plot, as matplotlib
/// draws a text (<c>clip_on False</c>) and a bar label (an Annotation with no clip box), while the marks before and after it are cut.</summary>
public class SkiaTextClipTests : IDisposable
{
    private readonly SKBitmap _bitmap = new(120, 120);
    private readonly SKCanvas _canvas;
    private readonly SkiaRenderContext _ctx;

    public SkiaTextClipTests()
    {
        _canvas = new SKCanvas(_bitmap);
        _canvas.Clear(SKColors.Transparent);
        _ctx = new SkiaRenderContext(_canvas);
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    [Fact]
    public void ATextDrawnUnderAClip_IsDrawnWhole_WhileTheMarksAroundItAreCut()
    {
        _ctx.PushClip(new Rect(0, 0, 40, 40));
        _ctx.DrawRectangle(new Rect(50, 0, 20, 20), new ShapeStyle(Colors.Red, null, 0));
        _ctx.DrawText("WWWW", new Point(50, 100), new Font { Size = 20, Color = Colors.Black }, TextAlignment.Left);
        _ctx.DrawRectangle(new Rect(0, 50, 20, 20), new ShapeStyle(Colors.Blue, null, 0));
        _ctx.DrawRectangle(new Rect(5, 5, 10, 10), new ShapeStyle(Colors.Green, null, 0));
        _ctx.PopClip();
        _canvas.Flush();

        Assert.True(InkIn(new SKRectI(50, 80, 120, 105)) > 0, "the text past the clip was cut");
        Assert.Equal(0, InkIn(new SKRectI(50, 0, 70, 20)));
        Assert.Equal(0, InkIn(new SKRectI(0, 50, 20, 70)));
        Assert.Equal(SKColors.Green, _bitmap.GetPixel(10, 10));
    }

    [Fact]
    public void ARichTextDrawnUnderAClip_IsDrawnWhole()
    {
        _ctx.PushClip(new Rect(0, 0, 40, 40));
        _ctx.DrawRichText(MatPlotLibNet.Rendering.MathText.MathTextParser.Parse("W$^{2}$"), new Point(50, 100),
            new Font { Size = 20, Color = Colors.Black }, TextAlignment.Left);
        _ctx.DrawRectangle(new Rect(0, 50, 20, 20), new ShapeStyle(Colors.Blue, null, 0));
        _ctx.PopClip();
        _canvas.Flush();

        Assert.True(InkIn(new SKRectI(50, 70, 120, 105)) > 0, "the rich text past the clip was cut");
        Assert.Equal(0, InkIn(new SKRectI(0, 50, 20, 70)));
    }

    /// <summary>A text drawn while the surface cuts its texts (a contour's labels, which matplotlib clips at the axes) is cut by the clip
    /// as a mark is; the text drawn after the scope ends is whole again.</summary>
    [Fact]
    public void ATextDrawnWhileTheTextsAreCut_IsCutByTheClip_AndTheNextTextIsWhole()
    {
        _ctx.PushClip(new Rect(0, 0, 40, 40));
        using (_ctx.CuttingTexts())
        {
            _ctx.DrawText("WWWW", new Point(50, 100), new Font { Size = 20, Color = Colors.Black }, TextAlignment.Left);
        }
        _ctx.DrawText("WWWW", new Point(50, 60), new Font { Size = 20, Color = Colors.Black }, TextAlignment.Left);
        _ctx.PopClip();
        _canvas.Flush();

        Assert.Equal(0, InkIn(new SKRectI(50, 80, 120, 105)));
        Assert.True(InkIn(new SKRectI(50, 40, 120, 65)) > 0, "the text after the scope was cut");
    }

    /// <summary>The label of a contour line past the axes is cut off with its line in a PNG: nothing of it stands in the margin beside
    /// the plot (the x axis is cut at 8, the line and its label stand at 8.5).</summary>
    [Fact]
    public void TheLabelOfAContourLinePastTheAxes_IsCutOffInAPng()
    {
        var figure = Theme.MatplotlibV2.LabelledContourPastTheAxes();
        var plot = figure.PlotAreaOnSkia();
        using var picture = SKBitmap.Decode(figure.ToPng());

        int ink = 0;
        for (int y = (int)plot.Y + 3; y < (int)(plot.Y + plot.Height) - 3; y++)
            for (int x = (int)(plot.X + plot.Width) + 3; x < picture.Width; x++)
                if (picture.GetPixel(x, y) != SKColors.White) ink++;

        Assert.Equal(0, ink);
    }

    /// <summary>The pixels with any alpha in <paramref name="area"/>.</summary>
    private int InkIn(SKRectI area)
    {
        int ink = 0;
        for (int y = area.Top; y < area.Bottom; y++)
            for (int x = area.Left; x < area.Right; x++)
                if (_bitmap.GetPixel(x, y).Alpha > 0) ink++;
        return ink;
    }
}
