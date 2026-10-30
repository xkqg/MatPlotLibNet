// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>
/// Describes the font used to render text.
/// </summary>
public sealed record Font
{
    /// <summary>The font family as a CSS font-family list, for example <c>"DejaVu Sans, sans-serif"</c>. <c>sans-serif</c> by default.</summary>
    public string Family { get; init; } = "sans-serif";

    /// <summary>The font size in points. 13 by default.</summary>
    public double Size { get; init; } = 13;

    /// <summary>How heavy the letters are drawn: light, normal or bold. Normal by default.</summary>
    public FontWeight Weight { get; init; } = FontWeight.Normal;

    /// <summary>Whether the text is upright, italic or oblique. Upright by default.</summary>
    public FontSlant Slant { get; init; } = FontSlant.Normal;

    /// <summary>The text colour, or <see langword="null"/> to set none and let the text take the surrounding default colour.</summary>
    public Color? Color { get; init; }
}

/// <summary>Specifies the weight (boldness) of a font.</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder,
/// remove, or renumber.</remarks>
public enum FontWeight
{
    /// <summary>A light font weight.</summary>
    Light = 0,

    /// <summary>The normal (regular) font weight.</summary>
    Normal = 1,

    /// <summary>A bold font weight.</summary>
    Bold = 2,
}

/// <summary>Specifies the slant style of a font.</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder,
/// remove, or renumber.</remarks>
public enum FontSlant
{
    /// <summary>Upright text with no slant.</summary>
    Normal = 0,

    /// <summary>Italic text.</summary>
    Italic = 1,

    /// <summary>Oblique (slanted) text.</summary>
    Oblique = 2,
}
