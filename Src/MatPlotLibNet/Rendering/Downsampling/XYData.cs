// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Downsampling;

/// <summary>A pair of parallel X and Y coordinate arrays representing a dataset.</summary>
/// <param name="X">The X coordinate values.</param>
/// <param name="Y">The Y coordinate values, parallel to <paramref name="X"/>.</param>
public readonly record struct XYData(double[] X, double[] Y)
{
    /// <summary>The point budget of a line (<c>MaxDisplayPoints</c>) applied to its samples, the one place a line that is over its
    /// budget is reduced, whether its view was cut by a linear scan (<see cref="ViewportCuller"/>) or by index arithmetic
    /// (<see cref="MonotonicViewportSlicer"/>): the samples as they are while there is no budget or they are within it; otherwise
    /// reduced to the budget by largest-triangle-three-buckets (<see cref="LttbDownsampler"/>), which keeps the first and last samples
    /// and the shape between.</summary>
    /// <param name="maxPoints">The most samples to keep, or <see langword="null"/> for no limit.</param>
    /// <returns>The samples to draw.</returns>
    internal XYData AtMost(int? maxPoints) =>
        maxPoints is null || X.Length <= maxPoints.Value
            ? this
            : new LttbDownsampler().Downsample(X, Y, maxPoints.Value);
}
