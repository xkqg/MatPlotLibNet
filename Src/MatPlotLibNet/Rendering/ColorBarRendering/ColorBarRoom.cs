// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.ColorBarRendering;

/// <summary>Where the colour bars of a figure stand and the room they take, one value of three by the figure's theme and layout
/// (<see cref="Under"/>): the library's margin (<see cref="Margin"/>), matplotlib's slot of the axes' cell (<see cref="Slot"/>) and
/// matplotlib's constrained margin (<see cref="ConstrainedMargin"/>). Each says where its bar's body stands, what of the axes it stands
/// past, how far its bar reaches with its text, and at which point of the layout that room is kept; the drawing and the layouts ask
/// the value and never test which one it is. What every bar has whatever its room (its text, the part of a cell it leaves) is
/// <see cref="ColorBarRoomExtensions"/>.</summary>
internal abstract class ColorBarRoom
{
    /// <summary>The library's own themes: the axes keep their cell and the bar stands in the margin, <see cref="ColorBar.Padding"/>
    /// pixels past what the axes draws on its side, <see cref="ColorBar.Width"/> thick.</summary>
    internal static ColorBarRoom Margin { get; } = new MarginRoom();

    /// <summary>The matplotlib themes in the default and the tight layout: the bar takes a slot of the axes' cell, as matplotlib's
    /// <c>colorbar.make_axes_gridspec</c> does.</summary>
    internal static ColorBarRoom Slot { get; } = new SlotRoom();

    /// <summary>The matplotlib themes in the constrained layout: the bar stands in the layout's margin, a pad of the axes' size past
    /// what the axes draws there, as matplotlib's constrained layout places a bar made by <c>colorbar.make_axes</c>
    /// (<c>_constrained_layout.reposition_colorbar</c>).</summary>
    internal static ColorBarRoom ConstrainedMargin { get; } = new ConstrainedMarginRoom();

    /// <summary>The room of the colour bars under <paramref name="theme"/>: matplotlib's under a theme that gives a bar its room
    /// (<see cref="ColorBarLook.TakesRoom"/>), in a slot or, under the constrained layout, in its margin; the library's margin
    /// otherwise.</summary>
    /// <param name="theme">The figure's theme.</param>
    /// <param name="constrained">Whether the figure has the constrained layout.</param>
    /// <returns>The room.</returns>
    internal static ColorBarRoom Under(Theme theme, bool constrained) =>
        !theme.Looks.ColorBar.TakesRoom ? Margin : constrained ? ConstrainedMargin : Slot;

    /// <summary>The room the bars of a draw stand in: the one its layout settled (<see cref="FigureRanges.BarRoom"/>), or, outside a
    /// draw, the one <paramref name="theme"/> gives in the default layout.</summary>
    /// <param name="ranges">The data ranges of the draw; <see langword="null"/> outside a draw.</param>
    /// <param name="theme">The theme the axes is drawn under.</param>
    /// <returns>The room.</returns>
    internal static ColorBarRoom Of(FigureRanges? ranges, Theme theme) => ranges?.BarRoom ?? Under(theme, constrained: false);

    /// <summary>Whether the bar takes its room from the cell of its axes (or the figure's from the box of every cell), which the axes
    /// then keep less of (<see cref="ColorBarRoomExtensions"/>' <c>KeptOf</c>).</summary>
    internal virtual bool TakesCell => false;

    /// <summary>Whether an axes whose shape is fixed is fitted against its bar, as matplotlib anchors the parent of a bar
    /// (<c>panchor</c>); the library's margin leaves the shape centred in its cell.</summary>
    internal virtual bool AnchorsAxes => false;

    /// <summary>Whether the room of <paramref name="bar"/> is kept at <paramref name="stage"/> of the layout.</summary>
    /// <param name="bar">The bar.</param>
    /// <param name="stage">The point of the layout.</param>
    /// <returns><see langword="true"/> when the room is kept at that point.</returns>
    internal abstract bool SettlesAt(ColorBar bar, BarRoomStage stage);

    /// <summary>The rectangle the bar's body (its gradient and its frame, with its end wedges) stands in.</summary>
    /// <param name="bar">The bar.</param>
    /// <param name="plotArea">The plot area of the axes.</param>
    /// <param name="cell">The cell the layout gave the axes; <see langword="null"/> when it is not known (it is then the one the plot
    /// area was kept from). Only a bar that takes its cell reads it.</param>
    /// <param name="clearance">How far past the plot area the axes draws on the bar's side (<see cref="ClearanceOf"/>), which a bar in
    /// a margin stands past.</param>
    /// <returns>The rectangle of the body.</returns>
    internal abstract Rect BodyOf(ColorBar bar, Rect plotArea, Rect? cell, double clearance);

