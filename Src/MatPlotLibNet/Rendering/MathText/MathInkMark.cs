// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.MathText;

/// <summary>Where a box's ink starts in a <see cref="MathInk"/>: the counts of its glyph runs and rules before it.</summary>
/// <param name="Glyphs">The glyph runs before it.</param>
/// <param name="Rules">The rules before it.</param>
internal readonly record struct MathInkMark(int Glyphs, int Rules);
