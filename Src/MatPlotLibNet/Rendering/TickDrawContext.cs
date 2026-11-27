// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>Where and how one axis' major ticks are drawn, and what their labels read, how big they are and how far they reach. The
/// <see cref="TickConfig"/> carries what the caller asked for; this carries what the drawing site knows (which edge, where it is, how its
/// labels are formatted), and answers every question about a label: its text, its font, its extent and its reach. The drawing
/// (<see cref="AxisTickExtensions"/>) and the measuring of a band read the labels from here.</summary>
internal readonly struct TickDrawContext
{
    /// <summary>The edge the ticks hang from; decides which way "out" points and where the labels go.</summary>
    public TickSide Side { get; init; }

    /// <summary>What that edge means to the drawing (<see cref="EdgeTerms"/>): which way it runs, which way is out, where a label
    /// stands.</summary>
    internal EdgeTerms Edge => EdgeTerms.Of(Side);

    /// <summary>The pixel coordinate of that edge: a Y for <see cref="TickSide.Bottom"/>/<see cref="TickSide.Top"/>,
    /// an X for <see cref="TickSide.Left"/>/<see cref="TickSide.Right"/>.</summary>
    public double AxisEdge { get; init; }

    /// <summary>Half the spine stroke on that edge. The end of a mark that meets the spine reaches this far past the
    /// edge, so the mark touches the stroke whatever the subpixel rounding (matplotlib draws ticks through the spine).</summary>
    public double SpineHalf { get; init; }

    /// <summary>The theme's tick font, before <see cref="TickConfig.LabelSize"/> and <see cref="TickConfig.LabelColor"/>.</summary>
    public Font BaseFont { get; init; }

    /// <summary>The axis' own formatter; when <see langword="null"/> <see cref="UniformFormat"/> formats the label.</summary>
    public ITickFormatter? Formatter { get; init; }

    /// <summary>The formatter shared by all ticks of the axis, used when <see cref="Formatter"/> is null; it writes a tick less
    /// <see cref="Offset"/>.</summary>
    public Func<double, string> UniformFormat { get; init; }

    /// <summary>The offset taken off every label written in <see cref="UniformFormat"/> and written once at the end of the axis
    /// (matplotlib's <c>ScalarFormatter</c> offset, <see cref="TickLabelExtensions"/>' <c>OffsetIn</c>); 0 for none.</summary>
    public double Offset { get; init; }

    /// <summary>The power of ten every label written in <see cref="UniformFormat"/> was divided by and that is written once at the end
    /// of the axis (matplotlib's <c>ScalarFormatter</c> scientific notation, <c>TickLabelExtensions.OrderOfMagnitudeIn</c>); 0 for none.</summary>
    public int OrderOfMagnitude { get; init; }

    /// <summary>The colour a mark takes when <see cref="TickConfig.Color"/> is not set: the theme's foreground.</summary>
    public Color Foreground { get; init; }

    /// <summary>The theme the ticks are drawn under: how a label written in <see cref="UniformFormat"/>, and the offset text, sign a
    /// negative number (its <see cref="TickLabelLook.Signed"/>, the true minus sign under the matplotlib themes; a label of the axis'
    /// own <see cref="Formatter"/> is its own), how the minor step is split, and whether a mark stands on the pixel grid of a surface
    /// that has one (<see cref="AxisTickExtensions"/>' <c>DrawTickMark</c>). A drawing with no theme of its own is given
    /// <see cref="Styling.Theme.Default"/>.</summary>
    public required Theme Theme { get; init; }

    /// <summary>The rotation the labels are drawn at, in degrees (the caller may have resolved an automatic one).</summary>
    public double LabelRotation { get; init; }

    /// <summary>How a label is anchored at its point.</summary>
    public TextAlignment Alignment { get; init; }

    /// <summary>The upright extent of each tick's label in the font it is drawn in (<see cref="TextExtent"/>: its width, height and
    /// ascent, as the one measuring seam gave them), in the order of the ticks this context is used with; <see langword="null"/> to
    /// measure each label while drawing or measuring. A site that measured the labels to decide how to draw them (the X labels'
    /// rotation) hands the extents on, so they are measured once and read back as measured. Read only when it holds one per tick.</summary>
    public IReadOnlyList<TextExtent>? LabelExtents { get; init; }

    /// <summary>The minor ticks of the axis when the scale its ticks are drawn on places them (a log or a logit axis',
    /// <see cref="TickLocators.IMinorTickLocator"/>), worked out only when its minor ticks are drawn; <see langword="null"/> when
    /// they split the major step (<see cref="AxisTickLook.MinorValuesBetween"/>).</summary>
    public double[]? ScaleMinors { get; init; }

    /// <summary>For each tick of the ticks this context is used with, whether it is one of its scale's minor ticks with a label
    /// (<see cref="AxesTickSourceExtensions"/>' <c>MinorNamedAmong</c>: a log axis' named multiple, a logit axis' named minor, under the
    /// matplotlib themes), drawn in <see cref="MinorLook"/>; <see langword="null"/> when every tick is a major one.</summary>
    public bool[]? MinorNamed { get; init; }

    /// <summary>How a labelled minor tick (<see cref="MinorNamed"/>) is drawn: the axis' minor ticks as its theme draws them, their mark
    /// and pad (matplotlib's minor tick of a log axis: 2.0 points long, its label 3.4 points past it under the default style, where a
    /// major's is 3.5 and 3.5); <see langword="null"/> when there is none.</summary>
    public TickConfig? MinorLook { get; init; }

    /// <summary>How tick <paramref name="i"/> is drawn: in <see cref="MinorLook"/> when it is a labelled minor tick, in
    /// <paramref name="major"/> otherwise.</summary>
    /// <param name="i">The index of the tick.</param>
    /// <param name="major">The axis' major ticks as drawn.</param>
    /// <returns>The tick look.</returns>
    internal TickConfig LookOf(int i, TickConfig major) => IsMinorNamed(i) ? MinorLook! : major;

    /// <summary>Whether tick <paramref name="i"/> is a labelled minor tick (<see cref="MinorNamed"/>), drawn in <see cref="MinorLook"/>.</summary>
    /// <param name="i">The index of the tick.</param>
    /// <returns><see langword="true"/> for a labelled minor tick.</returns>
    internal bool IsMinorNamed(int i) => MinorNamed is { } minor && minor[i];

    /// <summary>The font a tick label of <paramref name="config"/> is drawn in: <see cref="BaseFont"/> at the config's
    /// <see cref="TickConfig.LabelSize"/> and in its <see cref="TickConfig.LabelColor"/> where set, by the one rule of the font roles
    /// (<see cref="ThemeFontExtensions"/> <c>LabelFontOver</c>), which the locator's size of a label reads too.</summary>
    /// <param name="config">The axis' tick settings as drawn.</param>
    /// <returns>The label font.</returns>
    internal Font LabelFont(TickConfig config) => config.LabelFontOver(BaseFont);

    /// <summary>The text of the label of <paramref name="tick"/>: the axis' formatter, or the uniform format.</summary>
    /// <param name="tick">The tick's value.</param>
    /// <returns>The label.</returns>
    internal string LabelText(double tick) =>
        Formatter?.Format(tick) ?? Theme.Looks.TickLabels.Signed(UniformFormat(OrderOfMagnitude == 0
            ? tick - Offset
            : (tick - Offset) / Math.Pow(10, OrderOfMagnitude)));

    /// <summary>The upright extent of the label of tick <paramref name="index"/> of <paramref name="ticks"/> in
    /// <paramref name="font"/>: the extent the context carries (<see cref="LabelExtents"/>) when it holds one per tick, otherwise
    /// measured on <paramref name="ctx"/>. The caller that already holds the label's text hands it in (<paramref name="text"/>), so a label
    /// is formatted once, not once to be drawn and once to be measured.</summary>
    /// <param name="ctx">The surface a label is measured on.</param>
    /// <param name="ticks">The ticks this context is used with.</param>
    /// <param name="index">The index of the tick.</param>
    /// <param name="font">The label font.</param>
    /// <param name="text">The label's text, when the caller already holds it.</param>
    /// <returns>The extent.</returns>
    internal TextExtent LabelSize(IRenderContext ctx, double[] ticks, int index, Font font, string? text = null) =>
        LabelExtents is { } extents && extents.Count == ticks.Length
            ? extents[index]
            : ctx.ExtentOf(TextLabel.Of(text ?? LabelText(ticks[index])), font);

    /// <summary>How far one tick label reaches, as it is drawn, by the one rule for all four edges and every rotation: the
    /// label's box (<see cref="TextBox"/>, the ascent above the baseline, the descent below it, in
    /// the label's alignment) turned about its anchor by the label's rotation. <see cref="LabelReach.Away"/> is how far it
    /// stands from the edge it hangs from, past the mark and the pad; <see cref="LabelReach.Before"/> and
    /// <see cref="LabelReach.After"/> how far it stands before and after its anchor along the edge. Upright, a label on a
    /// top or bottom edge reaches exactly its height and one on a left or right edge exactly its width, the band every
    /// upright label is reserved, so a figure whose labels are not turned is laid out to the bit as before.</summary>
    /// <param name="size">The label's measured extent, upright.</param>
    /// <returns>The label's reach, in pixels.</returns>
    internal LabelReach Reach(TextExtent size)
    {
        var box = TurnedBox(size, LabelRotation);
        var edge = Edge;
        double before = Math.Max(0, -box.StartAlong(edge.AlongX));
        double after = Math.Max(0, box.EndAlong(edge.AlongX));
        if (LabelRotation == 0)
            return new LabelReach(size.ExtentAcross(edge.AlongX), before, after);

        return new LabelReach(edge.TurnedAway(box, TurnedBox(size, 0), size), before, after);
    }

    /// <summary>The box of a label of <paramref name="size"/> turned by <paramref name="degrees"/> about its anchor, at the
    /// anchor (the origin), in the label's alignment: the one box every reach is read off.</summary>
    private Rect TurnedBox(TextExtent size, double degrees) =>
        new TextBox(new TextPlacement(new Point(0, 0), Alignment) { Rotation = degrees }, size).Bounds();

    /// <summary>How far one tick label reaches past the mark and the pad, away from the edge it hangs from, as it is drawn
    /// (<see cref="Reach"/>): the tallest label's height or the widest label's width upright, and what the turn carries it
    /// to when it is turned. The drawing (<see cref="AxisTickExtensions"/>' <c>RenderAxisTicks</c>) and the measuring
    /// (<c>MeasureAxisTicks</c>) take it from here, so the band the layout reserves for turned labels and the room the axis title
    /// clears are the drawn ones.</summary>
    /// <param name="size">The label's measured extent, upright.</param>
    /// <returns>The label's extent away from the edge, in pixels.</returns>
    internal double LabelExtent(TextExtent size) => Reach(size).Away;

    /// <summary>How much nearer the edge than a major tick's label the label of tick <paramref name="i"/> stands: the major mark's
    /// part outside the axes and pad (<paramref name="majorReach"/>) less those of the tick's look (<see cref="LookOf"/>);
    /// 0 for a major tick. The drawing places the label by it and both the drawing and the measuring take it off the label's extent,
    /// so the band the layout reserves past the major mark and pad (<see cref="TickConfig.TickLabelBand"/>) reaches the far side of the
    /// label that reaches farthest.</summary>
    /// <param name="i">The index of the tick.</param>
    /// <param name="major">The axis' major ticks as drawn.</param>
    /// <param name="majorReach">The major mark's part outside the axes plus the major pad.</param>
    /// <returns>The distance in pixels; 0 for a major tick.</returns>
    internal double Nearer(int i, TickConfig major, double majorReach)
    {
        var look = LookOf(i, major);
        return ReferenceEquals(look, major) ? 0 : majorReach - (look.OutsideLength + look.Pad);
    }

    /// <summary>The context every axis starts from, the fields that are set alike on every edge: the side, the theme's tick
    /// font and foreground, the formatter of the ticks' source, told the ticks and the view when its labels depend on them
    /// (<see cref="ITickSetFormatter"/>), and the one number format of the ticks (<see cref="TickLabelExtensions"/>'
    /// <c>UniformFormat</c>). Where the edge stands, half the spine, the rotation and the alignment are the drawing site's to
    /// add.</summary>
    /// <param name="edge">The edge the ticks hang from.</param>
    /// <param name="source">Where the ticks come from: its formatter writes the labels.</param>
    /// <param name="ticks">The tick values: their step sets the number format.</param>
    /// <param name="theme">The active theme (the tick font and the foreground).</param>
    /// <param name="view">The ends of the view the axis shows, which a formatter of matplotlib's log scale labels by; <see langword="null"/>
    /// when none is known.</param>
    /// <param name="linThresh">The linear threshold of the symmetric-log scale the ticks are drawn on; <see langword="null"/> for any
    /// other.</param>
    /// <returns>The context.</returns>
    internal static TickDrawContext For(EdgeTerms edge, TickSource source, double[] ticks, Theme theme, Range1D? view = null,
        double? linThresh = null) => new()
    {
        Side          = edge.Side,
        BaseFont      = theme.TickFont(),
        Formatter     = source.Formatter is ITickSetFormatter set ? set.ForTicks(ticks, view, linThresh, theme) : source.Formatter,
        UniformFormat = ticks.UniformFormat(),
        Foreground    = theme.ForegroundText,
        Theme         = theme,
    };

    /// <summary>The context of <see cref="For(EdgeTerms, TickSource, double[], Theme, Range1D?, double?)"/> for the ticks of <paramref name="run"/>
    /// on its edge (<see cref="TickRun.Edge"/>) of <paramref name="axis"/>, with the offset its theme takes off the labels and the power
    /// of ten it divides them by (matplotlib's <c>ScalarFormatter</c>, under a theme that names an <see cref="TickLabelLook.OffsetThreshold"/>
    /// and <see cref="TickLabelLook.FormatterLimits"/>): only for plain numbers on a linear scale of the run (<see cref="TickRun.Scale"/>),
    /// the labels the default formatter writes, over the ticks in the run's own view (<see cref="TickRun.View"/>), in the unit of its
    /// ticks: a linked axis' in its own unit, as matplotlib's secondary axis formats its ticks on a <c>FuncScale</c> of its own. Such labels are written in the uniform format of the ticks less the
    /// offset and divided by the power. When the axis draws its minor ticks and the scale its ticks are drawn on places them
    /// (<see cref="TickLocators.IMinorTickLocator"/>), the context carries them (<see cref="ScaleMinors"/>), and the ticks that are
    /// labelled minor ones with the look they are drawn in (<see cref="MinorNamed"/>, <see cref="MinorLook"/>).</summary>
    /// <param name="source">Where the ticks come from: its formatter writes the labels.</param>
    /// <param name="run">The tick values, where they stand and the edge they lie on.</param>
    /// <param name="theme">The active theme.</param>
    /// <param name="axis">The axis the ticks stand on: its scale.</param>
    /// <param name="transform">The transform the axis is spread over: the ends of its view and the length of the plot.</param>
    /// <returns>The context.</returns>
    internal static TickDrawContext For(TickSource source, TickRun run, Theme theme, Axis axis, DataTransform transform)
    {
        bool alongX = run.Edge.AlongX;
        var ticks = run.Ticks;
        var shown = run.View;
        var scale = run.Scale;
        var context = For(run.Edge, source, ticks, theme, shown, scale.LinearThreshold);
        if (source.MinorNamedAmong(ticks, shown, theme) is { } minorNamed)
            context = context with { MinorNamed = minorNamed, MinorLook = axis.MinorTicksDrawnUnder(theme) };
        if (axis.MinorTicks.Spacing is not > 0
            && theme.Looks.Ticks.PlacesMinors(axis.MinorTicks, scale)
            && run.ScaleMinors(axis.TickSpaceAlong(transform.PlotBounds.LengthAlong(alongX), theme, alongX), theme) is { } minors)
            context = context with { ScaleMinors = minors };
        if (source.Formatter is not null || source.NamesCategories || !scale.OffsetsItsLabels || shown is not { } view) return context;

        double offset = theme.Looks.TickLabels.OffsetThreshold is { } threshold ? ticks.OffsetIn(view, threshold) : 0;
        int order = theme.Looks.TickLabels.FormatterLimits is { } limits ? ticks.OrderOfMagnitudeIn(view, offset, limits) : 0;
        if (offset == 0 && order == 0) return context;

        double divisor = Math.Pow(10, order);
        double[] shifted = new double[ticks.Length];
        for (int i = 0; i < ticks.Length; i++) shifted[i] = (ticks[i] - offset) / divisor;
        return context with { UniformFormat = shifted.UniformFormat(), Offset = offset, OrderOfMagnitude = order };
    }
}
