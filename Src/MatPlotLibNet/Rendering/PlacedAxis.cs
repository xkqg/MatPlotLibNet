// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>One axis of a Cartesian axes as ONE render places it (<see cref="CartesianAxisPlacement"/>), on any edge: the primary X and
/// Y axes, a Y axis stacked past the primary on either side, the top X axis and a linked X axis below the plot. Its axis and the edge it
/// stands on, the range and the transform its series and ticks are drawn with, where its ticks stand, its tick values and where they come
/// from, its own series and where they stand in the identity order, the line its ticks hang from, its band and how far out past the plot
/// it stands, and why a linked one draws no ticks. The part that draws it (<c>CartesianAxisPart</c>), the layout, the end labels, the
/// marks a legend keeps clear of and the interaction views all read this one value, so none of them works its range, its transform, its
/// ticks or its tick drawing out a second way.</summary>
/// <remarks>A linked axis (<see cref="Models.Axis.Link"/>) has no range of its own: it takes the primary's range and transform, and its
/// ticks stand through <see cref="Placement"/>, where the primary shows the value each maps back to.</remarks>
internal readonly record struct PlacedAxis
{
    /// <summary>The axis: its ticks, its title, its scale.</summary>
    internal required Axis Axis { get; init; }

    /// <summary>The edge the axis hangs its ticks from (<see cref="EdgeTerms"/>), which says which way its labels face and where its
    /// title stands.</summary>
    internal required EdgeTerms Edge { get; init; }

    /// <summary>The range the axis is drawn over; for a linked axis the primary's range whole.</summary>
    internal required DataRange Range { get; init; }

    /// <summary>The data-to-pixel transform the axis' series and ticks are drawn with; for a linked axis the primary's.</summary>
    internal required DataTransform Transform { get; init; }

    /// <summary>Where the ticks stand: <see cref="Transform"/>, a tick at its own value, unless set; for a linked axis a
    /// <see cref="LinkedTickPlacement"/>, a tick of value v where the primary shows the value v maps back to.</summary>
    internal ITickPlacement Placement
    {
        get => _placement ?? Transform;
        init => _placement = value;
    }
    private readonly ITickPlacement? _placement;

    /// <summary>The major tick values the axis draws, in its own unit.</summary>
    internal required double[] Ticks { get; init; }

    /// <summary>Where the ticks come from and how their labels are written.</summary>
    internal required TickSource Source { get; init; }

    /// <summary>What the drawing of the axis reports through <see cref="ChartDiagnostics"/>: why a linked axis draws no ticks, when its
    /// link cannot place the primary's range; <see langword="null"/> otherwise. Only the drawing reports it, so a render says it once,
    /// however often the layout, the legend and the views place the axis.</summary>
    internal ChartDiagnostic? Diagnostic { get; init; }

    /// <summary>The series the axis draws as its own: a Y axis stacked past the primary's, the top X axis'; none for the primary
    /// axes, whose series the data layer draws, and for a linked axis below the plot, which holds none.</summary>
    internal IReadOnlyList<ISeries> Series { get => field ?? []; init; }

    /// <summary>The series list that numbers <see cref="Series"/> (<see cref="SeriesSlotExtensions.SlotIndex"/>).</summary>
    internal SeriesAxisPair Pair { get; init; }

    /// <summary>The index the first of <see cref="Series"/> has in <see cref="Pair"/>'s list.</summary>
    internal int FirstIndexInPair { get; init; }

    /// <summary>From the axis' edge to the far side of what it draws past it: a Y slot's band (<see cref="YAxisTitleExtensions"/>), a
    /// linked X axis' below the plot.</summary>
    internal double Band { get; init; }

    /// <summary>How far out past the plot's edge on its side the axis' edge stands: 0 at the plot's edge, the bands before it and the
    /// gaps between them for a Y axis stacked past it, the walk down the stack for a linked axis below the plot.</summary>
    internal double Offset { get; init; }

    /// <summary>The coordinate across the edge (a y on a top or a bottom edge, an x on a left or a right one) the axis hangs its ticks
    /// and its title from: the spine on its side for a primary axis, wherever the spine was moved to; for an axis past the primary, a
    /// twin with a frame of its own (a Y axis stacked past it, the top X axis, a linked X axis below the plot), the place it stands at past
    /// the plot's edge, the edge itself at offset 0, as matplotlib's twin axes do. Set where the axis is placed.</summary>
    internal double EdgeLine { get; init; }

    /// <summary>The spine an axis draws as a line of its own at <see cref="EdgeLine"/>, as matplotlib's secondary axis draws its spine:
    /// a linked X axis below the plot draws the bottom spine's; <see langword="null"/> for an axis whose edge is the axes' own spine.</summary>
    internal SpineConfig? Line { get; init; }

    /// <summary>The axis' ticks as they stand on its edge, each where <see cref="Placement"/> stands it. Its major marks and labels, its
    /// minor marks and the layout's measure of its band all read the plot's hold on a tick from it.</summary>
    internal TickRun Run => new(Ticks, Placement, Edge);

    /// <summary>Whether the axis, past the primary, is drawn: it has series of its own, or it is linked (<see cref="Axis.IsDrawnWith"/>).</summary>
    internal bool IsDrawn => Axis.IsDrawnWith(Series);

    /// <summary>How the axis' major ticks are drawn and measured, hung from <see cref="EdgeLine"/>: by its tick source's formatter (or the
    /// uniform format of its ticks), in the theme's tick font, at its own label rotation, facing its edge and half the spine on its side
    /// in. The drawing and the layout's reserve both read it, so the band reserved is the band drawn.</summary>
    /// <param name="theme">The active theme.</param>
    /// <param name="spines">The axes' spines.</param>
    /// <returns>The tick drawing.</returns>
    internal TickDrawContext TickDraw(Theme theme, SpinesConfig spines) => Edge.Hang(
        TickDrawContext.For(Source, Run, theme, Axis, Transform) with { LabelRotation = Axis.MajorTicks.LabelRotation },
        EdgeLine, spines, theme);

    /// <summary>How far the tick labels the axis draws on its own side reach past the mark and the pad, measured with its tick drawing
    /// (<see cref="TickDraw"/>) before anything is drawn; 0 when its labels stand off its own side (<see cref="TickConfig.LabelsOnOwnSide"/>),
    /// where it draws its marks alone.</summary>
    /// <param name="ctx">The context the labels are measured with.</param>
    /// <param name="theme">The active theme.</param>
    /// <param name="spines">The axes' spines.</param>
    /// <returns>The reach, in pixels.</returns>
    internal double OwnSideLabelReach(IRenderContext ctx, Theme theme, SpinesConfig spines)
    {
        var major = Axis.MajorTicksUnder(theme);
        return major.LabelsOnOwnSide ? ctx.MeasureAxisTicks(Run, major, TickDraw(theme, spines)) : 0;
    }

    /// <summary><paramref name="draw"/>, the axis' tick drawing, hung from the spine of the edge opposite the axis' own: the same ticks,
    /// labels and rotation, its labels facing that edge (the mirrored X ticks above the plot, the mirrored Y ticks right of it).</summary>
    /// <param name="draw">How the axis' ticks are drawn on its own edge.</param>
    /// <param name="plot">The plot area.</param>
    /// <param name="spines">The axes' spines.</param>
    /// <param name="theme">The active theme.</param>
    /// <returns>The tick drawing on the opposite edge.</returns>
    internal TickDrawContext OppositeDraw(in TickDrawContext draw, Rect plot, SpinesConfig spines, Theme theme)
    {
        var opposite = Edge.Opposite;
        return opposite.Hang(draw, opposite.SpineAt(spines, plot, Transform), spines, theme);
    }
}
