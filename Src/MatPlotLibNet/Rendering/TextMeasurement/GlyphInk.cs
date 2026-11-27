// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.TextMeasurement;

/// <summary>How a text's glyphs stand when matplotlib's Agg backend sets them (<see cref="IGlyphInkMetrics"/>): its advance as the glyphs
/// are drawn, its advance with every glyph's own advance on whole pixels, and how far its ink reaches above and below the baseline in
/// whole pixels, and the typographic band of the face at the size the text is set at.</summary>
/// <param name="Advance">The advance of the text as its glyphs are shaped, kerning included, in pixels.</param>
/// <param name="PixelAdvance">The advance with each glyph's advance rounded to whole pixels at the font's size rounded to whole pixels,
/// the kerning between them kept, in pixels.</param>
/// <param name="Rise">How far the highest glyph's ink reaches above the baseline, on whole pixels; 0 for a text with no ink.</param>
/// <param name="Drop">How far the lowest glyph's ink reaches below the baseline, on whole pixels; 0 when none reaches below it.</param>
/// <param name="Band">The typographic band of the face at the size the text is set at (an empty text has one too).</param>
internal readonly record struct GlyphInk(double Advance, double PixelAdvance, double Rise, double Drop, FontBand Band);
