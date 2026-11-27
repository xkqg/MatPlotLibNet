// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>How a Gaussian kernel density is estimated where its author names no bandwidth: the factor of the normal-reference
/// bandwidth (<see cref="GaussianKde.Bandwidth"/>) and the number of points it is evaluated at. A kept estimate is kept for the rule it
/// was made under (<c>KeptEstimate</c>), so a series drawn under another theme is estimated again.</summary>
/// <param name="BandwidthFactor">The factor of the bandwidth rule (<see cref="GaussianKde.SilvermanFactor"/>,
/// <see cref="GaussianKde.ScottFactor"/>).</param>
/// <param name="Points">How many points the density is evaluated at.</param>
internal readonly record struct DensityRule(double BandwidthFactor, int Points);
