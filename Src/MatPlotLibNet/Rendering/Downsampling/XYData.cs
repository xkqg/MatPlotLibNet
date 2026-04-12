// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Downsampling;

/// <summary>A pair of parallel X and Y coordinate arrays representing a dataset.</summary>
/// <param name="X">The X coordinate values.</param>
/// <param name="Y">The Y coordinate values, parallel to <paramref name="X"/>.</param>
public readonly record struct XYData(double[] X, double[] Y);
