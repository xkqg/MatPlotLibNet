// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.TickFormatters;

/// <summary>Writes the labels of a logit axis as matplotlib's <c>LogitFormatter</c> does: a power of ten below ½ as <c>10⁻²</c>,
/// ½, one less a power of ten above it as <c>1−10⁻²</c>, any other value below 0.1 or above 0.9 as <c>2⋅10⁻²</c> or
/// <c>1−2⋅10⁻²</c>, and a value between them as a plain number, each with the digits that tell it from its nearest neighbour among
/// the axis' ticks. A value at or past 0 or 1 has no label. The formatter a logit axis is drawn with when it names none.</summary>
internal sealed class LogitFormatter : ITickSetFormatter
{

    /// <summary>The dot of <c>\cdot</c>.</summary>
    private const char Dot = '⋅';

    private readonly double[] _ticks;

    /// <summary>A formatter that knows no other tick: each value is written with the fewest digits.</summary>
    public LogitFormatter() : this([]) { }

    private LogitFormatter(double[] ticks) => _ticks = ticks;

    /// <inheritdoc />
    /// <remarks>Only the ticks count: each label takes the digits that tell it from its nearest neighbour among them.</remarks>
    public ITickFormatter ForTicks(double[] ticks, Range1D? view, double? linThresh, Styling.Theme theme) => new LogitFormatter(ticks);

    /// <inheritdoc />
    public string Format(double value)
    {
        if (!(value > 0 && value < 1)) return "";
        if (IsHalf(value)) return "½";
        if (value < 0.5 && IsDecade(value)) return "10" + LogTickFormatter.ToSuperscript((int)value.NearestDecadeExponent());
        if (value > 0.5 && IsDecade(1 - value)) return OneMinus("10" + LogTickFormatter.ToSuperscript((int)(1 - value).NearestDecadeExponent()));
        if (value < 0.1) return FormatValue(value, _ticks, sciNotation: true);
        if (value > 0.9) return OneMinus(FormatValue(1 - value, [.. _ticks.Select(t => 1 - t)], sciNotation: true));
        return FormatValue(value, _ticks, sciNotation: false);
    }

    /// <summary>Whether <paramref name="value"/> is one of the ticks a logit scale labels as a power: a power of ten, one less a
    /// power of ten, or ½ (<c>LogitFormatter.set_locs</c>' test before it names a minor tick).</summary>
    /// <param name="value">The tick value, between 0 and 1.</param>
    /// <returns><see langword="true"/> when the scale labels the value as a power.</returns>
    internal static bool IsIdeal(double value) => IsDecade(value) || IsDecade(1 - value) || IsHalf(value);

    private static string OneMinus(string text) => "1" + TickLabelLook.TrueMinusSign + text;

    /// <summary><c>_is_close_to_int(2 * x) and round(2 * x) == 1</c>.</summary>
    private static bool IsHalf(double value) => IsCloseToInt(2 * value) && Math.Round(2 * value, MidpointRounding.ToEven) == 1;

    /// <summary><c>math.isclose(x, round(x))</c>.</summary>
    private static bool IsCloseToInt(double x)
    {
        double r = Math.Round(x, MidpointRounding.ToEven);
        return Math.Abs(x - r) <= 1e-9 * Math.Max(Math.Abs(x), Math.Abs(r));
    }

    /// <summary><c>_is_decade(x, rtol=1e-7)</c>: <c>np.isclose(lx, round(lx), rtol=1e-7)</c> with <c>lx = log(|x|) / log(10)</c>, for a
    /// value strictly between 0 and 1, the only kind a logit axis asks about (its arms for 0 and for a value that is not finite have
    /// no case here).</summary>
    private static bool IsDecade(double x)
    {
        double lx = Math.Log(Math.Abs(x)) / Math.Log(10);
        double r = Math.Round(lx, MidpointRounding.ToEven);
        return Math.Abs(lx - r) <= 1e-8 + 1e-7 * Math.Abs(r);
    }

    /// <summary><c>LogitFormatter._format_value</c>: the value, in scientific notation or not, with as many digits as the distance to
    /// its nearest neighbour among <paramref name="ticks"/> asks for (at least none in scientific notation, one otherwise).</summary>
    private static string FormatValue(double x, double[] ticks, bool sciNotation)
    {
        int exponent = sciNotation ? (int)x.DecadeExponentAtOrBelow() : 0;
        int minPrecision = sciNotation ? 0 : 1;
        double value = x * Math.Pow(10, -exponent);
        int precision = minPrecision;
        if (ticks.Length >= 2)
        {
            var distances = new double[ticks.Length];
            for (int i = 0; i < ticks.Length; i++) distances[i] = Math.Abs(ticks[i] - x);
            Array.Sort(distances);
            double raw = -Math.Log10(distances[1]) + exponent;
            precision = IsCloseToInt(raw) ? (int)Math.Round(raw, MidpointRounding.ToEven) : (int)Math.Ceiling(raw);
            if (precision < minPrecision) precision = minPrecision;
        }

        string mantissa = value.InvariantText("F" + precision.InvariantText());
        return sciNotation ? mantissa + Dot + "10" + LogTickFormatter.ToSuperscript(exponent) : mantissa;
    }
}
