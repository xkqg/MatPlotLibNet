// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Interpolation;

namespace MatPlotLibNet.Tests;

/// <summary>An interpolation engine an application wrote, as a test hands one to an image: no registry knows it; it resamples as
/// <paramref name="resample"/> says, or hands the data back.</summary>
/// <param name="name">The engine's name.</param>
/// <param name="resample">The resampling of the data to a number of rows and columns; left out, the data as it is.</param>
internal sealed class TestInterpolationEngine(string name, Func<double[,], int, int, double[,]>? resample = null) : IInterpolationEngine
{
    public string Name => name;
    public double[,] Resample(double[,] data, int targetRows, int targetCols) => resample?.Invoke(data, targetRows, targetCols) ?? data;
}
