// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using SkiaSharp;

namespace MatPlotLibNet.Transforms;

/// <summary>Transforms a <see cref="Figure"/> into PNG format using SkiaSharp.</summary>
public sealed class PngTransform : FigureTransform
{
    /// <summary>Creates a new PNG transform with the specified chart renderer.</summary>
    public PngTransform(IChartRenderer renderer) : base(renderer) { }

    /// <summary>Creates a new PNG transform with the default chart renderer.</summary>
    public PngTransform() { }

    /// <summary>Transforms the figure and writes PNG bytes to the output stream.</summary>
    public override void Transform(Figure figure, Stream output)
    {
        int width = (int)figure.Width;
        int height = (int)figure.Height;

        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        var ctx = new MatPlotLibNet.Skia.SkiaRenderContext(canvas);
        Renderer.Render(figure, ctx);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        data.SaveTo(output);
    }
}
