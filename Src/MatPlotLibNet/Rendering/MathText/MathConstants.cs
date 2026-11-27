// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.MathText;

/// <summary>How big math is set and where its parts stand, for one math font set (<see cref="MathFontSet.Constants"/>): matplotlib's
/// font constants (<c>matplotlib._mathtext.FontConstantsBase</c> and its leaves, 3.11.2), which the layout (<see cref="MathBox"/>)
/// reads as TeX reads the parameters of its math fonts. The constants of a script's raise and drop, a fraction's shifts and the space
/// after a script are parts of the x-height (<see cref="XHeight"/>); the x-height, the axis height and the quad are parts of the em (the
/// font size). The one owner of every size and place in math: no leaf of the layout types a number of an em of its own.</summary>
internal sealed record MathConstants
{
    /// <summary>matplotlib's <c>'cm'</c> constants (<c>ComputerModernFontConstants</c>): read from <c>cmsy10.tfm</c>, divided by its
    /// design x-height.</summary>
    internal static MathConstants ComputerModern { get; } = new()
    {
        ScriptSpace = 0.132861328125,
        Delta = 0.132861328125,
        DeltaSlanted = 0.3,
        DeltaIntegral = 0.3,
        SupDrop = 404864.0 / CmXHeight,
        SubDrop = 52429.0 / CmXHeight,
        Sup1 = 432949.0 / CmXHeight,
        Sub1 = 157286.0 / CmXHeight,
        Sub2 = 259226.0 / CmXHeight,
        Num1 = 709370.0 / CmXHeight,
        Num2 = 412858.0 / CmXHeight,
        Num3 = 465286.0 / CmXHeight,
        Denom1 = 719272.0 / CmXHeight,
        Denom2 = 361592.0 / CmXHeight,
        AxisHeight = 262144.0 / (1 << 20),
        Quad = 1048579.0 / (1 << 20),
        XHeight = CmXHeight / (1 << 20),
    };

    /// <summary>matplotlib's <c>'dejavusans'</c> constants (<c>DejaVuSansFontConstants</c>): read from the TeX table of DejaVuSans.ttf,
    /// divided by its design x-height; the quad, which the table does not give, is the advance of an <c>m</c> in DejaVu Sans Oblique,
    /// which matplotlib measures for it (<c>TruetypeFonts.get_quad</c>).</summary>
    internal static MathConstants DejaVuSans { get; } = new()
    {
        SupDrop = 790.527 / DejaVuXHeight,
        SubDrop = 102.4 / DejaVuXHeight,
        Sup1 = 845.824 / DejaVuXHeight,
        Sub1 = 307.199 / DejaVuXHeight,
        Sub2 = 632.832 / DejaVuXHeight,
        Num1 = 1529.86 / DejaVuXHeight,
        Num2 = 868.352 / DejaVuXHeight,
        Num3 = 970.752 / DejaVuXHeight,
        Denom1 = 1548.29 / DejaVuXHeight,
        Denom2 = 768 / DejaVuXHeight,
        AxisHeight = 512.0 / 2048,
        Quad = 1995.0 / 2048,
        XHeight = DejaVuXHeight / 2048,
    };

    /// <summary>The library's own (math in the label's face): DejaVu Sans' constants, the face the library sets its text in, with the large
    /// operators drawn larger than the text, which a face of its own draws them in under the other sets.</summary>
    internal static MathConstants Library { get; } = DejaVuSans with { LargeOperatorScale = 1.4 };

    /// <summary>The x-height of <c>cmsy10.tfm</c>, in its design units of 2^20 to the em.</summary>
    private const double CmXHeight = 451470;

    /// <summary>The x-height of DejaVu Sans, in its units of 2048 to the em.</summary>
    private const double DejaVuXHeight = 1120;

    /// <summary>What a script, a fraction's parts and a radical's index are set at against the text they belong to, at each level
    /// (matplotlib's <c>SHRINK_FACTOR</c>).</summary>
    internal double ShrinkFactor { get; init; } = 0.7;

    /// <summary>The size <paramref name="levels"/> levels down against the text's: the shrink factor that many times over (a script is one
    /// level down, so its size is the factor itself, with no power worked out).</summary>
    /// <param name="levels">How many levels down, at least one.</param>
    internal double ShrunkBy(int levels) => levels == 1 ? ShrinkFactor : Math.Pow(ShrinkFactor, levels);

