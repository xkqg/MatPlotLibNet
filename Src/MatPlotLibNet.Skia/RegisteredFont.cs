// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Skia;

/// <summary>What <see cref="SkiaFonts.Register(string)"/> registered: the family name the font file declares, and
/// the weight and slant the file carries. Put <see cref="Family"/> in <see cref="Font.Family"/> (or in
/// <c>Theme.CreateFrom(...).WithFont(...)</c>) and the chart draws with it; the weight and slant say which
/// <see cref="Font"/> settings reach this file rather than another face of the same family.</summary>
/// <param name="Family">The family name inside the font file, which is the name to use in <see cref="Font.Family"/>.</param>
/// <param name="Weight">The weight the file carries.</param>
/// <param name="Slant">The slant the file carries.</param>
public readonly record struct RegisteredFont(string Family, FontWeight Weight, FontSlant Slant);
