// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators;

/// <summary>Result for band-based indicators (Bollinger Bands, Keltner Channels). Contains middle line with upper and lower bands.</summary>
public sealed record BandsResult(double[] Middle, double[] Upper, double[] Lower) : IIndicatorResult;
