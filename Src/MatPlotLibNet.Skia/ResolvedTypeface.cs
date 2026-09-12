// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace MatPlotLibNet.Skia;

/// <summary>A typeface and the shaper built over it, resolved once per (family, weight, slant) and shared by every
/// text call for the life of the process. Callers never dispose either: the registry owns them, and one shaper
/// serves every thread at once (HarfBuzz's font object is immutable after construction; each call brings its own
/// buffer).</summary>
/// <param name="Typeface">The typeface the family, weight and slant resolved to.</param>
/// <param name="Shaper">The HarfBuzz shaper over that typeface.</param>
internal readonly record struct ResolvedTypeface(SKTypeface Typeface, SKShaper Shaper);
