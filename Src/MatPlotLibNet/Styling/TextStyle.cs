// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>
/// A partial font/text override that layers on top of a theme <see cref="Font"/>.
/// All properties are nullable — a <c>null</c> value means "keep the base font value".
/// </summary>
/// <remarks>
/// <para>
/// <b>Design note (Liskov Substitution):</b> <see cref="TextStyle"/> is intentionally NOT a
/// subtype of <see cref="Font"/>. <see cref="Font"/> has fully-specified concrete defaults
/// (e.g. <c>Size = 13</c>); every <see cref="Font"/> satisfies the postcondition that all
/// properties are concrete. <see cref="TextStyle"/> has <i>nullable</i> properties — it is a
/// <i>partial override</i>, not a replacement. Making it extend <see cref="Font"/> would violate
/// Liskov: a <see cref="TextStyle"/> with <c>null</c> Size would break the postcondition that
/// <c>Size</c> is always concrete.
/// </para>
/// <para>
/// Use <see cref="ApplyTo"/> to merge overrides into a base <see cref="Font"/>, obtaining a
/// fully-specified <see cref="Font"/> that preserves all Liskov invariants.
/// </para>
/// </remarks>
public sealed record TextStyle
{
    public double? FontSize { get; init; }

    public FontWeight? FontWeight { get; init; }

    public string? FontFamily { get; init; }

    public FontSlant? FontSlant { get; init; }

    public Color? Color { get; init; }

    public double? Pad { get; init; }

    /// <summary>
    /// Merges this style's non-null overrides into <paramref name="baseFont"/>, returning a new
    /// fully-specified <see cref="Font"/>. The original <paramref name="baseFont"/> is not mutated.
    /// </summary>
    /// <param name="baseFont">The base font whose values are used for any property this style leaves <c>null</c>.</param>
    /// <returns>A new <see cref="Font"/> with overrides applied.</returns>
    public Font ApplyTo(Font baseFont) => baseFont with
    {
        Size = FontSize ?? baseFont.Size,
        Weight = FontWeight ?? baseFont.Weight,
        Family = FontFamily ?? baseFont.Family,
        Slant = FontSlant ?? baseFont.Slant,
        Color = Color ?? baseFont.Color,
    };
}
