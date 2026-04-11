// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>
/// Describes the font used to render text.
/// </summary>
public sealed record Font
{
    /// <summary>Gets the font family name.</summary>
    public string Family { get; init; } = "sans-serif";

    /// <summary>Gets the font size in CSS pixels. Default 13 ≈ matplotlib's 10pt at 96 dpi.</summary>
    public double Size { get; init; } = 13;

    /// <summary>Gets the font weight.</summary>
    public FontWeight Weight { get; init; } = FontWeight.Normal;

    /// <summary>Gets the font slant (normal, italic, or oblique).</summary>
    public FontSlant Slant { get; init; } = FontSlant.Normal;

    /// <summary>Gets the font color, or <c>null</c> to inherit from the theme.</summary>
    public Color? Color { get; init; }
}

/// <summary>
/// Specifies the weight (boldness) of a font.
/// </summary>
public enum FontWeight
{
    /// <summary>A light font weight.</summary>
    Light,

    /// <summary>The normal (regular) font weight.</summary>
    Normal,

    /// <summary>A bold font weight.</summary>
    Bold
}

/// <summary>
/// Specifies the slant style of a font.
/// </summary>
public enum FontSlant
{
    /// <summary>Upright text with no slant.</summary>
    Normal,

    /// <summary>Italic text.</summary>
    Italic,

    /// <summary>Oblique (slanted) text.</summary>
    Oblique
}
