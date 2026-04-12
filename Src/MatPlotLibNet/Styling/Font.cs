// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>
/// Describes the font used to render text.
/// </summary>
public sealed record Font
{
    public string Family { get; init; } = "sans-serif";

    public double Size { get; init; } = 13;

    public FontWeight Weight { get; init; } = FontWeight.Normal;

    public FontSlant Slant { get; init; } = FontSlant.Normal;

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
