// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>What places an axis title beside the tick labels of its edge (<see cref="AxisTickExtensions"/>'
/// <c>TitleOffset</c>): the band the labels take, the blank gap wanted between them and the title's ink, the title's font
/// size and which part of its ink faces them.</summary>
/// <param name="TickLabelBand">From the edge to the far side of the tick labels drawn there
/// (<see cref="AxisTickExtensions"/>' <c>TickLabelBand</c>); 0 when none is drawn.</param>
/// <param name="Gap">The blank gap between the far side of the tick labels and the title's ink, in pixels.</param>
/// <param name="LabelSize">The title's font size.</param>
/// <param name="Ink">Which part of the title's ink faces the tick labels.</param>
internal readonly record struct TitleClearance(double TickLabelBand, double Gap, double LabelSize, FacingInk Ink)
{
    /// <summary>Where the title is placed when no tick label is drawn on its edge (hidden ticks, axis-less series), or
    /// <see langword="null"/> when the edge has no fixed place and the rule holds with a band of 0.</summary>
    internal double? WithoutTickLabels { get; init; }
}
