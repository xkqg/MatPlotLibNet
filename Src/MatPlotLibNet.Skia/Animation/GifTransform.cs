// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Skia;
using SkiaSharp;

namespace MatPlotLibNet.Transforms.Animation;

/// <summary>Transforms a sequence of <see cref="Figure"/> frames into an animated GIF.</summary>
/// <remarks>Uses <see cref="GifEncoder"/> for GIF89a encoding and <see cref="ColorQuantizer"/>
/// for 256-color uniform quantization. Each frame is drawn at the resolution of its own figure
/// (<see cref="Figure.Dpi"/>), by the same rule as a PNG: <c>dpi / 96</c> pixels for each unit of the figure. The frames
/// of one animation share a size, so they share a resolution. A GIF is not saved through
/// <see cref="SaveOptions"/>.</remarks>
public sealed class GifTransform : IAnimationTransform
{
    private readonly IChartRenderer _renderer;

    /// <summary>Creates a <see cref="GifTransform"/> using the default chart renderer.</summary>
    public GifTransform() => _renderer = new ChartRenderer();

    /// <summary>Creates a <see cref="GifTransform"/> using the specified chart renderer.</summary>
    public GifTransform(IChartRenderer renderer) => _renderer = renderer;

    /// <inheritdoc />
    public void Transform(IEnumerable<Figure> frames, TimeSpan interval, bool loop, Stream output)
    {
        var bitmaps = new List<SKBitmap>();
        try
        {
            foreach (var figure in frames)
                bitmaps.Add(RenderToBitmap(figure));

            GifEncoder.Encode(bitmaps, (int)interval.TotalMilliseconds, loop, output);
        }
        finally
        {
            foreach (var bmp in bitmaps) bmp.Dispose();
        }
    }

    /// <summary>A frame is drawn at the resolution of its own figure, by the same rule as a PNG.</summary>
    private SKBitmap RenderToBitmap(Figure figure) => _renderer.RenderToBitmap(figure, figure.Dpi);
}
