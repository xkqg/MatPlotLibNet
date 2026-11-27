// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Lighting;

namespace MatPlotLibNet.Tests;

/// <summary>A light an application wrote, as a test hands one to a 3-D series: it has no JSON form and lights every face with
/// <paramref name="intensity"/>.</summary>
/// <param name="intensity">The intensity of every face.</param>
internal sealed class TestLightSource(double intensity) : ILightSource
{
    public double ComputeIntensity(double nx, double ny, double nz) => intensity;
}
