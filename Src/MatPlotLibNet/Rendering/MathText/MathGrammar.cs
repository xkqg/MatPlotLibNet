// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.MathText;

/// <summary>The grammar of math text, as matplotlib's mathtext reads it: every symbol name and the character it stands for, the
/// function names, the accents, the spaces and their widths, the large operators, and the symbols matplotlib puts space around.
/// The tables are generated from matplotlib's own parser (<c>tools/mpl_reference/generate_mathtext.py</c> writes
/// <c>MathGrammar.Data.cs</c>); the command table (<see cref="MathCommand"/>) registers its families from them and a symbol is
/// looked up in them (<see cref="MathSymbol"/>), so a name is known in one place. What the parse makes of them is a tree of boxes
/// (<see cref="MathBox"/>); the sizes and places of math are the font set's (<see cref="MathConstants"/>).</summary>
internal static partial class MathGrammar
{
    /// <summary>The Unicode spaces of a fixed width (The Unicode Standard, chapter 6.2) a space narrower than an em is written as,
    /// with their widths in ems, narrowest first; nothing is the space of no width.</summary>
    private static readonly string[] NarrowSpaces = ["", " ", " ", " ", " ", " ", " ", " "];

    /// <summary>The widths of <see cref="NarrowSpaces"/>, in ems.</summary>
    private static readonly double[] NarrowWidths = [0, 1.0 / 6, 1.0 / 5, 4.0 / 18, 1.0 / 4, 1.0 / 3, 1.0 / 2, 1];

    /// <summary>The text a space of <paramref name="em"/> ems is written as in the flat view of a label (<see cref="MathSpanWriter"/>;
    /// the layout sets the space at its exact width): an em space for each whole em, then the Unicode space whose width is nearest the
    /// rest. A space of no width or less (<c>\!</c> takes room away) is written as nothing, since a run of text cannot step back.</summary>
    /// <param name="em">The width in ems.</param>
    /// <returns>The spaces.</returns>
    internal static string SpaceOf(double em)
    {
        if (em <= 0) return string.Empty;

        int whole = (int)em;
        double rest = em - whole;
        int nearest = 0;
        for (int i = 1; i < NarrowWidths.Length; i++)
        {
            if (Math.Abs(NarrowWidths[i] - rest) < Math.Abs(NarrowWidths[nearest] - rest)) nearest = i;
        }

        return new string(' ', whole) + NarrowSpaces[nearest];
    }

    /// <summary>What the character <paramref name="c"/> is to the spacing of math.</summary>
    internal static MathSymbolRole RoleOf(char c) =>
        BinaryCharacters.Contains(c) ? MathSymbolRole.Binary
        : SpacedCharacters.Contains(c) ? MathSymbolRole.Relation
        : PunctuationCharacters.Contains(c) ? MathSymbolRole.Punctuation
        : MathSymbolRole.Ordinary;

    /// <summary>The spaces matplotlib puts around a symbol (<c>Parser.symbol</c>): around a binary operator, a relation and an arrow,
    /// but none in the scripts of a function name, and none around a binary operator written first or after an opening brace, an
    /// opening delimiter or a relation; after punctuation, but none after a comma alone in braces or a point between digits.</summary>
    /// <param name="role">What the symbol is to the spacing.</param>
    /// <param name="written">The symbol as written, when it is one character; <c>'\0'</c> for a name.</param>
    /// <param name="before">The character written before it, spaces skipped (<see cref="MathSource.Before"/>).</param>
    /// <param name="after">The character written after it, spaces skipped (<see cref="MathSource.After"/>).</param>
    /// <param name="unspaced">Whether symbols take no space where this one stands: the scripts of a function name, a delimiter.</param>
    /// <returns>Where the symbol takes a space.</returns>
    internal static MathSpaces SpacesAround(MathSymbolRole role, char written, char before, char after, bool unspaced) => role switch
    {
        MathSymbolRole.Binary when unspaced || before == '\0' || UnspacedAfter.Contains(before) => default,
        MathSymbolRole.Binary or MathSymbolRole.Relation when !unspaced => new(true, true),
        MathSymbolRole.Punctuation when written == ',' && before == '{' && after == '}' => default,
        MathSymbolRole.Punctuation when written == '.' && char.IsDigit(before) && char.IsDigit(after) => default,
        MathSymbolRole.Punctuation => new(false, true),
        _ => default,
    };
}

/// <summary>A symbol of the grammar (<see cref="MathGrammar.Symbols"/>): the character its name is set as and what it is to the
/// spacing of math, read in one lookup.</summary>
/// <param name="Glyph">The character.</param>
/// <param name="Role">What the symbol is to the spacing.</param>
internal readonly record struct MathSymbolEntry(string Glyph, MathSymbolRole Role = MathSymbolRole.Ordinary);

/// <summary>What a symbol is to the spacing of math (matplotlib's <c>Parser._binary_operators</c>, <c>_relation_symbols</c> and
/// <c>_arrow_symbols</c>, <c>_punctuation_symbols</c>).</summary>
internal enum MathSymbolRole
{
    /// <summary>No space of its own.</summary>
    Ordinary,

    /// <summary>A binary operator: space around it, but not where it stands first or after an opening.</summary>
    Binary,

    /// <summary>A relation or an arrow: space around it.</summary>
    Relation,

    /// <summary>Punctuation: space after it.</summary>
    Punctuation,
}

/// <summary>Where a symbol takes the space matplotlib puts around it (<see cref="MathGrammar.SymbolSpaceWidth"/> ems), which the parse
/// sets as a space of that width on each side it names (<see cref="MathTextCursor.AddSpaced"/>).</summary>
/// <param name="Before">Whether a space stands before the symbol.</param>
/// <param name="After">Whether a space stands after it.</param>
internal readonly record struct MathSpaces(bool Before, bool After);
