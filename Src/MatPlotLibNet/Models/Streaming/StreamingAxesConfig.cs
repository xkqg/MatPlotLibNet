// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Streaming;

/// <summary>Per-axes streaming configuration controlling how X and Y axes scale with incoming data.</summary>
/// <param name="XMode">X-axis scaling mode. Default <see cref="AxisScaleMode.SlidingWindow"/>.</param>
/// <param name="YMode">Y-axis scaling mode. Default <see cref="AxisScaleMode.AutoScale"/>.</param>
public sealed record StreamingAxesConfig(
    AxisScaleMode XMode,
    AxisScaleMode YMode)
{
    /// <summary>Default streaming config: sliding window on X, auto-scale on Y.</summary>
    public static StreamingAxesConfig Default(double windowSize = 100.0) =>
        new(new AxisScaleMode.SlidingWindow(windowSize), new AxisScaleMode.AutoScale());
}
