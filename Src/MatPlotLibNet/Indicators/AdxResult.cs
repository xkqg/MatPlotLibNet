// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators;

/// <summary>Full result of <see cref="Adx.ComputeFull"/>: the ADX trend-strength line plus
/// the directional indicators +DI and −DI.</summary>
/// <param name="Adx">ADX trend-strength values (0–100 scale).</param>
/// <param name="PlusDi">Positive directional indicator (+DI) values.</param>
/// <param name="MinusDi">Negative directional indicator (−DI) values.</param>
public sealed record AdxResult(double[] Adx, double[] PlusDi, double[] MinusDi);
