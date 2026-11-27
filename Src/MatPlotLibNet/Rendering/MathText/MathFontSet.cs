// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Globalization;
using System.Text;
using MatPlotLibNet.Rendering.Text;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.MathText;

/// <summary>The faces math is set in under a theme (matplotlib's <c>mathtext.fontset</c>), a value of the theme's text look
/// (<see cref="TextLook.MathFonts"/>): which face each character of a span inside <c>$...$</c> is set in, and how that face codes it.
/// A span outside <c>$...$</c> (<see cref="TextSpan.OutsideMath"/>) is the label's text and stays in the label's face under every set.
/// The one place a run of math becomes the runs a surface shapes and a measure reads (<see cref="RunsOf"/>); each set is a leaf.</summary>
/// <remarks>A run is cut when its box is laid out; the label keeps its layout (<see cref="LineBox.LaidOut"/>), so a label measured and
/// drawn is cut once.</remarks>
internal abstract class MathFontSet
{
    /// <summary>The library's own: math in the label's face.</summary>
    internal static MathFontSet Library { get; } = new LibraryMathFontSet();

    /// <summary>matplotlib's <c>'cm'</c>: math in the Computer Modern faces, as matplotlib's classic style sets it.</summary>
    internal static MathFontSet ComputerModern { get; } = new ComputerModernMathFontSet();

    /// <summary>matplotlib's <c>'dejavusans'</c>: math in DejaVu Sans, its letters oblique, as matplotlib's default style sets it.</summary>
    internal static MathFontSet DejaVuSans { get; } = new DejaVuSansMathFontSet();

    /// <summary>The set's name as matplotlib's <c>mathtext.fontset</c> names it, which the figure JSON carries; <see langword="null"/>
    /// for the library's own.</summary>
    internal abstract string? Name { get; }

    /// <summary>How big math is set and where its parts stand in this set's faces (matplotlib's font constants for the set).</summary>
    internal abstract MathConstants Constants { get; }

    /// <summary>The glyphs matplotlib's <c>AutoHeightChar</c> chooses among to set a delimiter (after <c>\left</c>, <c>\middle</c> or
    /// <c>\right</c>) or a radical sign as tall as what it stands beside, the size of the character itself first: the sizes the set's
    /// faces hold it in (<c>get_sized_alternatives_for_symbol</c>). None for a character the set has no sizes for, and for a set with no
    /// sized faces (the library's own), whose layout grows the character's own glyph instead (<see cref="SizedGlyph"/>).</summary>
    /// <param name="glyph">The delimiter or the sign, as the layout hands it.</param>
    /// <returns>The sizes, smallest first; <see cref="MathSizes.None"/> for none.</returns>
    internal abstract MathSizes SizedAlternatives(string glyph);

    /// <summary>The set <paramref name="name"/> names (<see cref="Name"/>); the library's own for a name no set has.</summary>
    /// <param name="name">A name as <see cref="Name"/> gives it, or <see langword="null"/>.</param>
    /// <returns>The set.</returns>
    internal static MathFontSet Named(string? name) => name switch
    {
        "cm" => ComputerModern,
        "dejavusans" => DejaVuSans,
        _ => Library,
    };

    /// <summary>The runs <paramref name="text"/> is set in: the label's text as one run in the label's face when it stands outside
    /// math, else as this set sets math under the font switch <paramref name="variant"/>.</summary>
    /// <param name="text">The characters.</param>
    /// <param name="variant">Their font switch.</param>
    /// <param name="outsideMath">Whether they are the label's text outside <c>$...$</c>.</param>
    /// <returns>The runs.</returns>
    internal MathRuns RunsOf(string text, FontVariant variant, bool outsideMath) =>
        outsideMath ? new MathRuns(new MathRun(text, MathFace.Label, 0)) : RunsOfMath(text, variant);

    /// <summary>The runs characters inside math are set in.</summary>
    /// <param name="text">The characters, inside <c>$...$</c>.</param>
    /// <param name="variant">Their font switch.</param>
    private protected abstract MathRuns RunsOfMath(string text, FontVariant variant);
}

/// <summary>What a character is set as: a face and the code of the character in it.</summary>
/// <param name="Face">The face.</param>
/// <param name="Code">The character as the face codes it, a code point: a face of the Basic Multilingual Plane, a TeX-coded face
/// (a character code below 256) or a mathematical alphanumeric of a STIX face (above U+FFFF).</param>
internal readonly record struct MathGlyph(MathFace Face, int Code);

/// <summary>The glyphs matplotlib's <c>AutoHeightChar</c> chooses among for a delimiter or a radical sign.</summary>
/// <param name="Glyphs">The sizes, smallest first.</param>
/// <param name="FirstIsNatural">Whether the first size is taken at its own size when it is the one chosen (matplotlib's size <c>'0'</c> of a
/// set that has other sizes), where every other choice is set at the size that makes it as tall as what it stands beside.</param>
internal readonly record struct MathSizes(MathGlyph[] Glyphs, bool FirstIsNatural)
{
    /// <summary>No sizes: the character's own glyph is grown instead (<see cref="SizedGlyph"/>).</summary>
    internal static MathSizes None { get; } = new([], false);
}

