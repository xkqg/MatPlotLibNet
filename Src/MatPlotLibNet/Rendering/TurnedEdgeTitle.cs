// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>The title of a left or a right edge (<see cref="EdgeTitle.Turned"/>): turned a quarter, centred along the plot's height,
/// standing past the tick labels by the one band rule every Y axis shares (<see cref="YAxisTitleExtensions"/>, the container's gap).</summary>
internal sealed class TurnedEdgeTitle : EdgeTitle
{
    /// <inheritdoc />
    internal override void Draw(IRenderContext ctx, Axes axes, Axis axis, EdgeTerms edge, Theme theme, Rect plot, double edgeLine,
        double tallestTickLabel) =>
        ctx.DrawYAxisTitle(axis, axes.TitleOf(axis, theme),
            axes.YAxes.TitleInputUnder(theme, edge.ContainerSide!.Value, axis.MajorTicksUnder(theme).TickLabelBand(tallestTickLabel)),
            new Point(edgeLine, plot.Center.Y));
}
