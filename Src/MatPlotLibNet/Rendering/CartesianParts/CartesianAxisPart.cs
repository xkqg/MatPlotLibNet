// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.CartesianParts;

/// <summary>
/// Draws ONE axis of a Cartesian axes, on any edge: the primary X and Y axes and their opposite edges, a Y axis stacked past the primary on
/// either side, the top X axis and a linked X axis below the plot. Its series, its line, its major and minor tick marks and labels, the
/// offset text, and its title. Every axis is drawn by the same steps, so what one axis can do (a locator, a log scale, minor ticks, turned
/// tick labels, labels kept off its own side, a title with math markup) every axis does; where an axis stands, which way it faces and what it
/// draws as its own are values of its <see cref="PlacedAxis"/> and of the terms of its edge (<see cref="EdgeTerms"/>), so nothing here asks
/// which edge it is on.
/// </summary>
/// <remarks>
/// <para>The renderer draws an axis past the primary whole with <see cref="Render"/>. The primary axes go through the same part in the steps
/// the axes' own drawing order puts them in: their tick labels and marks (<see cref="RenderTicks"/>; the marks at the axis' place among the
/// data under the matplotlib themes), their titles with the axis labels (<see cref="RenderTitle"/>); their series are drawn by the axes'
/// own data layer. The part is made by a factory (<see cref="Of"/>, <see cref="PrimaryX"/>), which makes its tick drawing once.</para>
/// <para>Each series is drawn by the one loop of a series pass (<see cref="SeriesPass.DrawSteps"/>) through the renderer it draws for, inside
/// the group keyed by the series' identity slot, with what belongs inside that group (its end label).</para>
/// <para>A value on the stack: a draw makes a part for each axis it draws and allocates none.</para>
/// </remarks>
internal readonly struct CartesianAxisPart
{
    /// <summary>How many tick positions <see cref="TurnedToFit"/> keeps on the stack while it decides the labels' rotation; a longer run
    /// of ticks takes an array.</summary>
    private const int MaxTicksOnTheStack = 64;

    private readonly CartesianPartContext _context;
    private readonly PlacedAxis _placed;
    private readonly ILayerHost _host;

    private CartesianAxisPart(in CartesianPartContext context, in PlacedAxis placed, in TickDrawContext draw, ILayerHost host)
    {
        _context = context;
        _placed = placed;
        _host = host;
        TickDraw = draw;
    }

    /// <summary>The part of <paramref name="placed"/>, its ticks drawn by its own tick drawing (<see cref="PlacedAxis.TickDraw"/>).</summary>
    /// <param name="context">What the render draws with.</param>
    /// <param name="placed">The axis as this render places it.</param>
    /// <param name="host">The renderer the axis' series are drawn for.</param>
    /// <returns>The part.</returns>
    internal static CartesianAxisPart Of(in CartesianPartContext context, in PlacedAxis placed, ILayerHost host) =>
        new(context, placed, placed.TickDraw(context.Theme, context.Axes.Spines), host);

    /// <summary>The part of the primary X axis, its labels turned to fit (<see cref="TurnedToFit"/>).</summary>
    /// <param name="context">What the render draws with.</param>
    /// <param name="placed">The primary X axis as this render places it (<see cref="CartesianAxisPlacement.PlacePrimaryX"/>).</param>
    /// <param name="host">The renderer the axis draws for.</param>
    /// <returns>The part.</returns>
    internal static CartesianAxisPart PrimaryX(in CartesianPartContext context, in PlacedAxis placed, ILayerHost host) =>
        new(context, placed, TurnedToFit(context.Ctx, placed, context.Theme, context.Axes.Spines), host);

    /// <summary>How the axis' major ticks are drawn: facing its edge and hung from it.</summary>
    internal TickDrawContext TickDraw { get; }

    /// <summary>The axis' ticks as they stand on its edge (<see cref="PlacedAxis.Run"/>).</summary>
    internal TickRun Run => _placed.Run;

    /// <summary>The axis.</summary>
    private Axis Axis => _placed.Axis;

    /// <summary>The axes the axis belongs to.</summary>
    private Axes Axes => _context.Axes;

    /// <summary>The plot area.</summary>
    private Rect PlotArea => _context.PlotArea;

    /// <summary>The surface drawn on.</summary>
    private IRenderContext Ctx => _context.Ctx;

    /// <summary>The active theme.</summary>
    private Theme Theme => _context.Theme;

    /// <summary>Draws an axis past the primary, whole: why a linked axis draws no ticks, when its link cannot place the primary's range
    /// (<see cref="PlacedAxis.Diagnostic"/>, said here, once per render: the layout and the views place the axis but never draw it), its
    /// own line, its series, its major ticks, its minor ticks, then its title past the labels drawn.</summary>
    /// <returns>How far the tick labels drawn on the axis' own edge reach past the mark and the pad; 0 when none was drawn. The band
    /// above the top X axis is read from it.</returns>
    internal double Render()
    {
        if (_placed.Diagnostic is { } diagnostic) Diagnostics.ChartDiagnostics.Emit(diagnostic);
        if (_placed.Line is { } spine) DrawLine(spine);
        DrawSeries();
        double reach = RenderTicks(TickInk.Both, OppositeTicks.None).OwnReach;
        RenderTitle(reach);
        return reach;
    }

    /// <summary>Draws the axis' own series through its transform, in the z-order the theme gives them, each in the colour its identity
    /// slot holds, in its group and clipped to the plot (<see cref="SeriesPass.OverPlot"/>).</summary>
    private void DrawSeries()
    {
        var host = _host;
        (SeriesPass.OverPlot(Axes, _context.Pass, _placed.Transform) with
        {
            Series = _placed.Series,
            Pair = _placed.Pair,
            FirstIndexInPair = _placed.FirstIndexInPair,
        }).Draw(ref host);
    }

    /// <summary>The axis' line across the plot at its edge, as matplotlib's secondary axis draws its spine: in the width of the spine it
    /// draws (<see cref="PlacedAxis.Line"/>), in the axis' tick colour when it was given one, else the spine's.</summary>
    private void DrawLine(SpineConfig spine)
    {
        var stroke = spine.StrokeIn(Theme);
        bool alongX = _placed.Edge.AlongX;
        double edge = TickDraw.AxisEdge;
        var line = Ctx.OnPixelGrid(new SeriesRenderers.PixelSegment(Point.Along(alongX, PlotArea.StartAlong(alongX), edge),
            Point.Along(alongX, PlotArea.EndAlong(alongX), edge)), stroke.Thickness, projectingEnds: true);
        Ctx.BeginGroup("spine");
        Ctx.DrawLine(line.From, line.To, Axis.MajorTicks.Color is { } own ? stroke with { Color = own } : stroke);
        Ctx.EndGroup();
    }

    /// <summary>Draws one pass over the axis' ticks, <paramref name="ink"/> of them: its major marks and labels on its own edge by its own
    /// <see cref="TickConfig"/> (the labels and their offset text only where the axis labels its own side), what the opposite edge draws of
    /// them (<paramref name="opposite"/>), then its minor marks when the pass draws marks.</summary>
    /// <param name="ink">What this pass draws: the labels, the marks, or both.</param>
    /// <param name="opposite">What the edge opposite the axis draws of its ticks (<see cref="CartesianEdges"/>).</param>
    /// <returns>How far the labels on its own edge reach, and the band of the labels on the opposite edge.</returns>
    internal TicksDrawn RenderTicks(TickInk ink, OppositeTicks opposite)
    {
        var major = Axis.MajorTicksUnder(Theme);
        var own = major.OnOwnSide(ink);
        double reach = Ctx.RenderAxisTicks(Run, major, TickDraw, own);
        if (own.DrawsLabels) Ctx.RenderTickOffset(TickDraw, major, PlotArea, reach);
        double band = RenderOpposite(major, ink, opposite);
        if (ink.DrawsMarks) Ctx.RenderMinorTicks(Run, Axis.MinorTicksUnder(Theme, Run.Scale), TickDraw);
        return new TicksDrawn(reach, band);
    }

    /// <summary>Draws what the opposite edge draws of the axis' ticks in this pass: its major ticks hung from the opposite spine
    /// (<see cref="PlacedAxis.OppositeDraw"/>, made once), the offset text with them when the axis' own side carries no label, and its minor
    /// marks where the opposite edge marks them.</summary>
    /// <returns>The band of the labels drawn there; 0 when none was drawn.</returns>
    private double RenderOpposite(TickConfig major, TickInk ink, OppositeTicks opposite)
    {
        var majors = ink.Within(opposite.Majors);
        bool minors = ink.Within(opposite.Minors).DrawsMarks;
        if (majors == TickInk.None && !minors) return 0;

        var draw = _placed.OppositeDraw(TickDraw, PlotArea, Axes.Spines, Theme);
        double reach = Ctx.RenderAxisTicks(Run, major, draw, majors);
        if (majors.DrawsLabels && !major.LabelsOnOwnSide) Ctx.RenderTickOffset(draw, major, PlotArea, reach);
        if (minors) Ctx.RenderMinorTicks(Run, Axis.MinorTicksUnder(Theme, Run.Scale), draw);
        return majors.DrawsLabels ? major.TickLabelBand(reach) : 0;
    }

    /// <summary>Draws the axis' title beside its edge by the edge's title rule (<see cref="EdgeTerms.DrawTitle"/>), past tick labels
    /// <paramref name="labelReach"/> beyond the mark and the pad.</summary>
    /// <param name="labelReach">How far the tick labels drawn on its edge reach; 0 when none was drawn.</param>
    internal void RenderTitle(double labelReach) =>
        _placed.Edge.DrawTitle(Ctx, Axes, Axis, Theme, PlotArea, TickDraw.AxisEdge, labelReach);

    /// <summary>How the primary X ticks are drawn under the bottom spine, at the rotation they are drawn at: the axis' own, or 30 degrees
    /// when upright labels would run into each other at this plot's width, as matplotlib's <c>Figure.autofmt_xdate</c> turns them. The
    /// labels are measured once, here, where their widths decide the rotation; the drawing and the measuring of the band read the same
    /// extents from the drawing. A tick the plot does not hold draws no label, so it is not measured and does not turn the others; hidden
    /// major ticks draw no label, so none is measured and nothing turns. The drawing (<see cref="PrimaryX"/>) and the layout's measure of
    /// the bands share it, so the band reserved is the band drawn.</summary>
    /// <param name="ctx">The surface the labels are measured on.</param>
    /// <param name="placed">The primary X axis as placed.</param>
    /// <param name="theme">The active theme.</param>
    /// <param name="spines">The axes' spines.</param>
    /// <returns>The tick drawing, turned to fit.</returns>
    internal static TickDrawContext TurnedToFit(IRenderContext ctx, in PlacedAxis placed, Theme theme, SpinesConfig spines)
    {
        var draw = placed.TickDraw(theme, spines);
        if (!placed.Axis.MajorTicksUnder(theme).Visible) return draw;

        var run = placed.Run;
        var ticks = run.Ticks;
        var given = placed.Axis.MajorTicks;
        var font = draw.LabelFont(given);
        var extents = new TextExtent[ticks.Length];
        Span<double> held = ticks.Length <= MaxTicksOnTheStack ? stackalloc double[ticks.Length] : new double[ticks.Length];
        int count = 0;
        double widest = 0;
        for (int i = 0; i < ticks.Length; i++)
        {
            if (run.PlaceOnPlot(ticks[i]) is not { } x) continue;
            extents[i] = ctx.ExtentOf(TextLabel.Of(draw.LabelText(ticks[i])), font);
            widest = Math.Max(widest, extents[i].Width);
            held[count++] = x;
        }
        double rotation = given.LabelRotation != 0 ? given.LabelRotation : Collide(held[..count], widest) ? 30 : 0;
        return placed.Edge.Facing(draw with { LabelRotation = rotation, LabelExtents = extents });
    }

    /// <summary>Whether two neighbouring labels, upright, would run into each other: their ticks stand closer than the widest label and
    /// a gap of 4 pixels.</summary>
    /// <param name="placed">Where each tick the plot holds stands, in pixels, in the order of the ticks.</param>
    /// <param name="widest">The widest of their labels, upright.</param>
    private static bool Collide(ReadOnlySpan<double> placed, double widest)
    {
        const double GapPx = 4;
        for (int i = 1; i < placed.Length; i++)
            if (Math.Abs(placed[i] - placed[i - 1]) < widest + GapPx) return true;
        return false;
    }

    /// <summary>How far the labels of <paramref name="draw"/> stand out past the left and the right edge of the plot area: for each tick
    /// the plot holds (<see cref="TickRun.PlaceOnPlot"/>, the ticks whose labels are drawn), how far its label reaches before and after its
    /// anchor (<see cref="TickDrawContext.Reach"/>) less the room between its anchor and that edge, the largest of them; 0 when every label
    /// lies within the plot's width.</summary>
    /// <param name="run">The X ticks and where each stands.</param>
    /// <param name="draw">How the labels are drawn: their rotation, alignment and the extents already measured.</param>
    /// <param name="plot">The plot area.</param>
    /// <returns>The overhang on each side.</returns>
    internal static SideOverhang OverhangOf(in TickRun run, in TickDrawContext draw, Rect plot)
    {
        double left = 0, right = 0;
        var extents = draw.LabelExtents;
        var ticks = run.Ticks;
        for (int i = 0; i < ticks.Length && extents is not null && i < extents.Count; i++)
        {
            if (run.PlaceOnPlot(ticks[i]) is not { } x) continue;
            var reach = draw.Reach(extents[i]);
            left = Math.Max(left, reach.Before - (x - plot.X));
            right = Math.Max(right, reach.After - (plot.X + plot.Width - x));
        }
        return new SideOverhang(left, right);
    }
}
