// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Interpolation;

/// <summary>Strategy for resampling a 2D scalar grid to a different resolution.</summary>
public interface IInterpolationEngine
{
    /// <summary>The name the engine is registered and looked up under in <see cref="InterpolationRegistry"/>, such as <c>"nearest"</c> or <c>"bilinear"</c>.</summary>
    string Name { get; }

    /// <summary>Resamples <paramref name="data"/> to a grid of <paramref name="targetRows"/> × <paramref name="targetCols"/> cells.</summary>
    double[,] Resample(double[,] data, int targetRows, int targetCols);
}
