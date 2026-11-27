// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.TextMeasurement;

/// <summary>The band a font's designer gave a line of its text: how far its typographic ascender rises above the baseline and how far its
/// typographic descender reaches below it, at the font's size (the OS/2 table's <c>sTypoAscender</c> and <c>sTypoDescender</c>, else the
/// horizontal header's ascent and descent), which matplotlib reads as the least a line of text takes.</summary>
/// <param name="Ascender">The ascender's height above the baseline, in pixels.</param>
/// <param name="Descender">The descender's depth below the baseline, in pixels (positive).</param>
internal readonly record struct FontBand(double Ascender, double Descender);
