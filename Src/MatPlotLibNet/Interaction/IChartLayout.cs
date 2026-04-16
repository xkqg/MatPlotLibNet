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
    (double XMin, double XMax, double YMin, double YMax) GetDataRange(int axesIndex);

    /// <summary>Returns the axes index whose plot area contains the given pixel position,
    /// or <c>null</c> if the point is outside every plot area.</summary>
    int? HitTestAxes(double pixelX, double pixelY);

    /// <summary>Returns the series index of the legend item hit at the given pixel position
    /// within the specified axes, or <c>null</c> if no legend item is hit.</summary>
    int? HitTestLegendItem(double pixelX, double pixelY, int axesIndex);
}
