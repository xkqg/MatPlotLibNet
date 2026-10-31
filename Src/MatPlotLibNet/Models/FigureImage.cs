// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models;

/// <summary>An image placed on the figure itself rather than in a subplot (matplotlib's <c>figimage</c>): a logo, a watermark,
/// a texture behind the charts. Each value of <see cref="Data"/> is one pixel of the figure, drawn at its native size with no
/// resampling, coloured through a colour map as an image series is, and the image stands under every subplot. Its place is a
/// pair of offsets in pixels from the bottom-left corner of the figure; the first row of the data is the top row of the
/// image.</summary>
public sealed class FigureImage
{
    /// <summary>The pixel values, indexed [row, column]; each becomes one pixel of the figure. The first row is the top row of
    /// the image.</summary>
    public double[,] Data { get; }

    /// <summary>How far the left edge of the image stands from the left edge of the figure, in pixels (the units of the figure's
    /// <see cref="Figure.Width"/>, which are pixels at 96 dpi). An image that reaches past the figure is cut at its edge.</summary>
    public double XOffset { get; }

    /// <summary>How far the bottom edge of the image stands above the bottom edge of the figure, in pixels (the units of the
    /// figure's <see cref="Figure.Height"/>), counted up as in matplotlib's <c>figimage</c>.</summary>
    public double YOffset { get; }

    /// <summary>The colour map the values are drawn through; <see langword="null"/> (the default) takes viridis, as an image
    /// series does.</summary>
    public IColorMap? ColorMap { get; set; }

    /// <summary>How a value is mapped onto the colour map's 0 to 1 range before the colour lookup (linear, log, and so on);
    /// <see langword="null"/> (the default) maps linearly.</summary>
    public INormalizer? Normalizer { get; set; }

    /// <summary>The value at the low end of the colour range; <see langword="null"/> (the default) uses the smallest value of
    /// <see cref="Data"/> that can be drawn (a missing value, NaN, and an infinity are left out).</summary>
    public double? VMin { get; set; }

    /// <summary>The value at the high end of the colour range; <see langword="null"/> (the default) uses the largest value of
    /// <see cref="Data"/> that can be drawn.</summary>
    public double? VMax { get; set; }

    /// <summary>The opacity of the whole image, from 0 (clear) to 1 (opaque, the default).</summary>
    public double Alpha { get; set; } = 1.0;

    /// <summary>Creates an image at a place on the figure. A value that is NaN is a gap: the pixel is left unpainted, or painted
    /// in the colour map's bad colour when it names one. An offset that is not a finite number gives the image no place, and it
    /// is not drawn.</summary>
    /// <param name="data">The pixel values, indexed [row, column].</param>
    /// <param name="xOffset">The distance from the left edge of the figure to the left edge of the image, in pixels.</param>
    /// <param name="yOffset">The distance from the bottom edge of the figure to the bottom edge of the image, in pixels.</param>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
    public FigureImage(double[,] data, double xOffset, double yOffset)
    {
        ArgumentNullException.ThrowIfNull(data);
        Data = data;
        XOffset = xOffset;
        YOffset = yOffset;
    }

    /// <summary>The rectangle the image fills on a figure of the given height, in pixels with y downward: its left edge at
    /// <see cref="XOffset"/>, one pixel for each column across and for each row down, its bottom edge <see cref="YOffset"/> above
    /// the bottom of the figure. <see langword="null"/> when an offset is not a finite number, so the image is skipped rather
    /// than drawn nowhere.</summary>
    /// <param name="figureHeight">The figure's height in pixels.</param>
    internal Rect? BoundsOn(double figureHeight)
    {
        if (!double.IsFinite(XOffset) || !double.IsFinite(YOffset)) return null;

        int rows = Data.GetLength(0), cols = Data.GetLength(1);
        return new Rect(XOffset, figureHeight - YOffset - rows, cols, rows);
    }

    /// <summary>Whether the image shows on a figure of the given height: it has a place on the figure (<see cref="BoundsOn"/>) and
    /// at least one pixel. The one rule for what the picture carries of its own, read by the data table and by the MCP reader.</summary>
    /// <param name="figureHeight">The figure's height in pixels.</param>
    internal bool IsShownOn(double figureHeight) =>
        BoundsOn(figureHeight) is not null && Data.GetLength(0) > 0 && Data.GetLength(1) > 0;

    /// <summary>The coloured cells of the image: its colour map (viridis when it names none), its normalizer (linear when it
    /// names none), the colour range of its values and its opacity.</summary>
    internal ImageCells Cells() =>
        ImageCells.Of(Data, ColorMap ?? ColorMaps.Viridis, Normalizer ?? LinearNormalizer.Instance, VMin, VMax, Alpha);
}
