// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Interaction;

/// <summary>A client-initiated pan that translates the target axes by a delta in the space each axis is drawn in: data
/// units on a linear axis, decades (log10) on a log axis, symlog units on a symlog axis, with the axis' breaks cut out of
/// that space. A pan is a shift on screen, and only in that space is a shift the same for both ends of the range; on a
/// linear axis without breaks it is the plain data delta it always was. Shape differs from <see cref="AxisRangeEvent"/>
/// (delta vs absolute), so <see cref="PanEvent"/> inherits directly from <see cref="FigureInteractionEvent"/> and supplies its
/// own <see cref="ApplyTo"/>. An axis with no limits and no <see cref="InView"/> is left alone: there is no range to
/// translate.</summary>
public sealed record PanEvent(
    string ChartId,
    int AxesIndex,
    double DxData,
    double DyData) : FigureInteractionEvent(ChartId, AxesIndex)
{
    /// <summary>The range that was on screen when the pointer moved, in data units, or <c>null</c> when the sender did
    /// not say.
    /// <para>An axes that scales itself has no limits to shift, so a pan applied to one used to do nothing until
    /// the reader had zoomed once. The pointer's side knows what is on screen; carrying it here lets the pan pin
    /// that range and move it, whether the event is applied in the control or on a server across SignalR. An
    /// <c>init</c> member so every existing construction site keeps working, and a limit the axes already has
    /// still wins over it.</para></summary>
    public DataRange? InView { get; init; }

    /// <summary>The new range of every Y axis stacked past the primary, already computed by
    /// <see cref="PanModifier"/> from the SAME pixel delta this event shifts the primary by, each in that axis' own data
    /// units, not the primary's (the plot's edges moved by that delta, read back through the axis' own transform).
    /// <see langword="init"/>-only so every existing construction site keeps working; absent/empty leaves every such axis
    /// where it was.</summary>
    public IReadOnlyList<SecondaryAxisRange>? SecondaryRanges { get; init; }

    /// <inheritdoc />
    internal override bool ChangesView => true;

    /// <inheritdoc />
    public override void ApplyTo(Figure figure) => TryApplyTo(figure);

    /// <inheritdoc />
    internal override bool TryApplyTo(Figure figure) => OnMovableAxes(figure, axes =>
    {
        foreach (var linked in XLinkedAxes(figure))
        {
            figure.Home.Remember(linked);
            if ((linked.XAxis.Min ?? InView?.XMin) is double xMin && (linked.XAxis.Max ?? InView?.XMax) is double xMax)
                Shift(linked.XAxis, linked.XBreaks, xMin, xMax, DxData, figure.Theme, isX: true);
        }

        if (!axes.HasFixedYRange && (axes.YAxis.Min ?? InView?.YMin) is double yMin && (axes.YAxis.Max ?? InView?.YMax) is double yMax)
            Shift(axes.YAxis, axes.YBreaks, yMin, yMax, DyData, figure.Theme, isX: false);

        axes.ApplySecondaryRanges(SecondaryRanges);
    });

    /// <summary>Sets <paramref name="axis"/>' limits to <paramref name="min"/>..<paramref name="max"/> moved by
    /// <paramref name="delta"/> in the space the axis is drawn in: through its scale, with its <paramref name="breaks"/> cut out
    /// over the whole axis (<see cref="AxisMapping.OverTheWholeAxis"/>, <see cref="AxisBreakMap.Shifted"/>), the space the
    /// pointer measured its move in. An end the pan carries across a break lands past it.</summary>
    private static void Shift(Axis axis, IReadOnlyList<AxisBreak> breaks, double min, double max, double delta, Styling.Theme theme, bool isX)
    {
        var whole = AxisMapping.Of(axis, breaks, theme, isX).OverTheWholeAxis();
        axis.Min = whole.Shifted(min, delta);
        axis.Max = whole.Shifted(max, delta);
    }
}
