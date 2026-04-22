// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators;

/// <summary>Result of <see cref="SqueezeMomentum"/>: per-bar squeeze state +
/// linear-regression-based momentum.</summary>
public sealed record SqueezeResult(bool[] SqueezeOn, double[] Momentum) : IIndicatorResult;
