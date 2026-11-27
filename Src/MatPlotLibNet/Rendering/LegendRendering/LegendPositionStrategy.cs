// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.Layout;

namespace MatPlotLibNet.Rendering.LegendRendering;

/// <summary>
/// Abstract base for legend-positioning strategies. Each concrete subtype encodes
/// the box-placement formula for one <see cref="LegendPosition"/> value, so
/// <see cref="AxesRenderer"/> carries no switch over the positions.
/// </summary>
/// <remarks>
/// Strategy pattern via subclass polymorphism; the factory
/// <see cref="LegendPositionStrategyFactory"/> does the single static dispatch from enum
/// to instance.
/// </remarks>
public abstract record LegendPositionStrategy
{
    /// <summary>The pixel inset from the plot edge for non-outside positions under the library's own themes; a theme's legend look
    /// may name another (matplotlib's <c>legend.borderaxespad</c>).</summary>
    public const double Inset = 10;

    /// <summary>The pixel gap from the plot edge for outside positions
    /// (kept clear of spine + tick marks).</summary>
    public const double OutsideGap = 8;

    /// <summary>Computes the top-left (X, Y) pixel position of the legend box, <see cref="Inset"/> inside the plot edges.</summary>
    /// <param name="plotArea">The pixel-space rectangle that bounds the plot.</param>
    /// <param name="boxW">The pre-measured legend box width in pixels.</param>
    /// <param name="boxH">The pre-measured legend box height in pixels.</param>
    /// <returns>The top-left corner of the box, in pixels.</returns>
    public abstract Point ComputeBox(Rect plotArea, double boxW, double boxH);

    /// <summary>The top-left pixel position of a box of <paramref name="box"/>'s size standing <paramref name="inset"/> inside the plot
    /// edges it is placed against. A position of the library places by it; one derived elsewhere stands where
    /// <see cref="ComputeBox(Rect, double, double)"/> puts it.</summary>
    /// <param name="plotArea">The pixel-space rectangle that bounds the plot.</param>
    /// <param name="box">The size of the legend box.</param>
    /// <param name="inset">How far inside the plot edges it stands, in pixels.</param>
    /// <returns>The top-left corner of the box, in pixels.</returns>
    internal virtual Point Place(Rect plotArea, Size box, double inset) => ComputeBox(plotArea, box.Width, box.Height);

    /// <summary>The top-left pixel position of the legend box knowing the marks the chart drew and what the axes draws past each edge
    /// of its plot (<see cref="AxesEdgeNeeds.DrawnPast"/>). A position inside the plot that names its place stands there whatever is
    /// under it (<see cref="Place(Rect, Size, double)"/>), and <c>Best</c> overrides it to look at the marks; a position outside the
    /// plot stands its gap past what is drawn on its edge (<see cref="OutsideStrategy"/>).</summary>
    /// <param name="plotArea">The pixel-space rectangle that bounds the plot.</param>
    /// <param name="box">The size of the legend box.</param>
    /// <param name="inset">How far inside the plot edges it stands, in pixels.</param>
    /// <param name="obstacles">The marks the chart drew on this axes.</param>
    /// <param name="drawnPast">What the axes draws past each edge of the plot; 0 on every edge for a legend of the figure.</param>
    /// <returns>The top-left corner of the box, in pixels.</returns>
    internal virtual Point Place(Rect plotArea, Size box, double inset, MarkObstacles obstacles, Sides<double> drawnPast) =>
        Place(plotArea, box, inset);

    /// <summary>The room the layout keeps past the edge an outside legend stands at, beyond its box: the gap past the spine
    /// (<see cref="OutsideGap"/>) and as much again past the box.</summary>
    internal const double OutsideReserve = 2 * OutsideGap;

    /// <summary>Whether this position stands the legend outside the plot area, so the layout must keep room for it. A position
    /// inside the plot does not.</summary>
    internal virtual bool StandsOutside => false;

