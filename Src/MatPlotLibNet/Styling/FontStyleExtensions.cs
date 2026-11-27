// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>Whether a <see cref="Font"/> is drawn bold or italic, asked of the font once for every surface that draws it: each surface
/// converts the answer to what its own canvas calls bold and italic, and none compares the weight or the slant to the enum itself. Whether a
/// slant leans is asked of the slant (<c>IsSlanted</c>), by the surfaces through <c>IsItalic</c> and by math for the letters of
/// a face.</summary>
internal static class FontStyleExtensions
{
    extension(Font font)
    {
        /// <summary>Whether the font is drawn bold: its weight is <see cref="FontWeight.Bold"/>. A light weight is drawn as the regular one.</summary>
        internal bool IsBold => font.Weight == FontWeight.Bold;

        /// <summary>Whether the font is drawn in its leaning face: its slant leans, italic or oblique. matplotlib sets the two in the same face
        /// (its font manager takes an oblique face for an italic font and an italic face for an oblique one, a near match of 0.1 in
        /// <c>score_style</c>), so an oblique text is drawn slanted on every surface, as an italic one is.</summary>
        internal bool IsItalic => font.Slant.IsSlanted;
    }

    extension(FontSlant slant)
    {
        /// <summary>Whether the slant leans: italic or oblique. The one rule a surface's face (<c>IsItalic</c>) and a math face's lean
        /// (the room a superscript keeps from a leaning letter) both ask.</summary>
        internal bool IsSlanted => slant is FontSlant.Italic or FontSlant.Oblique;
    }
}
