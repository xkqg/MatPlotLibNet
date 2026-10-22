// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Skia;
using SkiaSharp;

namespace MatPlotLibNet.Transforms;

/// <summary>Transforms a <see cref="Figure"/> into PNG format using SkiaSharp.</summary>
/// <remarks>A figure is laid out in units of one pixel at 96 dpi, and the PNG is drawn with <c>dpi / 96</c> pixels for
/// each unit, so <c>Figure.Dpi</c> (set with <c>WithDpi</c>) sizes the picture: a 500 by 350 figure at 192 dpi is a
/// 1000 by 700 PNG with the same layout, drawn finer. Handed <see cref="SaveOptions"/>, the transform reads three
/// of its settings: <see cref="SaveOptions.Dpi"/> (when it is not the default of 96) takes the place of the figure's
/// resolution, and <see cref="SaveOptions.Title"/> and <see cref="SaveOptions.Author"/> are stored in the file as text
/// chunks with the keywords Title and Author. The settings that belong to an SVG are not used.</remarks>
public sealed class PngTransform : FigureTransform
{
    /// <summary>Creates a new PNG transform with the specified chart renderer.</summary>
    public PngTransform(IChartRenderer renderer) : base(renderer) { }

    /// <summary>Creates a new PNG transform with the default chart renderer.</summary>
    public PngTransform() { }

    /// <summary>Transforms the figure and writes PNG bytes to the output stream, drawn at the figure's own resolution.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="Figure.Dpi"/> is not a positive, finite number.</exception>
    /// <exception cref="ArgumentException">At that resolution the figure is not a whole number of pixels from 1 up.</exception>
    public override void Transform(Figure figure, Stream output) => Transform(figure, output, new SaveOptions());

    /// <summary>Transforms the figure and writes PNG bytes to the output stream, drawn at the resolution of
    /// <paramref name="options"/> when they ask for one and at the figure's own otherwise, with the title and the author
    /// of the options stored as text chunks.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The resolution is not a positive, finite number.</exception>
    /// <exception cref="ArgumentException">At that resolution the figure is not a whole number of pixels from 1 up.</exception>
    public override void Transform(Figure figure, Stream output, SaveOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        using var bitmap = Renderer.RenderToBitmap(figure, options.DpiFor(figure));
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        var text = new List<PngTextEntry>(2);
        if (options.TitleOrNull() is { } title) text.Add(new PngTextEntry("Title", title));
        if (options.AuthorOrNull() is { } author) text.Add(new PngTextEntry("Author", author));

        if (text.Count == 0)
        {
            data.SaveTo(output);
            return;
        }

        byte[] png = data.ToArray().WithTextChunks(text);
        output.Write(png, 0, png.Length);
    }
}