    /// <summary>The room the layout needs on each edge with this position's legend box: <paramref name="needs"/> as it is for a
    /// position inside the plot; an outside position keeps room for the box on the edge it stands past.</summary>
    /// <param name="box">The measured legend box.</param>
    /// <param name="needs">What the edges need without the legend.</param>
    /// <param name="past">What the edges need for what the box stands past (what the axes draws there, its colour bar with it, and the pad);
    /// 0 on every edge for a legend of the figure.</param>
    internal virtual Sides<double> ReserveFor(Size box, Sides<double> needs, Sides<double> past) => needs;

    /// <summary>The horizontal centre of the plot area (the rectangle's centre along X, whose one owner is <c>AlongExtensions</c>).</summary>
    protected static double CenterX(Rect plotArea) => plotArea.CentreAlong(true);

    /// <summary>The vertical centre of the plot area (its centre along Y).</summary>
    protected static double CenterY(Rect plotArea) => plotArea.CentreAlong(false);
}

/// <summary>A position inside the plot, placed by its inset: <see cref="ComputeBox"/> at the library's <see cref="LegendPositionStrategy.Inset"/>,
/// <see cref="Place(Rect, Size, double)"/> at the one the theme's legend look names.</summary>
internal abstract record InsideStrategy : LegendPositionStrategy
{
    /// <inheritdoc />
    public sealed override Point ComputeBox(Rect plotArea, double boxW, double boxH) => Place(plotArea, new Size(boxW, boxH), Inset);

    /// <inheritdoc />
    internal abstract override Point Place(Rect plotArea, Size box, double inset);
}

/// <summary>A legend placed at the upper right corner of the plot, <see cref="LegendPositionStrategy.Inset"/> in from the
/// edges.</summary>
internal sealed record UpperRightStrategy : InsideStrategy
{
    /// <inheritdoc />
    internal override Point Place(Rect plotArea, Size box, double inset)
        => new(plotArea.X + plotArea.Width - box.Width - inset, plotArea.Y + inset);
}

/// <summary>A legend placed at the upper left corner of the plot, <see cref="LegendPositionStrategy.Inset"/> in from the edges.</summary>
internal sealed record UpperLeftStrategy : InsideStrategy
{
    /// <inheritdoc />
    internal override Point Place(Rect plotArea, Size box, double inset)
        => new(plotArea.X + inset, plotArea.Y + inset);
}

/// <summary>A legend placed at the lower right corner of the plot, <see cref="LegendPositionStrategy.Inset"/> in from the
/// edges.</summary>
internal sealed record LowerRightStrategy : InsideStrategy
{
    /// <inheritdoc />
    internal override Point Place(Rect plotArea, Size box, double inset)
        => new(plotArea.X + plotArea.Width - box.Width - inset, plotArea.Y + plotArea.Height - box.Height - inset);
}

/// <summary>A legend placed at the lower left corner of the plot, <see cref="LegendPositionStrategy.Inset"/> in from the edges.</summary>
internal sealed record LowerLeftStrategy : InsideStrategy
{
    /// <inheritdoc />
    internal override Point Place(Rect plotArea, Size box, double inset)
        => new(plotArea.X + inset, plotArea.Y + plotArea.Height - box.Height - inset);
}

/// <summary>A legend placed at the middle of the right edge of the plot, <see cref="LegendPositionStrategy.Inset"/> in from the
/// edges. It stands for <see cref="LegendPosition.Right"/> and <see cref="LegendPosition.CenterRight"/> alike: matplotlib's
/// <c>'right'</c> is its <c>'center right'</c> (<c>Legend.codes</c>), one rule for the two positions.</summary>
internal sealed record RightStrategy : InsideStrategy
{
    /// <inheritdoc />
    internal override Point Place(Rect plotArea, Size box, double inset)
        => new(plotArea.X + plotArea.Width - box.Width - inset, Range1D.Around(CenterY(plotArea), box.Height).Lo);
}

