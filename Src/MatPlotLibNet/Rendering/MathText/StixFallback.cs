// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.MathText;

/// <summary>The STIX faces matplotlib's font sets take a character from when the set has no glyph for it (<c>BakomaFonts</c> falls back
/// to <c>StixFonts</c>, <c>DejaVuFonts</c> to <c>StixSansFonts</c>), and the taller sizes <c>DejaVuFonts</c> grows a delimiter in, as a
/// member of this value.</summary>
internal enum StixFace : byte
{
    /// <summary>STIXGeneral, upright.</summary>
    Regular,

    /// <summary>STIXGeneral, italic.</summary>
    Italic,

    /// <summary>STIXGeneral, bold.</summary>
    Bold,

    /// <summary>STIXNonUnicode, upright: the private-use code points of the characters STIXGeneral has no Unicode place for.</summary>
    NonUnicode,

    /// <summary>STIXNonUnicode, italic: the script and blackboard capitals.</summary>
    NonUnicodeItalic,

    /// <summary>STIXSizeOneSym: the first size of a delimiter or radical sign.</summary>
    SizeOne,

    /// <summary>STIXSizeTwoSym: the second size.</summary>
    SizeTwo,

    /// <summary>STIXSizeThreeSym: the third size.</summary>
    SizeThree,

    /// <summary>STIXSizeFourSym: the fourth size.</summary>
    SizeFour,
}

/// <summary>The STIX glyphs a math font set takes what it has no glyph for from, as matplotlib's set does: a table of every character
/// the grammar can put in math, read from matplotlib's parser under the set (<c>StixFallback.Data.cs</c>, written by
/// <c>tools/mpl_reference/generate_mathtext.py</c>), and the one place the STIX faces are named. A set composes the value for its own
/// table (<see cref="ComputerModern"/>, <see cref="DejaVuSans"/>); there is no leaf per pair of sets. The tables are frozen and the
/// faces are fixed values: the value holds no state a draw changes.</summary>
internal sealed partial class StixFallback
{
    private static readonly MathFace Regular = Face(FontWeight.Normal, FontSlant.Normal, "STIXGeneral");
    private static readonly MathFace Italic = Face(FontWeight.Normal, FontSlant.Italic, "STIXGeneral");
    private static readonly MathFace Bold = Face(FontWeight.Bold, FontSlant.Normal, "STIXGeneral");
    private static readonly MathFace NonUnicode = Face(FontWeight.Normal, FontSlant.Normal, "STIXNonUnicode");
    private static readonly MathFace NonUnicodeItalic = Face(FontWeight.Normal, FontSlant.Italic, "STIXNonUnicode");
    private static readonly MathFace SizeOne = Face(FontWeight.Normal, FontSlant.Normal, "STIXSizeOneSym");
    private static readonly MathFace SizeTwo = Face(FontWeight.Normal, FontSlant.Normal, "STIXSizeTwoSym");
    private static readonly MathFace SizeThree = Face(FontWeight.Normal, FontSlant.Normal, "STIXSizeThreeSym");
    private static readonly MathFace SizeFour = Face(FontWeight.Normal, FontSlant.Normal, "STIXSizeFourSym");

    /// <summary>What matplotlib takes from STIX under <c>mathtext.fontset 'cm'</c>, the classic style.</summary>
    internal static StixFallback ComputerModern { get; } = new(ComputerModernRows());

    /// <summary>What matplotlib takes from STIX under <c>mathtext.fontset 'dejavusans'</c>, the default style.</summary>
    internal static StixFallback DejaVuSans { get; } = new(DejaVuSansRows());

    private readonly FrozenDictionary<char, StixRow> _rows;

    private StixFallback(FrozenDictionary<char, StixRow> rows) => _rows = rows;

    /// <summary>The glyph matplotlib takes from STIX for <paramref name="c"/> under the font switch <paramref name="variant"/>.</summary>
    /// <param name="c">The character.</param>
    /// <param name="variant">The font switch of its run.</param>
    /// <returns>The face and the code, or <see langword="null"/> when the set holds the character itself or no face holds it.</returns>
    internal MathGlyph? GlyphOf(char c, FontVariant variant)
    {
        if (!_rows.TryGetValue(c, out var row))
        {
            return null;
        }

        var glyph = row[variant];
        return glyph.IsSet ? new MathGlyph(FaceOf(glyph.Face), glyph.Code) : null;
    }

    private static MathFace Face(FontWeight weight, FontSlant slant, string family) => new(new Font { Family = family, Weight = weight, Slant = slant });

    /// <summary>The face <paramref name="face"/> names.</summary>
    /// <param name="face">A STIX face.</param>
    internal static MathFace FaceOf(StixFace face) => face switch
    {
        StixFace.Regular => Regular,
        StixFace.Italic => Italic,
        StixFace.Bold => Bold,
        StixFace.NonUnicode => NonUnicode,
        StixFace.SizeOne => SizeOne,
        StixFace.SizeTwo => SizeTwo,
        StixFace.SizeThree => SizeThree,
        StixFace.SizeFour => SizeFour,
        _ => NonUnicodeItalic,
    };

    private static StixGlyph R(int code) => new(StixFace.Regular, code);

    private static StixGlyph I(int code) => new(StixFace.Italic, code);

    private static StixGlyph B(int code) => new(StixFace.Bold, code);

    private static StixGlyph N(int code) => new(StixFace.NonUnicode, code);

    private static StixGlyph NI(int code) => new(StixFace.NonUnicodeItalic, code);

    /// <summary>What matplotlib's parse set one character in, under each font switch (<see cref="FontVariant"/>), in a STIX face: a glyph
    /// where it did, the default glyph where the font set holds the character itself.</summary>
    /// <param name="Default">The glyph under no font switch.</param>
    /// <param name="Roman">The glyph under <c>\mathrm</c> and <c>\text</c>.</param>
    /// <param name="Bold">The glyph under <c>\mathbf</c>.</param>
    /// <param name="Italic">The glyph under <c>\mathit</c>.</param>
    /// <param name="Calligraphic">The glyph under <c>\mathcal</c>.</param>
    /// <param name="BlackboardBold">The glyph under <c>\mathbb</c>.</param>
    private readonly record struct StixRow(StixGlyph Default, StixGlyph Roman, StixGlyph Bold, StixGlyph Italic, StixGlyph Calligraphic,
        StixGlyph BlackboardBold)
    {
        /// <summary>The glyph under the font switch <paramref name="variant"/>.</summary>
        /// <param name="variant">The font switch.</param>
        internal StixGlyph this[FontVariant variant] => variant switch
        {
            FontVariant.Default => Default,
            FontVariant.Roman => Roman,
            FontVariant.Bold => Bold,
            FontVariant.Italic => Italic,
            FontVariant.Calligraphic => Calligraphic,
            _ => BlackboardBold,
        };
    }

    /// <summary>A character as a STIX face sets it: the face and the code of the character in it.</summary>
    /// <param name="Face">The face.</param>
    /// <param name="Code">The character as the face codes it, a code point (a mathematical alphanumeric is above U+FFFF).</param>
    private readonly record struct StixGlyph(StixFace Face, int Code)
    {
        /// <summary>Whether a STIX face sets the character: the default value of a glyph is <see cref="StixFace.Regular"/> code 0, which
        /// no character has.</summary>
        internal bool IsSet => Code != 0;
    }
}
