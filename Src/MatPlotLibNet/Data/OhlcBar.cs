// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Data;

/// <summary>A single Open-High-Low-Close price bar.</summary>
/// <param name="Open">Opening price.</param>
/// <param name="High">Highest price in the period.</param>
/// <param name="Low">Lowest price in the period.</param>
/// <param name="Close">Closing price.</param>
public readonly record struct OhlcBar(double Open, double High, double Low, double Close);
