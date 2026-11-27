// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.MathText;

/// <summary>A face of a math font set: a font of its own (Computer Modern's <c>cmmi10</c>, DejaVu Sans Oblique), or the label's font,
/// in a weight or a slant of its own when one is given.</summary>
/// <param name="Own">The face's own font, whose family, weight and slant the run is set in; <see langword="null"/> for the label's.</param>
/// <param name="Weight">The weight the label's font is set in, when the face is the label's; <see langword="null"/> keeps the label's.</param>
/// <param name="Slant">The slant the label's font is set in, when the face is the label's; <see langword="null"/> keeps the label's.</param>
/// <param name="CodesByTeX">Whether the face codes its characters by their place in a TeX font (the BaKoMa Computer Modern faces), not
/// as Unicode does: it holds no combining mark, so an accented letter is not set in it.</param>
internal readonly record struct MathFace(Font? Own, FontWeight? Weight = null, FontSlant? Slant = null, bool CodesByTeX = false)
{
    /// <summary>The label's font itself.</summary>
    internal static MathFace Label => default;

    /// <summary>Whether matplotlib raises the glyphs of the face by half of their ink and a third of the size (its <c>Cmex10</c>
    /// offset: the extension face's signs hang below the baseline, and the box says they stand above it): <see cref="MathStyle.Run"/> reads it.</summary>
    internal bool RaisesInk { get; init; }

    /// <summary>Whether the face's letters lean although its font names no slant (Computer Modern's math italic, <c>cmmi10</c>).</summary>
    internal bool Leans { get; init; }

    /// <summary>Whether a glyph of the face leans right, so a superscript stands further from it (matplotlib's <c>FontMetrics.slanted</c>):
    /// a face whose slant leans (italic or oblique, the one rule every surface draws a slant by), or one that leans by design
    /// (<see cref="Leans"/>).</summary>
    internal bool Slanted => Leans || (Own?.Slant ?? Slant) is { } slant && slant.IsSlanted;

    /// <summary>The font of this face for a label set in <paramref name="label"/>: the face's own font, else the label's in this face's
    /// weight and slant (the label's font itself when the face gives neither).</summary>
    /// <param name="label">The font of the whole label.</param>
    /// <returns>The font of the run.</returns>
    internal Font In(Font label) => Own ?? (Weight is null && Slant is null
        ? label
        : label with { Weight = Weight ?? label.Weight, Slant = Slant ?? label.Slant });
}

/// <summary>A run of one span set in one face: the span's characters from <paramref name="Start"/> as the face codes them, one code
/// for each character, so a run stands where its characters stand in the span.</summary>
/// <param name="Text">The characters as the face codes them.</param>
/// <param name="Face">The face.</param>
/// <param name="Start">Where the run starts in the span's text.</param>
internal readonly record struct MathRun(string Text, MathFace Face, int Start);

/// <summary>The runs one span is set in: one, held inline, or several, in the order they are written.</summary>
internal readonly struct MathRuns
{
    private readonly MathRun _one;
    private readonly MathRun[]? _many;

    /// <summary>A span set as one run.</summary>
    /// <param name="one">The run.</param>
    internal MathRuns(MathRun one) => _one = one;

    /// <summary>A span set as <paramref name="many"/> runs.</summary>
    /// <param name="many">The runs, in the order they are written.</param>
    internal MathRuns(MathRun[] many) => _many = many;

    /// <summary>How many runs there are.</summary>
    internal int Count => _many?.Length ?? 1;

    /// <summary>The run at <paramref name="index"/>.</summary>
    /// <param name="index">The place of the run.</param>
    internal MathRun this[int index] => _many is null ? _one : _many[index];
}
