// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Streaming;

/// <summary>Controls how axes auto-scale when streaming data arrives.</summary>
/// <remarks>Each mode owns what it does to an axis, along X and along Y, so the streaming figure hands the data's range to the
/// mode instead of choosing the rule by the mode's type. A mode leaves an axis as it is unless it says otherwise.</remarks>
public abstract record AxisScaleMode
{
    private AxisScaleMode() { }

    /// <summary>What this mode does to the X axis for data that runs from <paramref name="min"/> to <paramref name="max"/>.</summary>
    /// <param name="axis">The X axis.</param>
    /// <param name="min">The smallest X of the visible streaming series.</param>
    /// <param name="max">The largest X of the visible streaming series.</param>
    internal virtual void ScaleX(Axis axis, double min, double max) { }

    /// <summary>What this mode does to the Y axis for data that runs from <paramref name="min"/> to <paramref name="max"/>.</summary>
    /// <param name="axis">The Y axis.</param>
    /// <param name="min">The smallest Y of the visible streaming series.</param>
    /// <param name="max">The largest Y of the visible streaming series.</param>
    internal virtual void ScaleY(Axis axis, double min, double max) { }

    /// <summary>User-set limits, never auto-adjusted.</summary>
    public sealed record Fixed : AxisScaleMode;

    /// <summary>Fit all data every render — axes expand/contract to show everything. Along Y a margin of a twentieth of the
    /// span stands above and below the data (one data unit for data that is flat).</summary>
    public sealed record AutoScale : AxisScaleMode
    {
        /// <inheritdoc />
        internal override void ScaleX(Axis axis, double min, double max)
        {
            axis.Min = min;
            axis.Max = max;
        }

        /// <inheritdoc />
        internal override void ScaleY(Axis axis, double min, double max)
        {
            double margin = (max - min) * 0.05;
            if (margin < 1e-10) margin = 1.0;
            axis.Min = min - margin;
            axis.Max = max + margin;
        }
    }

    /// <summary>Show the last <see cref="WindowSize"/> X-units, scrolling as new data arrives. A window is a span of X: as the
    /// mode of a Y axis it leaves the axis as it is.</summary>
    /// <param name="WindowSize">Width of the visible X window in data units.</param>
    public sealed record SlidingWindow(double WindowSize) : AxisScaleMode
    {
        /// <inheritdoc />
        internal override void ScaleX(Axis axis, double min, double max)
        {
            axis.Min = max - WindowSize;
            axis.Max = max;
        }
    }

    /// <summary>Like <see cref="SlidingWindow"/> but only scrolls when the rightmost data point
    /// is at or past the window edge. Stays still when the user has panned away. As the mode of a Y axis it leaves the axis as
    /// it is.</summary>
    /// <param name="WindowSize">Width of the visible X window in data units.</param>
    public sealed record StickyRight(double WindowSize) : AxisScaleMode
    {
        /// <inheritdoc />
        /// <remarks>Scrolls while the data reaches to within 1 % of the window of the axis' right edge (or the axis has no right
        /// edge yet).</remarks>
        internal override void ScaleX(Axis axis, double min, double max)
        {
            double currentMax = axis.Max ?? max;
            if (max >= currentMax - WindowSize * 0.01)
            {
                axis.Min = max - WindowSize;
                axis.Max = max;
            }
        }
    }
}
