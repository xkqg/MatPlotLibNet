// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Layout;

/// <summary>What one X axis draws below the edge it hangs from, each band measured from that edge: its tick labels and its title past
/// them. The one rule for how far the axis reaches below (<see cref="Extent"/>), which the primary X axis' measure and draw, the linked
/// X axes below the plot (<see cref="CartesianAxisPlacement.PlaceBottomAxes"/>) and the measured bands (<see cref="EdgeBands.BottomExtent"/>)
/// read.</summary>
/// <param name="TickLabels">From the edge to the far side of the tick labels (<see cref="Models.TickConfig.TickLabelBand"/>); 0 when
/// none is drawn.</param>
/// <param name="Title">From the edge to the far side of the axis title below them (<see cref="AxesTitleExtensions"/>' <c>BottomTitleExtent</c>);
/// 0 when the axis has no title.</param>
internal readonly record struct BandsBelowAxis(double TickLabels, double Title)
{
    /// <summary>From the edge to the far side of the lowest thing the axis draws: its title when it has one (it stands past the tick
    /// labels), else its tick labels; 0 when it draws neither.</summary>
    internal double Extent => Title > 0 ? Title : TickLabels;
}
