// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.MathText;

/// <summary>A fraction (matplotlib's <c>Parser._genfrac</c>, TeX's nodes 743 to 746): the numerator over the denominator, each centred on
/// the wider, shrunk once for each level of its style; with a rule, the rule on the math axis and the numerator and denominator raised
/// and dropped by the set's <see cref="MathConstants.Num2"/> and <see cref="MathConstants.Denom2"/> (in display style
/// <see cref="MathConstants.Num1"/> and <see cref="MathConstants.Denom1"/>) but at least a rule clear of it; without one
/// (<c>\binom</c>), by <see cref="MathConstants.Num3"/> and kept three rules apart. A rule's width of padding on each side.</summary>
/// <param name="numerator">The numerator.</param>
/// <param name="denominator">The denominator.</param>
/// <param name="rule">The thickness of the rule in pixels; <see langword="null"/>: the font's rule; 0: none.</param>
/// <param name="styleLevel">matplotlib's math style: 0 display (<c>\dfrac</c>), 1 text (<c>\frac</c>), 2 script, 3 script-script; the
/// parts shrink once for each level.</param>
internal sealed class FractionBox(MathBox numerator, MathBox denominator, double? rule, int styleLevel) : MathBox
{
    /// <inheritdoc />
    internal override MathExtent Lay(MathInk ink, in MathStyle style)
    {
        var c = style.Constants;
        double thickness = style.Rule, line = rule ?? thickness, axis = style.AxisHeight, xHeight = style.XHeight;
        var parts = style;
        for (int i = 0; i < styleLevel; i++)
        {
            xHeight *= c.ShrinkFactor;
            parts = parts.Smaller;
        }

        var numFrom = ink.Mark;
        var num = numerator.Lay(ink, parts);
        var denFrom = ink.Mark;
        var den = denominator.Lay(ink, parts);
        var end = ink.Mark;
        double width = Math.Max(num.Width, den.Width);

        bool display = styleLevel == 0;
        double numBaseline, denBaseline;
        if (line > 0)
        {
            double numShift = (display ? c.Num1 : c.Num2) * xHeight, denShift = (display ? c.Denom1 : c.Denom2) * xHeight;
            double clearance = display ? 3 * line : line, half = line / 2;
            double numClear = Math.Max(numShift - num.Depth - (axis + half), clearance);
            double denClear = Math.Max(axis - half - (den.Height - denShift), clearance);
            denBaseline = den.Height + denClear + half - axis;
            double ruleTop = denBaseline - den.Height - denClear - line;
            numBaseline = ruleTop - numClear - num.Depth;
            ink.AddRule(thickness, ruleTop, width, line);
        }
        else
        {
            double numShift = (display ? c.Num1 : c.Num3) * xHeight, denShift = (display ? c.Denom1 : c.Denom2) * xHeight;
            double least = (display ? 7 : 3) * thickness;
            double natural = numShift - num.Depth - (den.Height - denShift);
            double clearance = Math.Max(natural, least);
            denBaseline = denShift + (natural < least ? (least - natural) / 2 : 0);
            numBaseline = denBaseline - den.Height - clearance - num.Depth;
        }

        ink.Shift(numFrom, denFrom, thickness + CentredOffset(width, num.Width), numBaseline);
        ink.Shift(denFrom, end, thickness + CentredOffset(width, den.Width), denBaseline);
        return new MathExtent(width + 2 * thickness, num.Height - numBaseline, den.Depth + denBaseline);
    }

    /// <inheritdoc />
    internal override bool IsCharNode => false;

    /// <inheritdoc />
    internal override void Flatten(MathSpanWriter writer)
    {
        writer.Part(numerator, TextSpanKind.FractionNumerator, styleLevel);
        writer.Part(denominator, TextSpanKind.FractionDenominator, styleLevel);
    }
}