/// <summary>A set that chooses a face for each character of a span and cuts the span where the face changes. It is made of what
/// matplotlib's own font set is made of: the faces of its own (<see cref="OwnGlyphOf"/>) and, composed, the STIX faces matplotlib takes
/// what the set has no glyph for from (<see cref="StixFallback"/>), asked first because the table holds a character only where
/// matplotlib's parse set it in STIX. A character no face holds (matplotlib sets a dummy; a label's text may be in a script no
/// matplotlib face holds) is set in the label's face, here and nowhere else. A character with the combining marks after it (an accent
/// the parser writes as a combining character) and a surrogate pair stay together, in the face of their first character when that face
/// codes characters as Unicode does, else in the label's.</summary>
/// <param name="fallback">The STIX glyphs the set falls back to.</param>
/// <param name="sizes">The glyphs a delimiter or a radical sign is chosen among, by the character the layout hands over
/// (<see cref="SizedAlternatives"/>).</param>
internal abstract class GlyphMathFontSet(StixFallback fallback, FrozenDictionary<char, MathSizes> sizes) : MathFontSet
{
    /// <inheritdoc />
    internal sealed override MathSizes SizedAlternatives(string glyph) =>
        glyph.Length == 1 && sizes.TryGetValue(glyph[0], out var alternatives) ? alternatives : MathSizes.None;

    /// <summary>The face and the code of <paramref name="c"/> inside math in the faces of the set itself, under the font switch
    /// <paramref name="variant"/>: <see langword="null"/> when none of them holds it.</summary>
    /// <param name="c">The character.</param>
    /// <param name="variant">The font switch of its run.</param>
    private protected abstract MathGlyph? OwnGlyphOf(char c, FontVariant variant);

    /// <inheritdoc />
    /// <remarks>Cut into runs of one face each. A run whose characters are coded as written is the text itself, no copy; a text of one
    /// run is held inline, no array.</remarks>
    private protected sealed override MathRuns RunsOfMath(string text, FontVariant variant)
    {
        List<MathRun>? runs = null;
        StringBuilder? codes = null;
        var face = MathFace.Label;
        int start = 0;
        for (int i = 0; i < text.Length;)
        {
            int end = ClusterEnd(text, i);
            var glyph = fallback.GlyphOf(text[i], variant) ?? OwnGlyphOf(text[i], variant) ?? new MathGlyph(MathFace.Label, text[i]);
            var clusterFace = end - i == 1 || !glyph.Face.CodesByTeX ? glyph.Face : MathFace.Label;
            if (i > 0 && clusterFace != face)
            {
                (runs ??= new List<MathRun>(3)).Add(RunOf(text, codes, face, start, i));
                codes?.Clear();
                start = i;
            }

            face = clusterFace;
            if (end - i == 1 && glyph.Code != text[i] && codes is null)
            {
                codes = new StringBuilder(text.Length);
                codes.Append(text, start, i - start); // the characters of this run before the first one coded otherwise
            }

            if (codes is not null)
            {
                if (end - i != 1) codes.Append(text, i, end - i);
                else if (glyph.Code <= char.MaxValue) codes.Append((char)glyph.Code);
                else codes.Append(char.ConvertFromUtf32(glyph.Code)); // a mathematical alphanumeric: two UTF-16 units for one character
            }

            i = end;
        }

        var last = RunOf(text, codes, face, start, text.Length);
        if (runs is null) return new MathRuns(last);
        runs.Add(last);
        return new MathRuns([.. runs]);
    }

    /// <summary>The run of the characters from <paramref name="start"/> to <paramref name="end"/>: their codes when one is coded
    /// otherwise (<paramref name="codes"/>), else the characters as written, the text itself when the run is all of it.</summary>
    private static MathRun RunOf(string text, StringBuilder? codes, MathFace face, int start, int end) => new(
        codes is { Length: > 0 } ? codes.ToString() : start == 0 && end == text.Length ? text : text[start..end], face, start);

    /// <summary>Where the cluster starting at <paramref name="i"/> ends: past a surrogate pair, and past the combining marks after it.</summary>
    private static int ClusterEnd(string text, int i)
    {
        int end = i + text.UnitsAt(i);
        while (end < text.Length && CharUnicodeInfo.GetUnicodeCategory(text[end]) is UnicodeCategory.NonSpacingMark
                   or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark)
        {
            end++;
        }

        return end;
    }
}

/// <summary>The library's own set: math in the label's face, as one run per span; a font switch the caller wrote (a value the caller
/// gave, which wins under every theme) in the label's family: <c>\mathbf</c> bold, <c>\mathit</c> italic. The set has no calligraphic
/// or blackboard face of its own, so <c>\mathcal</c> and <c>\mathbb</c> stay in the label's face, as <c>\mathrm</c> does.</summary>
internal sealed class LibraryMathFontSet : MathFontSet
{
    private static readonly MathFace Bold = new(null, Weight: FontWeight.Bold);
    private static readonly MathFace Italic = new(null, Slant: FontSlant.Italic);

    /// <inheritdoc />
    internal override string? Name => null;

    /// <inheritdoc />
    internal override MathConstants Constants => MathConstants.Library;

    /// <inheritdoc />
    internal override MathSizes SizedAlternatives(string glyph) => MathSizes.None;

    /// <inheritdoc />
    private protected override MathRuns RunsOfMath(string text, FontVariant variant) => new(new MathRun(text, variant switch
    {
        FontVariant.Bold => Bold,
        FontVariant.Italic => Italic,
        _ => MathFace.Label,
    }, 0));
}
