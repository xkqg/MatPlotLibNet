// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Lighting;

/// <summary>Computes light intensity for a surface face given its normal vector.</summary>
public interface ILightSource
{
    /// <summary>Returns a light intensity in [0, 1] for a face with the given outward normal.</summary>
    double ComputeIntensity(double nx, double ny, double nz);
}
