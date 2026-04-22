// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>Paired X/Y sample arrays of equal length describing a sampled curve. Returned by
/// numerical routines that produce a curve, including
/// <see cref="MonotoneCubicSpline.Interpolate"/> and <see cref="Rendering.SeriesRenderers.GaussianKde.Evaluate"/>.
/// Deconstructs as <c>var (x, y) = curve;</c>.</summary>
/// <param name="X">Sample X coordinates.</param>
/// <param name="Y">Sample Y coordinates. Same length as <paramref name="X"/>.</param>
public readonly record struct XYCurve(double[] X, double[] Y);