/// <summary>A legend placed at the middle of the left edge of the plot, <see cref="LegendPositionStrategy.Inset"/> in from the
/// edges.</summary>
internal sealed record CenterLeftStrategy : InsideStrategy
{
    /// <inheritdoc />
    internal override Point Place(Rect plotArea, Size box, double inset)
        => new(plotArea.X + inset, Range1D.Around(CenterY(plotArea), box.Height).Lo);
}

/// <summary>A legend placed at the middle of the bottom edge of the plot, <see cref="LegendPositionStrategy.Inset"/> in from
/// the edges.</summary>
internal sealed record LowerCenterStrategy : InsideStrategy
{
    /// <inheritdoc />
    internal override Point Place(Rect plotArea, Size box, double inset)
        => new(Range1D.Around(CenterX(plotArea), box.Width).Lo, plotArea.Y + plotArea.Height - box.Height - inset);
}

/// <summary>A legend placed at the middle of the top edge of the plot, <see cref="LegendPositionStrategy.Inset"/> in from the
/// edges.</summary>
internal sealed record UpperCenterStrategy : InsideStrategy
{
    /// <inheritdoc />
    internal override Point Place(Rect plotArea, Size box, double inset)
        => new(Range1D.Around(CenterX(plotArea), box.Width).Lo, plotArea.Y + inset);
}

internal sealed record CenterStrategy : LegendPositionStrategy
{
    public override Point ComputeBox(Rect plotArea, double boxW, double boxH)
        => new(Range1D.Around(CenterX(plotArea), boxW).Lo, Range1D.Around(CenterY(plotArea), boxH).Lo);
}

/// <summary>A position outside the plot: the box stands <see cref="LegendPositionStrategy.OutsideGap"/> past what the axes draws on the
/// edge it stands at (<see cref="AxesEdgeNeeds.DrawnPast"/>: the left stack, the band past the right spine, the axes title, what stands
/// below the plot), centred along that edge, and the layout keeps room for it there by the same rule (<see cref="LegendPositionStrategy.ReserveFor(Size, Sides{double}, Sides{double})"/>): what
/// the edge needs for what the box stands past, the box and the gap on either side of it, and never less than the edge needs already
/// (a colour bar on that edge is drawn past it too, so the box stands past the bar; the layout keeps the room past the bar where it
/// keeps the bar's, <see cref="Rendering.Layout.ConstrainedLayoutEngine.SettleColorBars"/>). <see cref="ComputeBox"/> reads nothing drawn: the box stands
/// its gap past the plot's edge, as a legend of the figure stands past the figure's.</summary>
internal abstract record OutsideStrategy : LegendPositionStrategy
{
    /// <inheritdoc />
    public sealed override Point ComputeBox(Rect plotArea, double boxW, double boxH) => PlacePast(plotArea, new Size(boxW, boxH), default);

    /// <inheritdoc />
    internal sealed override Point Place(Rect plotArea, Size box, double inset, MarkObstacles obstacles, Sides<double> drawnPast) =>
        PlacePast(plotArea, box, drawnPast);

    /// <inheritdoc />
    internal sealed override bool StandsOutside => true;

    /// <summary>The top-left corner of the box standing its gap past <paramref name="drawnPast"/> on this position's edge.</summary>
    /// <param name="plotArea">The plot.</param>
    /// <param name="box">The size of the legend box.</param>
    /// <param name="drawnPast">What the axes draws past each edge of the plot.</param>
    /// <returns>The top-left corner, in pixels.</returns>
    private protected abstract Point PlacePast(Rect plotArea, Size box, Sides<double> drawnPast);

    /// <summary>The room an edge keeps for a box standing past what the edge needs it for: <paramref name="past"/>, the box and the gap
    /// on either side, and never less than <paramref name="need"/>.</summary>
    private protected static double RoomFor(double need, double past, double box) => Math.Max(need, past + box + OutsideReserve);
}

