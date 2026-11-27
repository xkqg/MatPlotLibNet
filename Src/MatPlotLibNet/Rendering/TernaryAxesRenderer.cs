// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>Renders axes in the ternary coordinate system: an equilateral triangle that a composition of three parts is placed
/// in, with a grid at every fifth and the parts named at the corners.
/// <para>The parts are named by the labels the axes already carries: the part at the apex by the Y label, the part at the
/// bottom left by the Z label, the part at the bottom right by the X label. The ticks run counter-clockwise, the way mpltern
/// draws them: the right-hand part along the base from left to right, the top part up the right edge, the left-hand part down
/// the left edge. A line of the grid joins the edge its part is ticked on to the next edge round.</para>
/// <para>The plot area this is given is the triangle's own box, and it is the
/// whole of the frame: the view transform is an ordinary transform over a frame one unit wide,
/// so the hit test and the pointer conversion read it like any other. The tick labels and the names of the corners are drawn
/// outside the box, in the margins of the figure.</para></summary>
public sealed class TernaryAxesRenderer : AxesRenderer
{
    /// <summary>Initializes a new ternary axes renderer.</summary>
    /// <param name="axes">The axes model to render.</param>
    /// <param name="plotArea">The pixel-space box of the triangle.</param>
    /// <param name="ctx">The drawing surface to emit primitives onto.</param>
    /// <param name="theme">The active visual theme.</param>
    public TernaryAxesRenderer(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme)
        : base(axes, plotArea, ctx, theme) { }

    /// <inheritdoc />
    /// <remarks>The frame one unit wide and √3/2 high over the plot area, whatever the series: nothing zooms into a triangle.</remarks>
    internal override DataTransform? ComputeViewTransform() => new(0, 1, 0, TernaryFrame.Height, PlotArea);

    /// <inheritdoc />
    /// <remarks>A ternary axes draws its patches in the triangle's own frame, one unit wide (<see cref="TernaryPointExtensions.InFrame"/> gives a
    /// composition's place), through the transform its series are placed with.</remarks>
    internal override bool DrawsPatches => true;

    /// <inheritdoc />
    /// <remarks>The grid (<see cref="DrawGrid"/>) or a patch, each patch cut off at the triangle (<see cref="_patchClip"/>), as mpltern
    /// clips an artist to the axes patch: a patch can stand anywhere in the frame, where a series' composition always lies in the
    /// triangle.</remarks>
    private protected override int DrawLayerAt(in SeriesPass pass, ReadOnlySpan<LayerStep> steps, int at)
    {
        if (steps[at].Layer == DataLayer.Grid) DrawGrid();
        else new PatchDrawing(Axes, Ctx, Theme).Draw(steps[at].Index, new TransformPlacement(DrawnTransform!), _patchClip);
        return at;
    }

    /// <summary>The triangle of the last render with its box, which the patches are cut off at; <see langword="null"/> for an axes
    /// with no patch.</summary>
    private ClipRegion? _patchClip;

    /// <inheritdoc />
    public override void Render()
    {
        var view = new DataTransform(0, 1, 0, TernaryFrame.Height, PlotArea);
        DrawnTransform = view;
        Point Place(TernaryPoint composition)
        {
            var frame = composition.InFrame();
            return view.DataToPixel(frame.X, frame.Y);
        }

        Point[] corners = [Place(new(1, 0, 0)), Place(new(0, 1, 0)), Place(new(0, 0, 1))];
        _patchClip = Axes.Patches.Count == 0 ? null : new ClipRegion(PlotArea,
            [new MoveToSegment(corners[0]), new LineToSegment(corners[1]), new LineToSegment(corners[2]), new CloseSegment()]);
        Ctx.DrawPolygon(corners, ShapeStyle.Filled(Theme.AxesBackground));

        // The grid, the patches and the series, each where the theme puts it (DrawLayerAt).
        RenderLayers(null, null);

        Ctx.DrawPolygon(corners, ShapeStyle.Outlined(Theme.ForegroundText, LineWidthRule.OwnPixel.Resting));

        var tickFont = TickFont();
        Ctx.DrawTernaryTicks(Place, tickFont, fraction => fraction.InvariantText("0.#"));
        DrawCornerNames(corners, tickFont);

        RenderLegend();
        RenderTitle();
    }

    /// <summary>The grid at its step of the layered pass: the lines of the grid at every fifth, in the grid of the theme with the
    /// author's change on top, at the z-order of mpltern's three axes (1.5 under the default style, over a scatter and under a line; 2.5
    /// under classic; under everything under the library's own themes). The tick labels stand outside the triangle and are drawn with
    /// the frame.</summary>
    private void DrawGrid()
    {
        var grid = Axes.ShapeGridUnder(Theme);
        if (!grid.Visible) return;

        var view = DrawnTransform!;
        Ctx.DrawTernaryGrid(composition =>
        {
            var frame = composition.InFrame();
            return view.DataToPixel(frame.X, frame.Y);
        }, grid.Stroke);
    }

    /// <summary>The names of the parts at their corners: above the apex, and under the two base corners, clear of the tick
    /// labels. The title of the axes is raised above the name at the apex.</summary>
    private void DrawCornerNames(Point[] corners, Font tickFont)
    {
        var apex = corners[0];
        var bottomLeft = corners[1];
        var bottomRight = corners[2];
        double belowBase = tickFont.Size + TernaryDrawingExtensions.TickGap + 4;

        if (Axes.TitleOf(Axes.YAxis, Theme) is { } top)
        {
            var font = Axes.YAxis.TitleFontUnder(Theme);
            Ctx.Draw(TextLabel.Of(top), font, new TextPlacement(new Point(apex.X, apex.Y - 6), TextAlignment.Center));
            MeasuredTopTickBand = font.Size + 6;
        }

        if (Axes.TitleOf(Axes.ZAxis, Theme) is { } left)
        {
            var font = Axes.ZAxis.TitleFontUnder(Theme);
            Ctx.Draw(TextLabel.Of(left), font, new TextPlacement(new Point(bottomLeft.X, bottomLeft.Y + belowBase), TextAlignment.Center) { Vertical = TextVerticalAlignment.Top });
        }

        if (Axes.TitleOf(Axes.XAxis, Theme) is { } right)
        {
            var font = Axes.XAxis.TitleFontUnder(Theme);
            Ctx.Draw(TextLabel.Of(right), font, new TextPlacement(new Point(bottomRight.X, bottomRight.Y + belowBase), TextAlignment.Center) { Vertical = TextVerticalAlignment.Top });
        }
    }
}
