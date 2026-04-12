// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>Normalized screen coordinate returned by <see cref="IMapProjection.Project"/>.
/// <c>Nx</c> = 0 is left, <c>Nx</c> = 1 is right; <c>Ny</c> = 0 is top, <c>Ny</c> = 1 is bottom.</summary>
public readonly record struct NormalizedPoint(double Nx, double Ny);
