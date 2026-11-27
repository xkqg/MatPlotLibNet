// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.ColorBarRendering;

/// <summary>matplotlib's room for a colour bar in the constrained layout (<see cref="ColorBarRoom.ConstrainedMargin"/>): the axes keep
/// their cell and the bar stands in the layout's margin, a pad of 0.05 of the axes' width (of their height below them) past what the
/// axes draws on that side, in a box 0.15 of the axes across, as matplotlib's constrained layout places it
/// (<c>_constrained_layout.reposition_colorbar</c>, <c>pad0 = 0.05 if fig.get_constrained_layout()</c>) and keeps the bar's whole
/// tight box and its pad in the margin (<c>make_layout_margins</c>: <c>margin['rightcb'] += cbbbox.width + pad</c>). The pad and the
/// thickness are shares of the plot's size, which the margins narrow, so the layout keeps this room once every other edge is
/// settled.</summary>
internal sealed class ConstrainedMarginRoom : ColorBarRoom
{
    /// <inheritdoc />
    internal override bool AnchorsAxes => true;

    /// <inheritdoc />
    internal override bool SettlesAt(ColorBar bar, BarRoomStage stage) => stage == BarRoomStage.Last;

    /// <inheritdoc />
    /// <remarks>The gap is 0.05 of the plot's side across the bar (<see cref="ColorBarRoomExtensions.ConstrainedPad"/>) unless the
    /// author gave one, and the bar the largest box of its aspect <see cref="ColorBarRoomExtensions.Fraction"/> of that side across,
    /// centred along the plot.</remarks>
    internal override Rect BodyOf(ColorBar bar, Rect plotArea, Rect? cell, double clearance)
    {
        // The bar runs along the plot's side it stands at and stands past the end of the plot across it.
        bool alongX = bar.Orientation.RunsAlongX;
        double side = plotArea.LengthAlong(!alongX);
        double gap = bar.GivenPadding ?? ColorBarRoomExtensions.ConstrainedPad * side;
        var box = BoxIn(bar, ColorBarRoomExtensions.Fraction * side, plotArea.LengthAlong(alongX) * bar.Shrink);
        var along = Range1D.CentredIn(plotArea.StartAlong(alongX), plotArea.LengthAlong(alongX), box.Length);
        return Rect.Along(alongX, along.Lo, box.Length, plotArea.EndAlong(!alongX) + clearance + gap, box.Thickness);
    }

    /// <inheritdoc />
    /// <remarks>Beside the plot the farther of what the axes draws past its right spine (a twin's axis, mirrored labels, end labels)
    /// and an X tick label that stands out past it: matplotlib's bar stands past the parent's box with its decorations. Below it what
    /// the axes draws under its plot.</remarks>
    internal override double ClearanceOf<TBands>(ColorBar bar, TBands axes)
    {
        if (!bar.Beside) return ClearanceBelow(axes);
        var edges = axes.Edges();
        return Math.Max(Math.Max(edges.RightPastEdge, edges.Overhang.Right), axes.PastRightSpine());
    }

    /// <inheritdoc />
    /// <remarks>From the plot's right edge to the bar's outer edge, then its text; 0 when the plot is not known (the bar has no size
    /// yet).</remarks>
    private protected override double Beside(ColorBar bar, Rect? area, IEnumerable<ISeries> series, IRenderContext ctx, Theme theme, double clearance) =>
        area is { } plot ? PastBeside(bar, plot, null, plot.Right, series, ctx, theme, clearance) : 0;

    /// <inheritdoc />
    /// <remarks>From the plot's bottom edge to the bar's lower edge, then its text; 0 when the plot is not known.</remarks>
    private protected override double Below(ColorBar bar, Rect? area, Theme theme, double clearance) =>
        area is { } plot ? PastBelow(bar, plot, null, plot.Bottom, theme, clearance) : 0;
}
