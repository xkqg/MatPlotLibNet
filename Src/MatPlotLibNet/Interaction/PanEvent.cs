// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Interaction;

/// <summary>A client-initiated pan that translates the target axes by a delta in the space each axis is drawn in: data
/// units on a linear axis, decades (log10) on a log axis, symlog units on a symlog axis. A pan is a shift on screen, and
/// only in that space is a shift the same for both ends of the range; on a linear axis it is the plain data delta it
/// always was. Shape differs from <see cref="AxisRangeEvent"/> (delta vs absolute), so <see cref="PanEvent"/>
/// inherits directly from <see cref="FigureInteractionEvent"/> and supplies its own <see cref="ApplyTo"/>. An axis with
/// no limits and no <see cref="InView"/> is left alone: there is no range to translate.</summary>
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
    /// <see cref="PanModifier"/> from the SAME pixel delta this event shifts the primary by — each in
    /// that axis' own data units, not the primary's (<see cref="Shifted"/>, read against
    /// <see cref="IChartLayout.GetSecondaryDataRanges"/>'s current value for that slot). <see langword="init"/>-only
    /// so every existing construction site keeps working; absent/empty leaves every such axis where it was.</summary>
    public IReadOnlyList<SecondaryAxisRange>? SecondaryRanges { get; init; }

    /// <inheritdoc />
    internal override bool ChangesView => true;

    /// <inheritdoc />
    public override void ApplyTo(Figure figure) => TryApplyTo(figure);

    /// <inheritdoc />
    internal override bool TryApplyTo(Figure figure)
    {
        if (!TryTargetMovableAxes(figure, out var axes)) return false;

        foreach (var linked in XLinkedAxes(figure))
        {
            figure.Home.Remember(linked);
            if ((linked.XAxis.Min ?? InView?.XMin) is double xMin && (linked.XAxis.Max ?? InView?.XMax) is double xMax)
                Shift(linked.XAxis, xMin, xMax, DxData);
        }

        if (!axes.HasFixedYRange && (axes.YAxis.Min ?? InView?.YMin) is double yMin && (axes.YAxis.Max ?? InView?.YMax) is double yMax)
            Shift(axes.YAxis, yMin, yMax, DyData);

        axes.ApplySecondaryRanges(SecondaryRanges);
        return true;
    }

    /// <summary>Sets <paramref name="axis"/>' limits to <paramref name="min"/>..<paramref name="max"/> moved by
    /// <paramref name="delta"/> in the space the axis is drawn in (<see cref="AxisScaleExtensions"/>).</summary>
    private static void Shift(Axis axis, double min, double max, double delta)
    {
        var moved = Shifted(new DataSpan(min, max), delta, axis.Scale, axis.SymLogLinThresh);
        axis.Min = moved.Min;
        axis.Max = moved.Max;
    }

    /// <summary>The <paramref name="span"/> moved by <paramref name="delta"/> in the space <paramref name="scale"/>
    /// draws in — the computation <see cref="Shift"/> applies to the primary axis, exposed so
    /// <see cref="PanModifier"/> can apply the SAME rule to a secondary axis' own scale without going through an
    /// <see cref="Axis"/> object (a modifier only ever sees a range and a transform).</summary>
    /// <param name="span">The range on screen before the pan, in data units.</param>
    /// <param name="delta">The shift, in the space the axis is drawn in: data units linear, decades log, symlog units
    /// on a symlog axis.</param>
    /// <param name="scale">The scale the axis is drawn with.</param>
    /// <param name="linThresh">The symlog linear threshold; ignored by the other scales.</param>
    /// <returns>The same span with both ends moved by <paramref name="delta"/> in that space.</returns>
    internal static DataSpan Shifted(DataSpan span, double delta, AxisScale scale, double linThresh) =>
        new(scale.Inverse(scale.Forward(span.Min, linThresh) + delta, linThresh),
            scale.Inverse(scale.Forward(span.Max, linThresh) + delta, linThresh));
}