    /// <summary>How many sizes there are (matplotlib's <c>NUM_SIZE_LEVELS</c>): the text and the levels it shrinks by; a level past
    /// the last is set at the last size.</summary>
    internal int SizeLevels { get; init; } = 6;

    /// <summary>The thickness of a fraction's rule and a radical's bar, in ems (matplotlib's <c>get_underline_thickness</c>, 0.75
    /// points at a size of 12).</summary>
    internal double RuleThickness { get; init; } = 0.75 / 12;

    /// <summary>The space after a script, in x-heights.</summary>
    internal double ScriptSpace { get; init; } = 0.05;

    /// <summary>How far a superscript of a box that is no character stands below the box's top, in x-heights of the script.</summary>
    internal double SupDrop { get; init; } = 0.4;

    /// <summary>How far a subscript of a box that is no character, or of an integral, stands below the box's bottom, in x-heights.</summary>
    internal double SubDrop { get; init; } = 0.4;

    /// <summary>The least raise of a superscript, in x-heights.</summary>
    internal double Sup1 { get; init; } = 0.7;

    /// <summary>The least drop of a subscript alone, in x-heights.</summary>
    internal double Sub1 { get; init; } = 0.3;

    /// <summary>The least drop of a subscript under a superscript, in x-heights.</summary>
    internal double Sub2 { get; init; } = 0.5;

    /// <summary>How far a script stands from an upright nucleus, in x-heights.</summary>
    internal double Delta { get; init; } = 0.025;

    /// <summary>How far more a superscript stands from a slanted nucleus, per x-height its last character rises above two thirds of
    /// the x-height.</summary>
    internal double DeltaSlanted { get; init; } = 0.2;

    /// <summary>How far an integral's scripts stand apart, per height of the integral sign.</summary>
    internal double DeltaIntegral { get; init; } = 0.1;

    /// <summary>The raise of a numerator in display style, in x-heights.</summary>
    internal double Num1 { get; init; } = 1.4;

    /// <summary>The raise of a numerator over a rule in text style, in x-heights.</summary>
    internal double Num2 { get; init; } = 1.5;

    /// <summary>The raise of a numerator with no rule in text style, in x-heights.</summary>
    internal double Num3 { get; init; } = 1.3;

    /// <summary>The drop of a denominator in display style, in x-heights.</summary>
    internal double Denom1 { get; init; } = 1.3;

    /// <summary>The drop of a denominator in text style, in x-heights.</summary>
    internal double Denom2 { get; init; } = 1.1;

    /// <summary>The height of the math axis (the middle of a minus sign, where a fraction's rule stands), in ems.</summary>
    internal double AxisHeight { get; init; }

    /// <summary>The quad, the em of math spaces (<c>\quad</c> is one), in ems.</summary>
    internal double Quad { get; init; } = 1;

    /// <summary>The x-height, in ems.</summary>
    internal double XHeight { get; init; }

    /// <summary>How much larger than the text a large operator (<c>\sum</c>, <c>\int</c>) is set.</summary>
    internal double LargeOperatorScale { get; init; } = 1;

    /// <summary>The least height above the baseline of a row of a matrix, in ems: LaTeX's array strut, 0.7 of a baseline skip of 1.2
    /// ems (matplotlib has no matrix).</summary>
    internal double MatrixStrutHeight { get; init; } = 0.84;

    /// <summary>The least depth below the baseline of a row of a matrix, in ems: 0.3 of a baseline skip of 1.2 ems.</summary>
    internal double MatrixStrutDepth { get; init; } = 0.36;

    /// <summary>The space between two columns of a matrix, in ems: LaTeX's two <c>\arraycolsep</c> of 5 points at 10 points.</summary>
    internal double MatrixColumnGap { get; init; } = 1;

    /// <summary>How much shorter than asked a size of a delimiter may be and still be taken, in x-heights (matplotlib's
    /// <c>AutoHeightChar</c>: <c>target_total - 0.2 * xHeight</c>, so a text of the regular size with a descender keeps the first size).</summary>
    internal double DelimiterShortfall { get; init; } = 0.2;
}