    /// <summary>How far past the plot area the bar stands clear of what the axes draws on its side: below the plot, the lowest thing
    /// the axes draws under it (<see cref="ClearanceBelow"/>); beside it what the room stands past there; 0 for a bar in a slot. Only
    /// what the room reads is asked of <paramref name="axes"/>. The one rule the drawing (<see cref="BodyOf"/>) and the layouts read it
    /// by.</summary>
    /// <typeparam name="TBands">What answers for the axes: its renderer, the layout's measure of it, or values measured already.</typeparam>
    /// <param name="bar">The bar.</param>
    /// <param name="axes">What its axes draws past its spines.</param>
    /// <returns>The clearance, in pixels.</returns>
    internal abstract double ClearanceOf<TBands>(ColorBar bar, TBands axes) where TBands : IAxesBands;

    /// <summary>How far below the plot a bar below it stands clear of what the axes draws there, under every room that stands a bar
    /// past it: the lowest thing the axes draws under its plot, its X axis title or else its X tick labels
    /// (<see cref="EdgeBands.BottomExtent"/>).</summary>
    /// <param name="axes">What the axes draws past its spines.</param>
    private protected static double ClearanceBelow<TBands>(TBands axes) where TBands : IAxesBands => axes.Ends().BottomExtent;

    /// <summary>How far a bar beside the plot reaches with its tick labels and its label past <paramref name="area"/> (the plot, or
    /// for a bar that takes its cell the cell); 0 for a bar below.</summary>
    /// <param name="bar">The bar.</param>
    /// <param name="area">The plot area, or the cell for a bar that takes it; <see langword="null"/> when it is not known.</param>
    /// <param name="series">The series that back the bar: the axes' own, or every subplot's for the bar of the figure.</param>
    /// <param name="ctx">The context text is measured with.</param>
    /// <param name="theme">The theme of the figure.</param>
    /// <param name="clearance">What the axes draws past its right spine (<see cref="ClearanceOf"/>).</param>
    /// <returns>The reach, in pixels.</returns>
    internal double ReachBeside(ColorBar bar, Rect? area, IEnumerable<ISeries> series, IRenderContext ctx, Theme theme, double clearance) =>
        bar.Beside ? Beside(bar, area, series, ctx, theme, clearance) : 0;

    /// <summary>How far a bar below the plot reaches with its tick labels and its label past <paramref name="area"/> (the plot, or for
    /// a bar that takes its cell the cell); 0 for a bar beside.</summary>
    /// <param name="bar">The bar.</param>
    /// <param name="area">The plot area, or the cell for a bar that takes it; <see langword="null"/> when it is not known.</param>
    /// <param name="theme">The theme of the figure.</param>
    /// <param name="clearance">What the axes draws under its plot (<see cref="ClearanceOf"/>).</param>
    /// <returns>The reach, in pixels.</returns>
    internal double ReachBelow(ColorBar bar, Rect? area, Theme theme, double clearance) =>
        bar.Beside ? 0 : Below(bar, area, theme, clearance);

    /// <summary>How far past the edge of <paramref name="plot"/> on its own side the bar reaches with its tick labels and its label, as
    /// it is drawn: from that edge to the far side of its body (<see cref="BodyOf"/>, standing by <paramref name="area"/>), then its text.
    /// What an outside legend on that edge stands past (<see cref="AxesEdgeNeeds.DrawnPast"/>): the axes' own bar stands by the plot
    /// itself, the figure's by the box of every plot (<see cref="FigureBarStand"/>).</summary>
    /// <param name="bar">The bar.</param>
    /// <param name="area">The plot area the bar stands by: its axes' plot, or the box of every plot for the figure's bar.</param>
    /// <param name="cell">The cell the bar takes its room from; <see langword="null"/> when it is not known.</param>
    /// <param name="plot">The plot past whose edge the reach is measured.</param>
    /// <param name="series">The series that back the bar.</param>
    /// <param name="ctx">The context text is measured with.</param>
    /// <param name="theme">The theme of the figure.</param>
    /// <param name="clearance">How far past <paramref name="area"/> the bar stands clear of what is drawn on its side
    /// (<see cref="ClearanceOf"/>).</param>
    /// <returns>The reach past the plot's edge, in pixels.</returns>
    internal double PastPlot(ColorBar bar, Rect area, Rect? cell, Rect plot, IEnumerable<ISeries> series, IRenderContext ctx, Theme theme,
        double clearance) =>
        bar.Beside ? PastBeside(bar, area, cell, plot.Right, series, ctx, theme, clearance) : PastBelow(bar, area, cell, plot.Bottom, theme, clearance);

