// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Streaming;

/// <summary>Controls how axes auto-scale when streaming data arrives.</summary>
public abstract record AxisScaleMode
{
    private AxisScaleMode() { }

    /// <summary>User-set limits, never auto-adjusted.</summary>
    public sealed record Fixed : AxisScaleMode;

    /// <summary>Fit all data every render — axes expand/contract to show everything.</summary>
    public sealed record AutoScale : AxisScaleMode;

    /// <summary>Show the last <see cref="WindowSize"/> X-units, scrolling as new data arrives.
    /// Y auto-scales within the visible window.</summary>
    /// <param name="WindowSize">Width of the visible X window in data units.</param>
    public sealed record SlidingWindow(double WindowSize) : AxisScaleMode;

    /// <summary>Like <see cref="SlidingWindow"/> but only scrolls when the rightmost data point
    /// is at or past the window edge. Stays still when the user has panned away.</summary>
    /// <param name="WindowSize">Width of the visible X window in data units.</param>
    public sealed record StickyRight(double WindowSize) : AxisScaleMode;
}
