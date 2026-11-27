// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>Symmetric logarithmic coordinate transform, matplotlib's <c>SymmetricalLogTransform</c>: within [-linthresh, linthresh] a
/// value is stretched by <c>linscale / (1 - 1/base)</c>, beyond it it is mapped to
/// <c>sign * linthresh * (linscale / (1 - 1/base) + log_base(|value| / linthresh))</c>, so the two parts meet at the thresholds. A
/// symmetric-log axis takes matplotlib's defaults (base <see cref="DefaultBase"/>, linscale <see cref="DefaultLinScale"/>); the symmetric-log normaliser
/// (<see cref="Styling.ColorMaps.SymLogNormalizer"/>) maps by the same rule with its own base and linscale. The browser's view
/// inverts it by the same formula.</summary>
public static class SymlogTransform
{
    /// <summary>Default linear threshold: values within this distance of 0 are mapped linearly.</summary>
    public const double DefaultLinThresh = 1.0;

    /// <summary>The logarithm base of a symmetric-log axis, and of a symmetric-log normaliser made with none (matplotlib's default).</summary>
    internal const double DefaultBase = 10.0;

    /// <summary>How many decades the linear part of a symmetric-log axis takes on either side of zero, and of a symmetric-log
    /// normaliser made with none (matplotlib's default).</summary>
    internal const double DefaultLinScale = 1.0;

    /// <summary>Transforms a value from data space to symlog space, as a symmetric-log axis maps it (base 10, linscale 1).</summary>
    /// <param name="x">Data value.</param>
    /// <param name="linthresh">Linear threshold. Values within [-linthresh, linthresh] map linearly.</param>
    /// <returns>Symlog-transformed value.</returns>
    public static double Forward(double x, double linthresh = DefaultLinThresh) => Forward(x, linthresh, DefaultBase, DefaultLinScale);

    /// <summary>Transforms a value from symlog space back to data space: the inverse of <see cref="Forward(double, double)"/>.</summary>
    /// <param name="y">Symlog-space value.</param>
    /// <param name="linthresh">Linear threshold used in the forward transform.</param>
    /// <returns>Data-space value.</returns>
    public static double Inverse(double y, double linthresh = DefaultLinThresh) => Inverse(y, linthresh, DefaultBase, DefaultLinScale);

    /// <summary>Transforms an array of data values to symlog space.</summary>
    public static double[] ForwardArray(double[] data, double linthresh = DefaultLinThresh)
    {
        var result = new double[data.Length];
        for (int i = 0; i < data.Length; i++)
            result[i] = Forward(data[i], linthresh);
        return result;
    }

    /// <summary>matplotlib's <c>SymmetricalLogTransform(base, linthresh, linscale)</c>: the one rule every symmetric-log mapping of the
    /// library follows. A missing value stays missing; a threshold that is not above 0 is taken as 1.</summary>
    /// <param name="x">Data value.</param>
    /// <param name="linthresh">Linear threshold.</param>
    /// <param name="base">The logarithm base beyond the threshold.</param>
    /// <param name="linscale">How many decades the linear part takes on either side of zero.</param>
    /// <returns>The transformed value.</returns>
    internal static double Forward(double x, double linthresh, double @base, double linscale)
    {
        if (linthresh <= 0) linthresh = DefaultLinThresh;
        // NaN must propagate; Math.Sign(NaN) throws, hence the explicit guard. An infinity goes through the log branch.
        if (double.IsNaN(x)) return double.NaN;
        double stretch = LinearStretch(@base, linscale);
        double magnitude = Math.Abs(x);
        if (magnitude <= linthresh) return x * stretch;
        return Math.Sign(x) * linthresh * (stretch + Math.Log(magnitude / linthresh) / Math.Log(@base));
    }

    /// <summary>matplotlib's <c>InvertedSymmetricalLogTransform(base, linthresh, linscale)</c>: the inverse of
    /// <see cref="Forward(double, double, double, double)"/>.</summary>
    /// <param name="y">Symlog-space value.</param>
    /// <param name="linthresh">Linear threshold.</param>
    /// <param name="base">The logarithm base beyond the threshold.</param>
    /// <param name="linscale">How many decades the linear part takes on either side of zero.</param>
    /// <returns>The data value.</returns>
    internal static double Inverse(double y, double linthresh, double @base, double linscale)
    {
        if (linthresh <= 0) linthresh = DefaultLinThresh;
        if (double.IsNaN(y)) return double.NaN;
        double stretch = LinearStretch(@base, linscale);
        double magnitude = Math.Abs(y);
        if (magnitude <= linthresh * stretch) return y / stretch;
        return Math.Sign(y) * linthresh * Math.Pow(@base, magnitude / linthresh - stretch);
    }

    /// <summary>The factor the linear part is stretched by, matplotlib's <c>linscale_adj = linscale / (1 - 1/base)</c>.</summary>
    private static double LinearStretch(double @base, double linscale) => linscale / (1.0 - 1.0 / @base);
}
