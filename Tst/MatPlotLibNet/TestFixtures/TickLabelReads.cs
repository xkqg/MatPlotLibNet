// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests;

/// <summary>One upright tick label as the renderer drew it: what it reads and where it stands along the edge it belongs to (its X above
/// or below the plot, its Y beside it).</summary>
/// <param name="Text">What the label reads.</param>
/// <param name="Along">Where the label stands along its edge, in pixels.</param>
public readonly record struct TickLabel(string Text, double Along);

/// <summary>How a test reads the tick labels of an edge off the calls a renderer made.</summary>
internal static class TickLabelReads
{
    extension(RecordingRenderContext ctx)
    {
        /// <summary>The upright centred labels drawn above the top spine of a plot, in the order drawn, each at its X.</summary>
        /// <param name="plot">The plot the labels were drawn around.</param>
        public List<TickLabel> LabelsAbove(Rect plot) =>
            [.. ctx.Of<DrawTextCall>()
                .Where(a => a.Rotation == 0 && a.Alignment == TextAlignment.Center && a.Position.Y < plot.Y)
                .Select(a => new TickLabel(a.Text, a.Position.X))];

        /// <summary>The upright centred labels drawn below the bottom spine of a plot, in the order drawn, each at its X.</summary>
        /// <param name="plot">The plot the labels were drawn around.</param>
        public List<TickLabel> LabelsBelow(Rect plot) =>
            [.. ctx.Of<DrawTextCall>()
                .Where(a => a.Rotation == 0 && a.Alignment == TextAlignment.Center && a.Position.Y > plot.Y + plot.Height)
                .Select(a => new TickLabel(a.Text, a.Position.X))];
    }
}