internal sealed record OutsideRightStrategy : OutsideStrategy
{
    private protected override Point PlacePast(Rect plotArea, Size box, Sides<double> drawnPast)
        => new(plotArea.X + plotArea.Width + drawnPast.Right + OutsideGap, Range1D.Around(CenterY(plotArea), box.Height).Lo);

    /// <inheritdoc />
    internal override Sides<double> ReserveFor(Size box, Sides<double> needs, Sides<double> past)
        => needs with { Right = RoomFor(needs.Right, past.Right, box.Width) };
}

internal sealed record OutsideLeftStrategy : OutsideStrategy
{
    private protected override Point PlacePast(Rect plotArea, Size box, Sides<double> drawnPast)
        => new(plotArea.X - drawnPast.Left - box.Width - OutsideGap, Range1D.Around(CenterY(plotArea), box.Height).Lo);

    /// <inheritdoc />
    internal override Sides<double> ReserveFor(Size box, Sides<double> needs, Sides<double> past)
        => needs with { Left = RoomFor(needs.Left, past.Left, box.Width) };
}

internal sealed record OutsideTopStrategy : OutsideStrategy
{
    private protected override Point PlacePast(Rect plotArea, Size box, Sides<double> drawnPast)
        => new(Range1D.Around(CenterX(plotArea), box.Width).Lo, plotArea.Y - drawnPast.Top - box.Height - OutsideGap);

    /// <inheritdoc />
    internal override Sides<double> ReserveFor(Size box, Sides<double> needs, Sides<double> past)
        => needs with { Top = RoomFor(needs.Top, past.Top, box.Height) };
}

internal sealed record OutsideBottomStrategy : OutsideStrategy
{
    private protected override Point PlacePast(Rect plotArea, Size box, Sides<double> drawnPast)
        => new(Range1D.Around(CenterX(plotArea), box.Width).Lo, plotArea.Y + plotArea.Height + drawnPast.Bottom + OutsideGap);

    /// <inheritdoc />
    internal override Sides<double> ReserveFor(Size box, Sides<double> needs, Sides<double> past)
        => needs with { Bottom = RoomFor(needs.Bottom, past.Bottom, box.Height) };
}

/// <summary>
/// The "best" position: the first of the places that covers the fewest of the chart's marks, tried in matplotlib's order
/// (upper right, upper left, lower left, lower right, right, centre left, lower centre, upper centre, centre), each placed by its
/// own strategy. matplotlib's tenth place, centre right, is the fifth, right (<see cref="RightStrategy"/>): a box that stands where
/// one it tried stands is never the first of the least covered, so it is tried once. With no marks to look at it is the upper
/// right, as it always was; the marks come in through <see cref="Place(Rect, Size, double, MarkObstacles, Sides{double})"/>.
/// </summary>
internal sealed record BestStrategy : LegendPositionStrategy
{
    private static readonly LegendPositionStrategy[] Places =
    [
        new UpperRightStrategy(), new UpperLeftStrategy(), new LowerLeftStrategy(), new LowerRightStrategy(),
        new RightStrategy(), new CenterLeftStrategy(), new LowerCenterStrategy(), new UpperCenterStrategy(), new CenterStrategy(),
    ];

    public override Point ComputeBox(Rect plotArea, double boxW, double boxH)
        => Places[0].ComputeBox(plotArea, boxW, boxH);

    /// <inheritdoc />
    internal override Point Place(Rect plotArea, Size box, double inset) => Places[0].Place(plotArea, box, inset);

    /// <inheritdoc />
    internal override Point Place(Rect plotArea, Size box, double inset, MarkObstacles obstacles, Sides<double> drawnPast)
    {
        var corners = new Point[Places.Length];
        var boxes = new Rect[Places.Length];
        for (int i = 0; i < Places.Length; i++)
        {
            corners[i] = Places[i].Place(plotArea, box, inset);
            boxes[i] = new Rect(corners[i].X, corners[i].Y, box.Width, box.Height);
        }
        return corners[obstacles.FirstLeastCovered(boxes)];
    }
}
