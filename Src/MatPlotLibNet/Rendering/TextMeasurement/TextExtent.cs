// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.TextMeasurement;

/// <summary>How much room a line of text takes: its advance along the baseline, the height of the line it stands in, and how
/// far its letters rise above the baseline (the rest of the height lies below it). What every measuring site of the library
/// reads, through <see cref="TextExtentExtensions"/>; the library's metrics measure a line by <see cref="OfLine"/>.</summary>
/// <param name="Width">The advance of the text, in pixels.</param>
/// <param name="Height">The height of the line the text stands in, in pixels.</param>
/// <param name="Ascent">How far the letters rise above the baseline, in pixels.</param>
internal readonly record struct TextExtent(double Width, double Height, double Ascent)
{
    /// <summary>How many times its font size a line of text is tall.</summary>
    internal const double LineSpacing = 1.2;

    /// <summary>How far the line reaches below the baseline.</summary>
    internal double Descent => Height - Ascent;

    /// <summary>The upright box of the text: its advance by its line's height.</summary>
    internal Size Box => new(Width, Height);

    /// <summary>A line of text <paramref name="width"/> wide in <paramref name="font"/>: <see cref="LineSpacing"/> times the font
    /// size tall, the letters rising the font size above the baseline.</summary>
    /// <param name="width">The advance of the text.</param>
    /// <param name="font">The font it is set in.</param>
    /// <returns>The extent.</returns>
    internal static TextExtent OfLine(double width, Font font) => new(width, font.Size * LineSpacing, font.Size);

    /// <summary>The extent of a text measured as <paramref name="box"/> (by a font metrics or a surface, or a size kept from an
    /// earlier measure): the letters rise the font size above the baseline.</summary>
    /// <param name="box">The measured box.</param>
    /// <param name="fontSize">The size of the font the text was measured in.</param>
    /// <returns>The extent.</returns>
    internal static TextExtent Of(Size box, double fontSize) => new(box.Width, box.Height, fontSize);

    /// <summary>This extent, rising at least as far above the baseline and reaching at least as deep below it as <paramref name="floor"/>:
    /// how matplotlib's <c>TextArea</c> boxes a text in a legend (<c>offsetbox.TextArea.get_bbox</c> measures 'lp' and takes
    /// <c>max(h_ - d_, h - yd)</c> above the baseline and <c>max(d_, yd)</c> below it). The width is this extent's.</summary>
    /// <param name="floor">The extent of the text the box is at least as tall as; <see langword="null"/>: none, the extent as measured.</param>
    /// <returns>The extent.</returns>
    internal TextExtent AtLeast(TextExtent? floor)
    {
        if (floor is not { } f) return this;
        double ascent = Math.Max(Ascent, f.Ascent);
        return new(Width, ascent + Math.Max(Descent, f.Descent), ascent);
    }

    /// <summary>A line of text measured by its ink, as matplotlib's Agg backend measures it (<c>Text._get_layout</c>): the letters rise
    /// as far as their ink or the font's typographic ascender, whichever is higher, and the line reaches as far below the baseline as
    /// their ink or the font's typographic descender, whichever is deeper.</summary>
    /// <param name="width">The advance of the text.</param>
    /// <param name="rise">How far its ink reaches above the baseline.</param>
    /// <param name="drop">How far its ink reaches below the baseline.</param>
    /// <param name="band">The typographic band of its font at its size.</param>
    /// <returns>The extent.</returns>
    internal static TextExtent OfInk(double width, double rise, double drop, FontBand band)
    {
        double ascent = Math.Max(rise, band.Ascender);
        return new(width, ascent + Math.Max(drop, band.Descender), ascent);
    }
}
