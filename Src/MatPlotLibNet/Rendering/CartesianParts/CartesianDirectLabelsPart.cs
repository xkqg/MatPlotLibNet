// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.CartesianParts;

/// <summary>One end label, placed.</summary>
/// <param name="Label">What the label says, which series it names and the data point its line ends at.</param>
/// <param name="Anchor">Where that line ends, in pixels.</param>
/// <param name="Position">Where the text is drawn: the left end of its baseline. Every label of an axes shares the x;
/// the y is the anchor's, moved only as far as needed to keep one label height between neighbours.</param>
/// <param name="Font">The end-label font in the colour of the series.</param>
internal readonly record struct PlacedDirectLabel(DirectLabel Label, Point Anchor, Point Position, Font Font);

/// <summary>
/// Names lines at their end (<see cref="Models.Series.IHasLabelPlacement"/> with
/// <see cref="Models.Series.SeriesLabelPlacement.LineEnd"/>): a column of labels right of the plot area, each in the
/// colour of its line, at the height of the line's last drawable point, pushed apart so no two overlap.
/// </summary>
/// <remarks>
/// <para>The labels of every series of the axes — on every Y axis of the container, either side — are placed ONCE, in
/// the constructor, so they can be kept apart across series; each stands at the height its own slot's transform puts the
/// end of its line at (<see cref="PlacedYSlot"/>). What a label says and which point is the end of its line come from
/// <see cref="DirectLabelMetrics"/>, the owner the layout engine reserves the right edge with; the column starts past
/// the axis side of the edge (<see cref="RightBand.Axis"/>: the right axes' bands stacked together or the mirrored Y tick
/// labels, the rule that side is reserved with) plus <see cref="DirectLabelMetrics.Gap"/>; the colour is the series' slot
/// colour (<see cref="SeriesSlotExtensions"/>), the one its line and legend swatch wear.</para>
/// <para>Every label is drawn inside its own series' group, while the pass that draws that series has it open
/// (<see cref="DrawLabelOf"/>): the primary series pass for a primary line, the part of its slot for any other. So
/// whatever shows, hides or highlights the series — the legend toggle, the hover highlight, a diff by
/// <c>data-series-index</c> — takes the label along, on any axis. The part therefore draws nothing on its own: it is
/// not a step of the renderer's part sequence but the collaborator every series pass calls.</para>
/// <para>The part keeps its state to itself: subplots render in parallel and each renderer builds its own part.</para>
/// </remarks>
internal sealed class CartesianDirectLabelsPart
{
    /// <summary>Places the end labels of the axes of <paramref name="context"/>.</summary>
    /// <param name="context">The axes whose series are labelled, the plot area, the context labels are measured with and
    /// drawn into, and the active theme (font, cycle colours).</param>
    /// <param name="slots">Every Y axis slot of the axes as this render places it, the primary first
    /// (<see cref="PlacedYSlot"/>): a label stands at the height its own slot's transform puts the end of its line at, the
    /// X range they share is the range the axes is drawn with (a point outside it is not the end of a line), and the column
    /// starts past the right slots' bands stacked together.</param>
    /// <param name="mirroredYTicks">The band the mirrored Y tick labels took past the right spine
    /// (<see cref="EdgeBands.Right"/>); 0 when none were drawn. The column starts past it.</param>
    internal CartesianDirectLabelsPart(CartesianPartContext context, IReadOnlyList<PlacedYSlot> slots, double mirroredYTicks)
    {
        _axes = context.Axes;
        _plotArea = context.PlotArea;
        _ctx = context.Ctx;
        _theme = context.Theme;
        _mirroredYTicks = mirroredYTicks;
        Placements = Place(slots);
    }

    private readonly Axes _axes;
    private readonly Rect _plotArea;
    private readonly IRenderContext _ctx;
    private readonly Theme _theme;
    private readonly double _mirroredYTicks;

    /// <summary>The placed labels in identity order: the primary's series first, then every right slot's, then every left
    /// slot's, each list in series order.</summary>
    internal IReadOnlyList<PlacedDirectLabel> Placements { get; }

    /// <summary>Draws the end label of the series whose identity slot is <paramref name="seriesIndex"/>
    /// (<see cref="SeriesSlotExtensions.SlotIndex"/>, its place in <see cref="Axes.AllSeries"/>), when it has one. Called
    /// inside that series' group, by whichever pass draws the series.</summary>
    /// <param name="seriesIndex">The series' identity slot.</param>
    internal void DrawLabelOf(int seriesIndex)
    {
        foreach (var placed in Placements)
            if (_axes.SlotIndex(placed.Label.Pair, placed.Label.IndexInPair) == seriesIndex) Draw(placed);
    }

    private void Draw(PlacedDirectLabel placed)
    {
        string text = placed.Label.Text;
        if (MathTextParser.ContainsMath(text))
            _ctx.DrawRichText(MathTextParser.Parse(text), placed.Position, placed.Font, TextAlignment.Left);
        else
            _ctx.DrawText(text, placed.Position, placed.Font, TextAlignment.Left);
    }

    private List<PlacedDirectLabel> Place(IReadOnlyList<PlacedYSlot> slots)
    {
        var shared = slots[0].Range;
        var metrics = new DirectLabelMetrics(_axes, _theme, _ctx, new MinMaxRange(shared.XMin, shared.XMax));
        var labels = new List<DirectLabel>();
        var anchors = new List<Point>();
        double labelHeight = 0;
        foreach (var label in metrics.Labels)
        {
            var transform = slots.First(placed => ReferenceEquals(placed.Slot, label.Slot)).Transform;
            var anchor = transform.DataToPixel(label.EndX, label.EndY);
            // The axis cannot draw this end (a non-positive value on a log axis, a value inside a break): the line has
            // no visible end there, so the label has nowhere to stand.
            if (!anchor.Y.IsDrawable()) continue;
            labels.Add(label);
            anchors.Add(anchor);
            labelHeight = Math.Max(labelHeight, metrics.Measure(label.Text).Height);
        }

        // Each label is a point at its anchor's height, kept one label height from its neighbours, in the order the
        // anchors run, inside the plot area's height; when the plot is too short for all of them the stack keeps its
        // spacing and climbs past the top.
        var centres = anchors.Select(a => new Interval1D(a.Y, 0)).ToList();
        centres.SpreadApart(labelHeight, _plotArea.Y, _plotArea.Y + _plotArea.Height);

        // Past the axis side of the right edge, by the rule the layout reserves it with (RightBand.Axis): the bands this
        // render placed the right slots by, stacked together as the layout stacks them.
        double[] rightBands = [.. slots.Where(placed => placed.Slot.Side == AxisSide.Right).Select(placed => placed.Band)];
        double axisSide = new RightBand(rightBands.TotalStackedBand(_axes.YAxes.TitleGap), _mirroredYTicks, 0).Axis;
        double x = _plotArea.X + _plotArea.Width + axisSide + DirectLabelMetrics.Gap;
        var placed = new List<PlacedDirectLabel>(labels.Count);
        var colorSlots = _axes.ColorSlots();
        for (int k = 0; k < labels.Count; k++)
        {
            var label = labels[k];
            var font = metrics.Font with
            {
                Color = _theme.SeriesSlot(colorSlots[_axes.SlotIndex(label.Pair, label.IndexInPair)]).ColorOf(label.Series),
            };
            placed.Add(new PlacedDirectLabel(label, anchors[k],
                new Point(x, centres[k].Start + font.Size * TextBox.CentreDrop), font));
        }
        return placed;
    }
}
