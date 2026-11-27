// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>A transparent square bitmap, the canvas over it and the render context drawn onto the canvas, which a test of the context
/// reads back pixels from.</summary>
public sealed class SkiaCanvasFixture : IDisposable
{
    /// <summary>Makes a bitmap of <paramref name="size"/> pixels square, cleared to transparent, and a context onto it.</summary>
    /// <param name="size">The width and the height of the bitmap, in pixels.</param>
    public SkiaCanvasFixture(int size)
    {
        Bitmap = new SKBitmap(size, size);
        Canvas = new SKCanvas(Bitmap);
        Canvas.Clear(SKColors.Transparent);
        Context = new SkiaRenderContext(Canvas);
    }

    /// <summary>The bitmap the context draws onto.</summary>
    public SKBitmap Bitmap { get; }

    /// <summary>The canvas over <see cref="Bitmap"/>.</summary>
    public SKCanvas Canvas { get; }

    /// <summary>The render context onto <see cref="Canvas"/>.</summary>
    public SkiaRenderContext Context { get; }

    /// <summary>Lets go of the canvas and the bitmap.</summary>
    public void Dispose()
    {
        Canvas.Dispose();
        Bitmap.Dispose();
    }
}
