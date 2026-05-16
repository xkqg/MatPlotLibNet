// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>RS-Ratio and RS-Momentum arrays for a single asset, as returned by
/// <see cref="RelativeRotationSeries.ComputeRsData"/>.</summary>
/// <param name="RsRatio">RS-Ratio time series. Same length as the input close series;
/// leading values are <see cref="double.NaN"/> where lookback windows are not yet full.</param>
/// <param name="RsMomentum">RS-Momentum time series. Same length as the input close series;
/// leading values are <see cref="double.NaN"/> where lookback windows are not yet full.</param>
public readonly record struct RrsPoint(double[] RsRatio, double[] RsMomentum);
