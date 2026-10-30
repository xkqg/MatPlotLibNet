// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>One Y axis slot of the container as ONE render draws it: the slot, the range and the transform its series and
/// ticks are drawn with, the ticks it places, where its series stand in the identity order, its band and how far out past
/// the plot's edge it stands. The renderer places every slot once per render (<c>CartesianAxesRenderer</c>), and the part
/// that draws a slot (<c>CartesianYAxisSlotPart</c>), the end labels (<c>CartesianDirectLabelsPart</c>) and the
/// interaction views all read the same value, so none of them works the range or the transform out again.</summary>
/// <remarks>The primary Y axis is left slot 0: its range is the axes' own (autoscale, margins, limits), its ticks those
/// its grid is drawn through, and its band is known once its tick labels are drawn.</remarks>
internal readonly record struct PlacedYSlot
{
    /// <summary>The slot: its axis and its series.</summary>
    internal required YAxisSlot Slot { get; init; }

    /// <summary>The range the slot is drawn over: the primary's X range and the slot's own Y range.</summary>
    internal required DataRange Range { get; init; }

    /// <summary>The data-to-pixel transform the slot's series and ticks are drawn with, through the slot's own scale.</summary>
    internal required DataTransform Transform { get; init; }

    /// <summary>The major tick values the slot draws.</summary>
    internal required double[] Ticks { get; init; }

    /// <summary>Where those ticks come from and how they read.</summary>
    internal required TickSource Source { get; init; }

    /// <summary>The series list that numbers the slot's series (<see cref="SeriesSlotExtensions.SlotIndex"/>).</summary>
    internal SeriesAxisPair Pair { get; init; }

    /// <summary>The index the slot's first series has in <see cref="Pair"/>'s list.</summary>
    internal int FirstIndexInPair { get; init; }

    /// <summary>From the slot's edge to the far side of what it draws (<see cref="YAxisTitleExtensions"/>).</summary>
    internal double Band { get; init; }

    /// <summary>How far out past the plot's edge on its side the slot's edge stands: 0 for slot 0, the bands before it
    /// and the gaps between them otherwise.</summary>
    internal double Offset { get; init; }

    /// <summary>What differs between the sides, for this slot's side.</summary>
    internal YSideTerms Terms => YSideTerms.Of(Slot.Side);

    /// <summary>The x of this slot's edge beside <paramref name="plot"/>.</summary>
    /// <param name="plot">The plot area.</param>
    /// <returns>The edge, in pixels.</returns>
    internal double EdgeX(Rect plot) => Terms.EdgeX(plot, Offset);

    /// <summary>The x this slot's tick marks and labels hang from. The slot standing at the plot's own edge (offset 0) hangs
    /// from the spine on its side wherever that has been moved to (a data value, a fraction of the axes); a slot stacked out
    /// past it keeps to its place beside the plot.</summary>
    /// <param name="plot">The plot area.</param>
    /// <param name="spines">The axes' spines.</param>
    /// <returns>The x, in pixels.</returns>
    internal double TickEdgeX(Rect plot, SpinesConfig spines) =>
        Offset == 0 ? Terms.Spine(spines).VerticalX(plot, EdgeX(plot), Transform) : EdgeX(plot);

    /// <summary>How this slot's tick labels are drawn and measured, before they are hung from an edge: by its tick source's
    /// formatter (or the uniform format of its ticks), in the theme's tick font, at its label rotation, anchored away from
    /// the plot. The drawing and the layout's reserve both read it.</summary>
    /// <param name="theme">The active theme (the tick font and the foreground).</param>
    /// <returns>The tick drawing context, facing its side, without its edge.</returns>
    internal TickDrawContext TickDraw(Theme theme) => new()
    {
        Side          = Terms.TickSide,
        BaseFont      = ThemedFontProvider.TickFont(theme),
        Formatter     = Source.Formatter,
        UniformFormat = AxesRenderer.BuildUniformTickFormatter(Ticks),
        Foreground    = theme.ForegroundText,
        LabelRotation = Slot.Axis.MajorTicks.LabelRotation,
        Alignment     = Terms.LabelAlignment,
    };
}
