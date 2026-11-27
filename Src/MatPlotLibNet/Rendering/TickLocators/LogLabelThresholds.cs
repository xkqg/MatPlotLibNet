// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.TickLocators;

/// <summary>Which ticks of a log scale are named, as matplotlib's <c>LogFormatter.minor_thresholds</c> decides in <c>set_locs</c>:
/// with more powers of ten in view than <paramref name="Powers"/>, only the powers; with a view wider than <paramref name="Decades"/>
/// decades, the multiples whose coefficient rounds to 1, 2, 3, 4 or 6; otherwise every one. The one rule the ticks a log axis names
/// (<see cref="LogLocator"/>) and the labels its formatter writes for ticks of any locator (<see cref="TickFormatters.LogTickFormatter"/>)
/// follow.</summary>
/// <param name="Powers">The most powers of ten in view for which multiples are named.</param>
/// <param name="Decades">The width of the view, in decades, above which only some multiples are named.</param>
internal readonly record struct LogLabelThresholds(double Powers, double Decades)
{
    /// <summary>The default style's thresholds: <c>(1, 0.4)</c>.</summary>
    internal static readonly LogLabelThresholds Default = new(1, 0.4);

    /// <summary>The thresholds of matplotlib's classic mode: <c>(0, 0)</c>.</summary>
    internal static readonly LogLabelThresholds Classic = new(0, 0);

    /// <summary>How many powers of ten stand in a view from <paramref name="lmin"/> to <paramref name="lmax"/> decades: <c>set_locs</c>'
    /// <c>math.floor(lmax) - math.floor(math.nextafter(lmin, -math.inf))</c>, a power at the low end counted.</summary>
    /// <param name="lmin">The low end of the view, in decades.</param>
    /// <param name="lmax">The high end of the view, in decades.</param>
    /// <returns>The number of powers.</returns>
    internal static int PowersIn(double lmin, double lmax) => (int)Math.Floor(lmax) - (int)Math.Floor(Math.BitDecrement(lmin));

    /// <summary>Which multiples are named over <paramref name="lo"/> .. <paramref name="hi"/>, matplotlib 3.11.2's <c>set_locs</c>:
    /// "if numticks &gt; self.minor_thresholds[0]: # Label only bases" (1), "elif numdec &gt; self.minor_thresholds[1]" the
    /// coefficients 1, 2, 3, 4, 6, 10 (2), else every one (3). <c>numticks</c> counts the powers in view and <c>numdec</c> the decades
    /// across it; on a symmetric-log axis (<paramref name="linThresh"/>) both are counted over its log parts alone, past
    /// <c>-linthresh</c> and <c>linthresh</c>.</summary>
    /// <param name="lo">The low end of the view.</param>
    /// <param name="hi">The high end of the view.</param>
    /// <param name="linThresh">The linear threshold of a symmetric-log axis, <see langword="null"/> on a log axis.</param>
    /// <returns>1, 2 or 3, for <see cref="Names"/>.</returns>
    internal int NamedOver(double lo, double hi, double? linThresh = null)
    {
        if (lo > hi) (lo, hi) = (hi, lo);
        // "if linthresh is None and vmin <= 0: self._sublabels = {1}  # label powers of base"
        if (linThresh is not { } t)
            return lo > 0 ? LevelOf(PowersIn(Math.Log10(lo), Math.Log10(hi)), Math.Abs(Math.Log10(hi) - Math.Log10(lo))) : 1;

        int powers = 0;
        double decades = 0;
        if (lo < -t)
        {
            double rhs = Math.Min(hi, -t);
            powers += PowersIn(Math.Log10(Math.Abs(lo)), Math.Log10(Math.Abs(rhs)));
            decades += Math.Log10(lo / rhs);
        }

        if (hi > t)
        {
            double lhs = Math.Max(lo, t);
            powers += PowersIn(Math.Log10(lhs), Math.Log10(hi));
            decades += Math.Log10(hi / lhs);
        }

        return LevelOf(powers, decades);
    }

    /// <summary>The level <see cref="NamedOver"/> names at for <paramref name="numticks"/> powers and <paramref name="numdec"/> decades in
    /// view.</summary>
    private int LevelOf(int numticks, double numdec) => numticks > Powers ? 1 : numdec > Decades ? 2 : 3;

    /// <summary>Whether a tick whose coefficient rounds to <paramref name="coefficient"/> (always 1 to 10) is named at
    /// <paramref name="named"/> (<see cref="NamedOver"/>): the powers alone (1), the coefficients <c>np.round(np.geomspace(1, 10, 6))</c>
    /// = 1, 2, 3, 4, 6, 10 (2), or every one (3).</summary>
    /// <param name="coefficient">The coefficient of the tick, rounded.</param>
    /// <param name="named">How many of the coefficients a tick is named at: 1, 2 or 3.</param>
    /// <returns><see langword="true"/> when the tick is named.</returns>
    internal static bool Names(int coefficient, int named) => named switch
    {
        1 => coefficient == 1,
        2 => coefficient is 1 or 2 or 3 or 4 or 6 or 10,
        _ => true,
    };

    /// <summary>The coefficient matplotlib's <c>LogFormatter.__call__</c> names a value by: <c>fx = math.log(x) / math.log(b)</c>, the
    /// exponent the power at or below it, <c>round(b ** (fx - exponent))</c> half to even. A tick of 4.500000000000001 has the
    /// coefficient 4.499999999999999 that way, which rounds to 4, where log10 gives 4.500000000000001, which rounds to 5.</summary>
    /// <param name="value">The value, above 0.</param>
    /// <returns>The coefficient, rounded, from 1 to 10.</returns>
    internal static int CoefficientOf(double value)
    {
        double fx = Math.Log(value) / Math.Log(10);
        double exponent = Math.Abs(fx - Math.Round(fx)) < 1e-10 ? Math.Round(fx) : Math.Floor(fx);
        return (int)Math.Round(Math.Pow(10, fx - exponent), MidpointRounding.ToEven);
    }
}
