// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>Symmetric logarithmic coordinate transform. Linear within [-linthresh, linthresh],
/// logarithmic outside. Continuous and differentiable at the thresholds.
/// Equivalent to matplotlib's <c>symlog</c> scale.</summary>
public static class SymlogTransform
{
    /// <summary>Default linear threshold — values within [-1, 1] are mapped linearly.</summary>
    public const double DefaultLinThresh = 1.0;

    /// <summary>Transforms a value from data space to symlog space.</summary>
    /// <param name="x">Data value.</param>
    /// <param name="linthresh">Linear threshold. Values within [-linthresh, linthresh] map linearly.</param>
    /// <returns>Symlog-transformed value.</returns>
    public static double Forward(double x, double linthresh = DefaultLinThresh)
    {
        if (linthresh <= 0) linthresh = DefaultLinThresh;
        if (Math.Abs(x) <= linthresh) return x;
        return Math.Sign(x) * (linthresh * (1 + Math.Log10(Math.Abs(x) / linthresh)));
    }

    /// <summary>Transforms a value from symlog space back to data space.</summary>
    /// <param name="y">Symlog-space value.</param>
    /// <param name="linthresh">Linear threshold used in the forward transform.</param>
    /// <returns>Data-space value.</returns>
    public static double Inverse(double y, double linthresh = DefaultLinThresh)
    {
        if (linthresh <= 0) linthresh = DefaultLinThresh;
        if (Math.Abs(y) <= linthresh) return y;
        return Math.Sign(y) * linthresh * Math.Pow(10, Math.Abs(y) / linthresh - 1);
    }

    /// <summary>Transforms an array of data values to symlog space.</summary>
    public static double[] ForwardArray(double[] data, double linthresh = DefaultLinThresh)
    {
        var result = new double[data.Length];
        for (int i = 0; i < data.Length; i++)
            result[i] = Forward(data[i], linthresh);
        return result;
    }
}
