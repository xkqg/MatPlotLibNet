// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>Confidence band returned by <see cref="LeastSquares.ConfidenceBand"/>.</summary>
/// <param name="Upper">Upper confidence bound evaluated at each requested X point.</param>
/// <param name="Lower">Lower confidence bound evaluated at each requested X point.</param>
public sealed record ConfidenceBand(double[] Upper, double[] Lower);
