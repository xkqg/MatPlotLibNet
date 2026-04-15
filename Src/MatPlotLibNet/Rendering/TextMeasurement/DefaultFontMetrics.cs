// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.TextMeasurement;

/// <summary>
/// Pure-managed default <see cref="IFontMetrics"/> that uses <see cref="CharacterWidthTable"/>
/// per-character width factors. Good enough when no platform font rasterizer is available;
/// ignores <see cref="Font.Family"/>, <see cref="Font.Weight"/>, <see cref="Font.Slant"/>.
/// Replaced at runtime by <c>SkiaFontMetrics</c> when <c>MatPlotLibNet.Skia</c> is loaded.
/// </summary>
public sealed class DefaultFontMetrics : IFontMetrics
{
    /// <inheritdoc />
    public Size Measure(string text, Font font)
    {
        double width = 0;
        foreach (var c in text)
            width += CharacterWidthTable.GetWidth(c);
        width *= font.Size;
        double height = font.Size * 1.2;
        return new Size(width, height);
    }
}
