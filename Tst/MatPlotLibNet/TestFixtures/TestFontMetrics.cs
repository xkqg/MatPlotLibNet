// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests;

/// <summary>The text measure a test hands a renderer, a layout or the process: it measures as <paramref name="measure"/> says, so a box
/// does not depend on a machine's fonts, and keeps every text it measured. A test that also needs the ink of the glyphs derives from it.</summary>
/// <param name="measure">The box of a text in a font.</param>
internal class TestFontMetrics(Func<string, Font, Size> measure) : IFontMetrics
{
    /// <summary>Every text the measure was asked about, in order; written under its own lock, as a renderer may measure on many threads.</summary>
    public List<string> Measured { get; } = [];

    /// <summary>How many texts the measure was asked about.</summary>
    public int Calls => Measured.Count;

    public Size Measure(string text, Font font)
    {
        lock (Measured) Measured.Add(text);
        return measure(text, font);
    }
}
