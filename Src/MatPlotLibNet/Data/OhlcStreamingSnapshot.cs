// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Data;

/// <summary>Immutable point-in-time copy of streaming OHLC data, safe for render-thread consumption.</summary>
/// <param name="Open">Open price values in chronological order.</param>
/// <param name="High">High price values in chronological order.</param>
/// <param name="Low">Low price values in chronological order.</param>
/// <param name="Close">Close price values in chronological order.</param>
/// <param name="Version">Monotonic version counter at the time of snapshot creation.</param>
public readonly record struct OhlcStreamingSnapshot(double[] Open, double[] High, double[] Low, double[] Close, long Version);
