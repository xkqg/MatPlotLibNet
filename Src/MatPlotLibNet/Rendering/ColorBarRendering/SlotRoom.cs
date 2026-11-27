// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.ColorBarRendering;

/// <summary>matplotlib's room for a colour bar in the default and the tight layout (<see cref="ColorBarRoom.Slot"/>), as
/// <c>fig.colorbar(mappable, ax=ax)</c> takes it (<c>colorbar.make_axes_gridspec</c>, <c>fraction=0.15</c>, <c>aspect=20</c>,
/// <c>pad</c> 0.05 beside the axes and 0.15 below them): the cell the layout gives the axes is split into the axes, a gap and a slot of
/// 0.15 of its width (of its height for a bar below), and the bar stands in the slot, against the axes' side, as thick as it is long
/// over <see cref="ColorBar.Aspect"/>, as long as the slot times <see cref="ColorBar.Shrink"/> and centred along it. The bar of the
/// figure takes its room the same way from the box that holds the cells of every subplot (<c>colorbar.make_axes</c>). What reaches past
/// the cell is kept by the layout once the side the bar stands on is settled.</summary>
internal sealed class SlotRoom : ColorBarRoom
{
    /// <inheritdoc />
    internal override bool TakesCell => true;

    /// <inheritdoc />
    internal override bool AnchorsAxes => true;

    /// <inheritdoc />
    /// <remarks>A bar beside the axes once the sides are measured, one below them once the top and the bottom are.</remarks>
    internal override bool SettlesAt(ColorBar bar, BarRoomStage stage) => stage == (bar.Beside ? BarRoomStage.Sides : BarRoomStage.Ends);

    /// <inheritdoc />
    /// <remarks>The bar stands against the axes in the last <see cref="ColorBarRoomExtensions.Fraction"/> of the cell and centred along
    /// it; the clearance is not read.</remarks>
    internal override Rect BodyOf(ColorBar bar, Rect plotArea, Rect? cell, double clearance)
    {
        var whole = cell ?? CellAround(bar, plotArea);
        // The bar runs along the cell's side it stands at, in the last part of the cell across it.
        bool alongX = bar.Orientation.RunsAlongX;
        var box = BoxIn(bar, ColorBarRoomExtensions.Fraction * whole.LengthAlong(!alongX), whole.LengthAlong(alongX) * bar.Shrink);
        var along = Range1D.CentredIn(whole.StartAlong(alongX), whole.LengthAlong(alongX), box.Length);
        return Rect.Along(alongX, along.Lo, box.Length,
            whole.StartAlong(!alongX) + whole.LengthAlong(!alongX) * (1 - ColorBarRoomExtensions.Fraction), box.Thickness);
    }

    /// <inheritdoc />
    /// <remarks>0: the bar has its slot, past nothing the axes draws.</remarks>
    internal override double ClearanceOf<TBands>(ColorBar bar, TBands axes) => 0;

    /// <inheritdoc />
    /// <remarks>Past the right edge of the cell (the box of every cell for the bar of the figure): 0 when the text stays inside the
    /// slot, the room a tight layout keeps past the cell otherwise.</remarks>
    private protected override double Beside(ColorBar bar, Rect? area, IEnumerable<ISeries> series, IRenderContext ctx, Theme theme, double clearance)
    {
        if (area is not { } cell) return 0;
        var body = BodyOf(bar, bar.KeptOf(cell), cell, 0);
        double end = body.X + body.Width + bar.TextReachBeside(series, ctx, theme);
        return Math.Max(0, end - (cell.X + cell.Width));
    }

    /// <inheritdoc />
    /// <remarks>Past the bottom edge of the cell: 0 when the text stays inside the slot.</remarks>
    private protected override double Below(ColorBar bar, Rect? area, Theme theme, double clearance)
    {
        if (area is not { } cell) return 0;
        var body = BodyOf(bar, bar.KeptOf(cell), cell, 0);
        return Math.Max(0, body.Y + body.Height + bar.TextReachBelow(theme) - (cell.Y + cell.Height));
    }

    /// <inheritdoc />
    /// <remarks>The cells, before any of them is given to the bar of its axes.</remarks>
    internal override List<Rect> AreasAt(FigureGrid cells, SubPlotSpacing spacing) => cells.Cells(spacing.MarginTop, spacing);

    /// <inheritdoc />
    /// <remarks>The box that holds the cells of every subplot before the figure's bar took its room
    /// (<see cref="FigureGrid.FigureBarCell"/>).</remarks>
    internal override Rect? FigureAreaAt(FigureGrid cells, SubPlotSpacing spacing, List<Rect> areas) => cells.FigureBarCell(spacing.MarginTop, spacing);

    /// <summary>The cell an axes part <paramref name="kept"/> was kept from when the bar took its room: the inverse of
    /// <see cref="ColorBarRoomExtensions"/>' <c>KeptOf</c>, for a bar drawn by a renderer that was not told its cell.</summary>
    private static Rect CellAround(ColorBar bar, Rect kept)
    {
        const double Fraction = ColorBarRoomExtensions.Fraction;
        if (bar.Beside)
        {
            double width = bar.GivenPadding is { } gap ? (kept.Width + gap) / (1 - Fraction) : kept.Width / (1 - Fraction - ColorBarRoomExtensions.PadBeside);
            return new Rect(kept.X, kept.Y, width, kept.Height);
        }

        double height = bar.GivenPadding is { } below ? (kept.Height + below) / (1 - Fraction) : kept.Height / (1 - Fraction - ColorBarRoomExtensions.PadBelow);
        return new Rect(kept.X, kept.Y, kept.Width, height);
    }
}