    /// <summary>From <paramref name="edge"/> to the far side of the body of a bar beside <paramref name="area"/>, then its text
    /// (<see cref="PastPlot"/>).</summary>
    private protected double PastBeside(ColorBar bar, Rect area, Rect? cell, double edge, IEnumerable<ISeries> series, IRenderContext ctx,
        Theme theme, double clearance) =>
        BodyOf(bar, area, cell, clearance).Right - edge + bar.TextReachBeside(series, ctx, theme);

    /// <summary>From <paramref name="edge"/> to the lower side of the body of a bar below <paramref name="area"/>, then its text
    /// (<see cref="PastPlot"/>).</summary>
    private protected double PastBelow(ColorBar bar, Rect area, Rect? cell, double edge, Theme theme, double clearance) =>
        BodyOf(bar, area, cell, clearance).Bottom - edge + bar.TextReachBelow(theme);

    /// <summary>How far the bar reaches past <paramref name="area"/> on its own side: <see cref="ReachBeside"/> for a bar beside the
    /// plot, <see cref="ReachBelow"/> for one below it.</summary>
    /// <inheritdoc cref="ReachBeside"/>
    internal double ReachOf(ColorBar bar, Rect? area, IEnumerable<ISeries> series, IRenderContext ctx, Theme theme, double clearance) =>
        bar.Beside ? Beside(bar, area, series, ctx, theme, clearance) : Below(bar, area, theme, clearance);

    /// <summary>The reach of a bar beside the plot under this room.</summary>
    private protected abstract double Beside(ColorBar bar, Rect? area, IEnumerable<ISeries> series, IRenderContext ctx, Theme theme, double clearance);

    /// <summary>The reach of a bar below the plot under this room.</summary>
    private protected abstract double Below(ColorBar bar, Rect? area, Theme theme, double clearance);

    /// <summary>The rectangle each axes' bar reaches past while the layout settles it: the plot areas under
    /// <paramref name="spacing"/>; a bar that takes its cell reads the cells.</summary>
    /// <param name="cells">The grid the subplots stand on.</param>
    /// <param name="spacing">The spacing being settled.</param>
    /// <returns>The rectangle of each axes, in the order of the grid.</returns>
    internal virtual List<Rect> AreasAt(FigureGrid cells, SubPlotSpacing spacing) => cells.PlotRects(spacing.MarginTop, spacing);

    /// <summary>The rectangle the figure's own bar reaches past while the layout settles it: the box of every plot area; a bar that
    /// takes its room from the cells reads the box of every cell before it took it.</summary>
    /// <param name="cells">The grid the subplots stand on.</param>
    /// <param name="spacing">The spacing being settled.</param>
    /// <param name="areas">The rectangles of <see cref="AreasAt"/> under <paramref name="spacing"/>.</param>
    /// <returns>The rectangle, or <see langword="null"/> when no area is left.</returns>
    internal virtual Rect? FigureAreaAt(FigureGrid cells, SubPlotSpacing spacing, List<Rect> areas) => Rect.Enclosing(areas);

    /// <summary>The thickness and the length of the bar in a box <paramref name="across"/> by <paramref name="along"/>: the largest box
    /// of its aspect in it (matplotlib's <c>set_box_aspect</c>), shortened when the box is too thin for it, unless the author gave its
    /// thickness; <see cref="ColorBar.Width"/> thick for an aspect of 0 or less.</summary>
    private protected static BarBox BoxIn(ColorBar bar, double across, double along)
    {
        if (bar.GivenWidth is { } given) return new BarBox(given, along);
        if (bar.Aspect <= 0) return new BarBox(bar.Width, along);
        double thickness = along / bar.Aspect;
        return thickness > across ? new BarBox(across, across * bar.Aspect) : new BarBox(thickness, along);
    }

    /// <summary>The thickness and the length of a colour bar's body.</summary>
    /// <param name="Thickness">Across the bar.</param>
    /// <param name="Length">Along the bar.</param>
    private protected readonly record struct BarBox(double Thickness, double Length);
}
