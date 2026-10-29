// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Interaction;

/// <summary>Provides the spatial and data-range information needed by interaction modifiers
/// to convert pixel-space pointer positions into data-space coordinates.</summary>
public interface IChartLayout
{
    /// <summary>Number of axes (subplots) in the figure.</summary>
    int AxesCount { get; }

    /// <summary>Returns the pixel-space bounding rectangle of the plot area for the given axes.</summary>
    Rect GetPlotArea(int axesIndex);

    /// <summary>Returns the current data-space range displayed by the given axes.</summary>
    DataRange GetDataRange(int axesIndex);

    /// <summary>Returns the current data range of every Y axis past the primary for the given subplot, right
    /// slots then left slots, in stack order, however many there are, so a zoom or a pan can move each of them by its
    /// own range and not only the primary's.
    /// <para>The numbering is OUTWARD only, and it is a contract: <see cref="SecondaryAxisRange.SlotIndex"/> 0 on the left
    /// is the first axis past the primary Y axis, 0 on the right is the first right axis. The primary Y axis is not in this
    /// list, so a range read here is never the primary's (<see cref="Models.YAxisStack.SlotAtOutward"/> is the one place that maps
    /// the index to an axis of the container). A browser tab that holds an older drawing sends these indices back, so they
    /// keep their meaning across releases.</para>
    /// Default interface member: a host with nothing but a plot area and a primary range has no secondary axes to
    /// report, so every existing <see cref="IChartLayout"/> implementer keeps working unchanged.</summary>
    IReadOnlyList<SecondaryAxisRange> GetSecondaryDataRanges(int axesIndex) => [];

    /// <summary>Returns the axes index whose plot area contains the given pixel position,
    /// or <c>null</c> if the point is outside every plot area.</summary>
    int? HitTestAxes(double pixelX, double pixelY);

    /// <summary>Returns the series index of the legend item hit at the given pixel position
    /// within the specified axes, or <c>null</c> if no legend item is hit.</summary>
    int? HitTestLegendItem(double pixelX, double pixelY, int axesIndex);
}
