// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>A point normalized to the [−1, 1]³ cube, returned by <see cref="Projection3D.Normalize"/>.</summary>
public readonly record struct Normalized3DPoint(double Nx, double Ny, double Nz);
