// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Downsampling;

/// <summary>Reduces a large dataset to a manageable number of display points while preserving visual shape.</summary>
public interface IDownsampler
{
    /// <summary>
    /// Returns a downsampled version of the input arrays with at most <paramref name="targetPoints"/> entries.
    /// If the input already has fewer points than the target, all points are returned unchanged.
    /// </summary>
    (double[] X, double[] Y) Downsample(double[] x, double[] y, int targetPoints);
}
