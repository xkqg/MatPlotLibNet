// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>Maps a raw data value to a normalized [0, 1] range for colormap lookup.</summary>
public interface INormalizer
{
    /// <summary>Normalizes <paramref name="value"/> within the range [<paramref name="min"/>, <paramref name="max"/>] to [0, 1].</summary>
    double Normalize(double value, double min, double max);
}

/// <summary>Linear normalization: <c>(value - min) / (max - min)</c>, clamped to [0, 1].</summary>
public sealed class LinearNormalizer : INormalizer
{
    /// <summary>Gets the singleton instance.</summary>
    public static LinearNormalizer Instance { get; } = new();

    /// <inheritdoc />
    public double Normalize(double value, double min, double max)
    {
        double range = max - min;
        if (range == 0) return 0.5;
        return Math.Clamp((value - min) / range, 0, 1);
    }
}

/// <summary>Logarithmic normalization that compresses high values: <c>log(1 + value - min) / log(1 + max - min)</c>.</summary>
public sealed class LogNormalizer : INormalizer
{
    /// <inheritdoc />
    public double Normalize(double value, double min, double max)
    {
        double range = max - min;
        if (range == 0) return 0.5;
        double clamped = Math.Clamp(value, min, max);
        return Math.Log(1 + clamped - min) / Math.Log(1 + range);
    }
}

/// <summary>Two-slope normalization with a center point that maps to 0.5. Useful for diverging data
/// where values above and below center should use different halves of a diverging colormap.</summary>
public sealed class TwoSlopeNormalizer : INormalizer
{
    /// <summary>Gets the center data value that maps to 0.5.</summary>
    public double Center { get; }

    /// <summary>Creates a two-slope normalizer with the specified center value.</summary>
    public TwoSlopeNormalizer(double center) => Center = center;

    /// <inheritdoc />
    public double Normalize(double value, double min, double max)
    {
        double clamped = Math.Clamp(value, min, max);
        if (clamped <= Center)
        {
            double lowerRange = Center - min;
            return lowerRange == 0 ? 0.5 : 0.5 * (clamped - min) / lowerRange;
        }
        else
        {
            double upperRange = max - Center;
            return upperRange == 0 ? 0.5 : 0.5 + 0.5 * (clamped - Center) / upperRange;
        }
    }
}

/// <summary>Discrete boundary normalization that maps values to bins defined by boundary values.
/// Values between consecutive boundaries map to the same normalized value.</summary>
public sealed class BoundaryNormalizer : INormalizer
{
    private readonly double[] _boundaries;

    /// <summary>Creates a boundary normalizer with the specified boundary values (must be sorted ascending).</summary>
    public BoundaryNormalizer(double[] boundaries) => _boundaries = boundaries;

    /// <inheritdoc />
    public double Normalize(double value, double min, double max)
    {
        int nBins = _boundaries.Length - 1;
        if (nBins <= 0) return 0.5;

        for (int i = 0; i < nBins; i++)
        {
            if (value < _boundaries[i + 1])
                return (double)i / nBins;
        }

        return (double)(nBins - 1) / nBins;
    }
}
