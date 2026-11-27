// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Globalization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.MathText;

/// <summary>matplotlib's <c>'dejavusans'</c> math font set (<c>DejaVuSansFonts</c>), the one its default style sets math in, whatever
/// the label's family: a letter in DejaVu Sans Oblique (<c>mathtext.default</c> 'normal': a character of the Unicode letter classes
/// that is not a Greek capital), every other character in DejaVu Sans; under a font switch <c>\mathrm</c> in DejaVu Sans,
/// <c>\mathbf</c> in DejaVu Sans Bold, <c>\mathit</c> in DejaVu Sans Oblique. Whatever the switch, a character DejaVu Sans Display holds
/// (the large operators, a few quotes and dashes) is set in that face, a prime in Computer Modern's <c>cmsy10</c> (as matplotlib sets
/// <c>\prime</c> and an apostrophe), and a hyphen is the minus sign. What DejaVu Sans has no glyph for, and the calligraphic and
/// blackboard characters it has no face for (<c>\mathcal</c>, <c>\mathbb</c>), matplotlib takes from STIX: they are set where it
/// sets them, in a STIX face (<see cref="StixFallback.DejaVuSans"/>).</summary>
internal sealed partial class DejaVuSansMathFontSet() : GlyphMathFontSet(StixFallback.DejaVuSans, SizedTable())
{
    private const string Family = "DejaVu Sans";

    private static readonly MathFace Book = new(new Font { Family = Family });
    private static readonly MathFace Oblique = new(new Font { Family = Family, Slant = FontSlant.Italic });
    private static readonly MathFace Bold = new(new Font { Family = Family, Weight = FontWeight.Bold });
    private static readonly MathFace Display = new(new Font { Family = "DejaVu Sans Display" });

    /// <summary>The characters DejaVu Sans Display holds (its character map, read with matplotlib 3.11.2's FT2Font), which
    /// <c>DejaVuFonts._get_glyph</c> sets in it before any other face.</summary>
    private static readonly FrozenSet<char> DisplayHolds = FrozenSet.ToFrozenSet(
    [
        '–', '—', '‘', '’', '‚', '“', '”', '„', '•', '‹', '›',
        '∏', '∐', '∑', '∫', '∬', '∭', '∮', '∯', '∰', '∱', '∲', '∳',
        '⋀', '⋁', '⋂', '⋃', '⨀', '⨁', '⨂', '⨌', '⨍', '⨎', '⨏', '⨐',
        '⨑', '⨒', '⨓', '⨔', '⨕', '⨖', '⨗', '⨘', '⨙', '⨚', '⨛', '⨜',
    ]);

    /// <inheritdoc />
    internal override string Name => "dejavusans";

    /// <inheritdoc />
    internal override MathConstants Constants => MathConstants.DejaVuSans;

    /// <inheritdoc />
    private protected override MathGlyph? OwnGlyphOf(char c, FontVariant variant)
    {
        if (DisplayHolds.Contains(c)) return new(Display, c);
        if (c is '\'' or '′') return new(ComputerModernMathFontSet.Symbols, '0');
        MathFace? face = variant switch
        {
            FontVariant.Default => IsSetItalic(c) ? Oblique : Book,
            FontVariant.Roman => Book,
            FontVariant.Bold => Bold,
            FontVariant.Italic => Oblique,
            _ => null, // \mathcal and \mathbb: matplotlib has no face of its own for them, the STIX table (asked first) sets them
        };
        return face is { } own ? new(own, c == '-' ? TickLabelLook.TrueMinusSign : c) : null;
    }

    /// <summary>Whether matplotlib sets <paramref name="c"/> in the italic face when no switch is written (<c>UnicodeFonts._get_glyph</c>):
    /// a character of the letter classes whose Unicode name does not begin "GREEK CAPITAL". Those names are the capital and title-case
    /// letters of the Greek block and of Greek Extended, less the Greek symbols and Coptic letters of U+03D2 to U+03EF (checked against
    /// every name of the Basic Multilingual Plane).</summary>
    private static bool IsSetItalic(char c)
    {
        var category = CharUnicodeInfo.GetUnicodeCategory(c);
        if (category is not (UnicodeCategory.UppercaseLetter or UnicodeCategory.LowercaseLetter or UnicodeCategory.TitlecaseLetter
            or UnicodeCategory.ModifierLetter or UnicodeCategory.OtherLetter))
        {
            return false;
        }

        bool greekCapital = category is UnicodeCategory.UppercaseLetter or UnicodeCategory.TitlecaseLetter
            && (c is >= 'Ͱ' and <= 'Ͽ' and not (>= 'ϒ' and <= 'ϯ') || c is >= 'ἀ' and <= '῿');
        return !greekCapital;
    }
}
