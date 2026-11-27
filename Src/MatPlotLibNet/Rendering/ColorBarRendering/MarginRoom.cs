// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.ColorBarRendering;

/// <summary>The library's own room for a colour bar (<see cref="ColorBarRoom.Margin"/>): the axes keep their cell and the bar stands
/// in the margin, <see cref="ColorBar.Padding"/> pixels past what the axes draws on its side, <see cref="ColorBar.Width"/> thick (a bar
/// below as thick as its length over <see cref="ColorBar.Aspect"/> when that is positive). Below the plot it stands past the X tick
/// labels and the X axis title; beside it past the secondary Y axes, the mirrored Y tick labels and the end labels, so it covers none
/// of them. Its room is part of what the edges of its axes need, so the layout keeps it while it measures them.</summary>
internal sealed class MarginRoom : ColorBarRoom
{
    /// <inheritdoc />
    internal override bool SettlesAt(ColorBar bar, BarRoomStage stage) => stage == BarRoomStage.Measure;

    /// <inheritdoc />
    /// <remarks>Beside the plot as long as the plot's height times <see cref="ColorBar.Shrink"/> and centred along it; below it as
    /// long as the plot's width times the shrink.</remarks>
    internal override Rect BodyOf(ColorBar bar, Rect plotArea, Rect? cell, double clearance)
    {
        if (bar.Beside)
        {
            double length = plotArea.Height * bar.Shrink;
            return new Rect(plotArea.X + plotArea.Width + clearance + bar.Padding, Range1D.CentredIn(plotArea.Y, plotArea.Height, length).Lo, bar.Width, length);
        }

        double run = plotArea.Width * bar.Shrink;
        return new Rect(Range1D.CentredIn(plotArea.X, plotArea.Width, run).Lo, plotArea.Y + plotArea.Height + clearance + bar.Padding, run,
            ThicknessBelow(bar, plotArea.Width));
    }

    /// <inheritdoc />
    /// <remarks>Beside the plot everything the axes draws past its right spine (<see cref="RightBand.Past"/>), so the bar and that band
    /// are stacked; below it what the axes draws under its plot.</remarks>
    internal override double ClearanceOf<TBands>(ColorBar bar, TBands axes) => bar.Beside ? axes.PastRightSpine() : ClearanceBelow(axes);

    /// <inheritdoc />
    /// <remarks>The clearance, the gap, the bar and its text (<see cref="ColorBarRoomExtensions"/>' <c>TextReachBeside</c>); the plot's
    /// size is not read.</remarks>
    private protected override double Beside(ColorBar bar, Rect? area, IEnumerable<ISeries> series, IRenderContext ctx, Theme theme, double clearance) =>
        clearance + bar.Padding + bar.Width + bar.TextReachBeside(series, ctx, theme);

    /// <inheritdoc />
    /// <remarks>The clearance, the gap, the bar (as thick as the plot's width makes it, <see cref="ColorBar.Width"/> when the plot is
    /// not known) and its tick labels and label.</remarks>
    private protected override double Below(ColorBar bar, Rect? area, Theme theme, double clearance) =>
        clearance + bar.Padding + (area is { } plotted ? ThicknessBelow(bar, plotted.Width) : bar.Width) + bar.TextReachBelow(theme);

    /// <summary>The thickness of a bar below a plot area <paramref name="plotWidth"/> wide: as thick as it is long over
    /// <see cref="ColorBar.Aspect"/> when that is positive, else <see cref="ColorBar.Width"/>.</summary>
    private static double ThicknessBelow(ColorBar bar, double plotWidth) => bar.Aspect > 0 ? plotWidth * bar.Shrink / bar.Aspect : bar.Width;
}
