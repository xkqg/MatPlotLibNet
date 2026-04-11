// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

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

/// <summary>Symmetric logarithmic normalization. Linear within ±<see cref="Linthresh"/>,
/// log-compressed beyond. Useful for data that spans positive and negative values including zero.</summary>
public sealed class SymLogNormalizer : INormalizer
{
    public double Linthresh { get; }

    public double Base { get; }

    public double LinScale { get; }

    /// <summary>Creates a symmetric-log normalizer.</summary>
    public SymLogNormalizer(double linthresh = 1.0, double @base = 10.0, double linScale = 1.0)
    {
        Linthresh = linthresh;
        Base = @base;
        LinScale = linScale;
    }

    /// <inheritdoc />
    public double Normalize(double value, double min, double max)
    {
        double Transform(double v) =>
            Math.Abs(v) <= Linthresh
                ? v * LinScale / Linthresh
                : Math.Sign(v) * (LinScale + Math.Log(Math.Abs(v) / Linthresh) / Math.Log(Base));

        double t = Transform(Math.Clamp(value, min, max));
        double tMin = Transform(min);
        double tMax = Transform(max);
        double range = tMax - tMin;
        return range == 0 ? 0.5 : Math.Clamp((t - tMin) / range, 0, 1);
    }
}

/// <summary>Power-law normalization: <c>((value − min) / (max − min))^gamma</c>.
/// gamma &lt; 1 expands low values; gamma &gt; 1 compresses them.</summary>
public sealed class PowerNormNormalizer : INormalizer
{
    public double Gamma { get; }

    /// <summary>Creates a power-norm normalizer with the specified gamma.</summary>
    public PowerNormNormalizer(double gamma = 1.0) => Gamma = gamma;

    /// <inheritdoc />
    public double Normalize(double value, double min, double max)
    {
        double range = max - min;
        if (range == 0) return 0.5;
        return Math.Pow(Math.Clamp((value - min) / range, 0, 1), Gamma);
    }
}

/// <summary>Centered normalization that maps a chosen <see cref="Vcenter"/> to 0.5.
/// Independently scales the lower and upper halves. Optionally constrains a symmetric
/// half-range around the center.</summary>
public sealed class CenteredNormNormalizer : INormalizer
{
    public double Vcenter { get; }

    public double? Halfrange { get; }

    /// <summary>Creates a centered normalizer.</summary>
    public CenteredNormNormalizer(double vcenter = 0.0, double? halfrange = null)
    {
        Vcenter = vcenter;
        Halfrange = halfrange;
    }

    /// <inheritdoc />
    public double Normalize(double value, double min, double max)
    {
        double lo = Halfrange.HasValue ? Vcenter - Halfrange.Value : min;
        double hi = Halfrange.HasValue ? Vcenter + Halfrange.Value : max;
        double clamped = Math.Clamp(value, lo, hi);
        if (clamped <= Vcenter)
        {
            double lower = Vcenter - lo;
            return lower == 0 ? 0.5 : 0.5 * (clamped - lo) / lower;
        }
        double upper = hi - Vcenter;
        return upper == 0 ? 0.5 : 0.5 + 0.5 * (clamped - Vcenter) / upper;
    }
}

/// <summary>No-op normalization: the value is passed through as-is, clamped to [0, 1].
/// Use when the data is already in the normalized range.</summary>
public sealed class NoNormNormalizer : INormalizer
{
    public static NoNormNormalizer Instance { get; } = new();

    /// <inheritdoc />
    public double Normalize(double value, double min, double max) => Math.Clamp(value, 0, 1);
}
