// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>Where a legend frame's fill comes from when the legend names no face colour (<see cref="LegendLook.Face"/>).</summary>
internal enum LegendFace
{
    /// <summary>The figure's background (<see cref="Theme.Background"/>).</summary>
    Figure,

    /// <summary>The axes' colour (<see cref="Theme.AxesBackground"/>), matplotlib's <c>legend.facecolor: 'inherit'</c>.</summary>
    Axes,
}

