// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using SkiaSharp;

namespace MatPlotLibNet.Skia;

/// <summary>Text after shaping: the glyphs to draw, where each one sits relative to the start of the baseline, in
/// the order they appear from left to right, and the total advance. Every place the Skia package measures or draws
/// text reads one of these, so the width the layout reserved is the width the glyph outlines span and the width the
/// canvas paints.</summary>
/// <param name="Glyphs">Glyph ids in visual order, left to right.</param>
/// <param name="Positions">Where each glyph's origin sits, relative to a baseline origin at (0, 0).</param>
/// <param name="Width">The total advance.</param>
/// <remarks>Holds arrays, so the generated equality compares references: two shaped texts are never compared or
/// used as keys.</remarks>
internal readonly record struct ShapedText(ushort[] Glyphs, SKPoint[] Positions, float Width)
{
    /// <summary>Nothing to draw and no width.</summary>
    public static ShapedText Empty { get; } = new([], [], 0f);
}
