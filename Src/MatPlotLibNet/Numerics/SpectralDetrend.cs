// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace MatPlotLibNet.Numerics;

/// <summary>What is taken out of each segment of a signal before its spectrum is computed, so a slow drift does not fill the
/// lowest bins. The value you get from <c>default</c> is <see cref="None"/>, matplotlib's <c>detrend_none</c>, which its
/// spectral functions use when none is given.
/// <para>Make one with a factory: <see cref="None"/>, <see cref="Mean"/> (matplotlib's <c>detrend_mean</c>),
/// <see cref="Linear"/> (<c>detrend_linear</c>), or <see cref="Of"/> for a detrend of your own.</para></summary>
/// <example>
/// <code>
/// SpectralDetrend detrend = SpectralDetrend.Mean;
/// SpectralDetrend firstSample = SpectralDetrend.Of(segment => [.. segment.Select(v => v - segment[0])]);
/// </code>
/// </example>
public readonly record struct SpectralDetrend
{
    private readonly Func<double[], double[]>? _removed;

    private SpectralDetrend(Func<double[], double[]> removed) => _removed = removed;

    /// <summary>Nothing is taken out: the segment is used as it is, its mean included. It is what <c>default</c> means.</summary>
    public static SpectralDetrend None => default;

    /// <summary>The mean of the segment is subtracted from each of its samples, so the DC bin holds no offset.</summary>
    public static SpectralDetrend Mean => new(static segment => SubtractLine(segment, 0));

    /// <summary>The least-squares straight line through the segment (its samples against their index) is subtracted, so
    /// neither an offset nor a steady drift reaches the spectrum. A segment of one sample becomes 0.</summary>
    public static SpectralDetrend Linear => new(static segment => SubtractLine(segment, Slope(segment)));

    /// <summary>A detrend of your own, given by the function that turns a segment into what its spectrum is taken of.</summary>
    /// <param name="removed">The segment with its trend removed: as many finite numbers as the segment it is given holds. It
    /// is checked each time it is applied.</param>
    /// <returns>The detrend.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="removed"/> is <see langword="null"/>.</exception>
    public static SpectralDetrend Of(Func<double[], double[]> removed)
    {
        ArgumentNullException.ThrowIfNull(removed);
        return new(segment => Checked(removed(segment), segment.Length));
    }

    /// <summary>The segment with its trend taken out. <see cref="None"/> gives the segment itself.</summary>
    /// <exception cref="InvalidOperationException">A detrend from <see cref="Of"/> gave nothing, a different number of values
    /// than the segment holds, or a value that is not a finite number.</exception>
    internal double[] Removed(double[] segment) => _removed is null ? segment : _removed(segment);

    /// <summary>The slope of the least-squares line through (i, segment[i]); 0 for one sample.</summary>
    private static double Slope(double[] segment)
    {
        int n = segment.Length;
        double meanIndex = (n - 1) / 2.0, mean = segment.Average();
        double sxx = 0, sxy = 0;
        for (int i = 0; i < n; i++)
        {
            double di = i - meanIndex;
            sxx += di * di;
            sxy += di * (segment[i] - mean);
        }

        return n > 1 ? sxy / sxx : 0;
    }

    /// <summary>The segment less the line through its mean at the middle index with the given slope.</summary>
    private static double[] SubtractLine(double[] segment, double slope)
    {
        double meanIndex = (segment.Length - 1) / 2.0, mean = segment.Average();
        var result = new double[segment.Length];
        for (int i = 0; i < segment.Length; i++) result[i] = segment[i] - mean - slope * (i - meanIndex);
        return result;
    }

    private static double[] Checked(double[]? values, int n)
    {
        if (values is null || values.Length != n)
        {
            throw new InvalidOperationException(
                $"The detrend function gave {(values is null ? "no" : values.Length.ToString(CultureInfo.InvariantCulture))} values for a segment of {n.ToString(CultureInfo.InvariantCulture)} samples.");
        }

        foreach (double value in values)
        {
            if (!double.IsFinite(value))
            {
                throw new InvalidOperationException(
                    $"The detrend function gave a value of {value.ToString(CultureInfo.InvariantCulture)}; every value is a finite number.");
            }
        }

        return values;
    }
}
