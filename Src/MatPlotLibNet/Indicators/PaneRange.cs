// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>The range of values an indicator's output lives in, which the indicator pins the Y axis of its pane to: 0 to 100 for an
/// RSI, 0 to 1 for an efficiency ratio, -1 to 1 for an inverse Fisher transform. A side left <see langword="null"/> is left to the
/// data: a drawdown, drawn below zero, pins only its top; a volume pins only its bottom. A pinned side is a limit of the axis, so the
/// pane no longer scales to the data on that side.</summary>
/// <param name="Min">The bottom of the range, or <see langword="null"/> to leave the bottom to the data.</param>
/// <param name="Max">The top of the range, or <see langword="null"/> to leave the top to the data.</param>
internal readonly record struct PaneRange(double? Min, double? Max)
{
    /// <summary>0 to 100: a percentage, such as an RSI or a stochastic.</summary>
    internal static PaneRange Percent { get; } = new(0, 100);

    /// <summary>0 to 1: a share or a probability, such as an efficiency ratio or an entropy.</summary>
    internal static PaneRange Unit { get; } = new(0, 1);

    /// <summary>-1 to 1: a signed share, such as an inverse Fisher transform or a money flow.</summary>
    internal static PaneRange Signed { get; } = new(-1, 1);

    /// <summary>A range from <paramref name="min"/> up, its top left to the data.</summary>
    /// <param name="min">The bottom of the range.</param>
    /// <returns>The range.</returns>
    internal static PaneRange AtLeast(double min) => new(min, null);

    /// <summary>A range up to <paramref name="max"/>, its bottom left to the data.</summary>
    /// <param name="max">The top of the range.</param>
    /// <returns>The range.</returns>
    internal static PaneRange AtMost(double max) => new(null, max);

    /// <summary>Pins the Y axis of <paramref name="axes"/> to the range: sets the limit of each side the range has, and leaves the
    /// other side as it was.</summary>
    /// <param name="axes">The pane of the indicator.</param>
    internal void Pin(Axes axes)
    {
        if (Min is { } min) axes.YAxis.Min = min;
        if (Max is { } max) axes.YAxis.Max = max;
    }
}
