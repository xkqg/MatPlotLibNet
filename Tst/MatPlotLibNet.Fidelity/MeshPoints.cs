// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Fidelity;

/// <summary>The points of a grid, each as its x and its y, in the order numpy's <c>meshgrid</c> then <c>ravel</c> list them.</summary>
/// <param name="X">The x of each point.</param>
/// <param name="Y">The y of each point.</param>
public readonly record struct MeshPoints(double[] X, double[] Y);
