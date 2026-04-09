// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators;

/// <summary>Result for the Stochastic oscillator containing the %K and %D lines.</summary>
public sealed record StochasticResult(double[] K, double[] D) : IIndicatorResult;
