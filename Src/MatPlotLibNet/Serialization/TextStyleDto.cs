// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Serialization;

/// <summary>The figure JSON's form of a <see cref="TextStyle"/> (an axes title's or an axis label's): each override is
/// written only when it is set, so a style that overrides nothing is left out whole.</summary>
internal sealed record TextStyleDto
{
    public double? FontSize { get; init; }
    public FontWeight? FontWeight { get; init; }
    public string? FontFamily { get; init; }
    public FontSlant? FontSlant { get; init; }
    public Color? Color { get; init; }
    public double? Pad { get; init; }

    private static readonly TextStyle Plain = new();

    /// <summary>The JSON's form of <paramref name="style"/>, or <see langword="null"/> when it overrides nothing.</summary>
    internal static TextStyleDto? From(TextStyle? style) => style is null || style == Plain ? null : new()
    {
        FontSize = style.FontSize,
        FontWeight = style.FontWeight,
        FontFamily = style.FontFamily,
        FontSlant = style.FontSlant,
        Color = style.Color,
        Pad = style.Pad,
    };

    /// <summary>The style this record describes.</summary>
    internal TextStyle ToTextStyle() => new()
    {
        FontSize = FontSize,
        FontWeight = FontWeight,
        FontFamily = FontFamily,
        FontSlant = FontSlant,
        Color = Color,
        Pad = Pad,
    };
}

/// <summary>The figure JSON's form of a <see cref="Font"/> (an annotation's): each property is written only when it
/// differs from a new font's.</summary>
internal sealed record FontDto
{
    public string? Family { get; init; }
    public double? Size { get; init; }
    public FontWeight? Weight { get; init; }
    public FontSlant? Slant { get; init; }
    public Color? Color { get; init; }

    private static readonly Font Rest = new();

    /// <summary>The JSON's form of <paramref name="font"/>, or <see langword="null"/> when there is no font (the text
    /// then takes the theme's).</summary>
    internal static FontDto? From(Font? font) => font is null ? null : new()
    {
        Family = font.Family.OrNullAtRest(Rest.Family),
        Size = font.Size.OrNullAtRest(Rest.Size),
        Weight = font.Weight.OrNullAtRest(Rest.Weight),
        Slant = font.Slant.OrNullAtRest(Rest.Slant),
        Color = font.Color,
    };

    /// <summary>The font this record describes.</summary>
    internal Font ToFont() => new()
    {
        Family = Family ?? Rest.Family,
        Size = Size ?? Rest.Size,
        Weight = Weight ?? Rest.Weight,
        Slant = Slant ?? Rest.Slant,
        Color = Color,
    };
}
