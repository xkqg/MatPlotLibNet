// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>One input of a <see cref="StreamingVwap"/>: a price and the volume traded at it. A bar reaches the form as its typical
/// price, (high + low + close) / 3, with its volume.</summary>
/// <param name="Price">The price.</param>
/// <param name="Volume">The volume traded at it; NaN when it is not known.</param>
public readonly record struct TradedPrice(double Price, double Volume);
