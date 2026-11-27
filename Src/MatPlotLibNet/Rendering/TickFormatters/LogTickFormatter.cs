// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.TickFormatters;

/// <summary>Formats tick values for logarithmic and symmetric-log axes in scientific notation with superscripts, as matplotlib's
/// <c>LogFormatterSciNotation</c> writes them: <c>10²</c> for a power of ten, <c>2×10²</c> for a value off one, <c>0</c> for zero and
/// a value below zero with the minus sign before it (<c>−10⁵</c>), the sign matplotlib's mathtext draws. On an axis it labels, as
/// matplotlib's <c>set_locs</c> does, only the ticks whose coefficient the view names: the powers alone over more than one power of ten,
/// and so on; any other tick gets an empty label.</summary>
public sealed class LogTickFormatter : ITickFormatter, ITickSetFormatter, IJsonDescribed<TickFormatterDto>
{
    private static readonly string[] Superscripts = ["\u2070", "\u00B9", "\u00B2", "\u00B3", "\u2074", "\u2075", "\u2076", "\u2077", "\u2078", "\u2079"];

    /// <summary>Which coefficients are labelled (<see cref="Rendering.TickLocators.LogLabelThresholds.NamedOver"/>); 0 for every one, a
    /// formatter told no view.</summary>
    private readonly int _named;

    /// <summary>A formatter that labels every value.</summary>
    public LogTickFormatter() { }

    private LogTickFormatter(int named) => _named = named;

    /// <summary>The formatter told a view, one per level (1, 2, 3); it holds nothing else, so every axis shares them and a draw makes
    /// none.</summary>
    private static readonly LogTickFormatter[] ByLevel = [new(1), new(2), new(3)];

    /// <inheritdoc />
    TickFormatterDto IJsonDescribed<TickFormatterDto>.Describe() => new LogTickFormatterDto();

    /// <inheritdoc />
    /// <remarks>matplotlib 3.11.2's <c>LogFormatter.set_locs</c>: "Use axis view limits to control which ticks are labeled. The *locs*
    /// parameter is ignored", over the log parts alone on a symmetric-log axis, by the theme's thresholds; a formatter told no view labels
    /// every tick.</remarks>
    ITickFormatter ITickSetFormatter.ForTicks(double[] ticks, Range1D? view, double? linThresh, Styling.Theme theme) =>
        view is { } shown
            ? ByLevel[theme.Looks.TickLabels.DrawnLogThresholds.NamedOver(shown.Lo, shown.Hi, linThresh) - 1]
            : this;

    /// <inheritdoc />
    /// <remarks>matplotlib 3.11.2's <c>LogFormatterMathtext.__call__</c>: <c>if x == 0: return '0'</c>, and <c>sign_string = '-' if x &lt; 0
    /// else ''</c> before the label of <c>abs(x)</c>; its mathtext draws that '-' as the true minus sign (U+2212) whatever
    /// <c>axes.unicode_minus</c> says, so the label carries it under every theme. A formatter told the view of its axis returns ''
    /// for a tick whose rounded coefficient the view does not name (<c>if self._sublabels is not None and coeff not in
    /// self._sublabels: return ''</c>).</remarks>
    public string Format(double value)
    {
        if (value == 0) return "0";
        if (value < 0) return Format(-value) is { Length: > 0 } label ? TickLabelLook.TrueMinusSign + label : "";

        double log = Math.Log10(value);
        if (Math.Abs(log - Math.Round(log)) < 1e-10)
        {
            // A power of ten: its coefficient is 1, which every level names.
            int exp = (int)Math.Round(log);
            return $"10{ToSuperscript(exp)}";
        }

        // Level 3 names every coefficient, and a formatter told no view (0) names every tick.
        if (_named is 1 or 2 && !Rendering.TickLocators.LogLabelThresholds.Names(Rendering.TickLocators.LogLabelThresholds.CoefficientOf(value), _named))
            return "";

        return SciNotation(value);
    }

    /// <summary>The value as matplotlib's <c>LogFormatterSciNotation</c> writes it, whether or not it stands on a power of ten: <c>10ⁿ</c> for a
    /// power, <c>c×10ⁿ</c> otherwise, <c>c</c> being the value's coefficient in <c>[1, 10)</c> as <c>%g</c> prints it (<c>2×10²</c>,
    /// <c>6×10⁻¹</c>): what <see cref="Format"/> writes for a value off a power, and every label of a colour bar of matplotlib's log
    /// norm.</summary>
    /// <param name="value">The value, above 0.</param>
    /// <returns>The label.</returns>
    internal string SciNotation(double value)
    {
        double log = Math.Log10(value);
        int exponent = (int)Math.Floor(log + 1e-10);
        double coefficient = Math.Pow(10, log - exponent);
        if (Math.Abs(coefficient - Math.Round(coefficient)) < 1e-9) coefficient = Math.Round(coefficient);
        return coefficient == 1
            ? $"10{ToSuperscript(exponent)}"
            : $"{coefficient.InvariantText("G6")}×10{ToSuperscript(exponent)}";
    }

    /// <summary>The exponent <paramref name="n"/> in superscript digits, with a superscript minus below 0, as a log or logit label writes a power of ten.</summary>
    /// <param name="n">The exponent.</param>
    /// <returns>The exponent in superscript digits.</returns>
    internal static string ToSuperscript(int n)
    {
        if (n >= 0 && n <= 9) return Superscripts[n];

        var chars = n.InvariantText();
        var sb = new System.Text.StringBuilder();
        foreach (var c in chars)
        {
            if (c == '-') sb.Append('\u207B');
            else if (c >= '0' && c <= '9') sb.Append(Superscripts[c - '0']);
        }
        return sb.ToString();
    }
}
