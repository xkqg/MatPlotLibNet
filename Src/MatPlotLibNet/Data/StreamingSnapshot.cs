// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Data;

/// <summary>Immutable point-in-time copy of streaming XY data, safe for render-thread consumption.</summary>
/// <param name="XData">X-coordinate values in chronological order.</param>
/// <param name="YData">Y-coordinate values in chronological order.</param>
/// <param name="Version">Monotonic version counter at the time of snapshot creation.</param>
public readonly record struct StreamingSnapshot(double[] XData, double[] YData, long Version);
