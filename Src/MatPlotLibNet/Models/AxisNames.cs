// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Linq;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Models;

/// <summary>The names an axes shows along the two axes one series stands on, as the data table asks for them: a position a
/// named tick stands on reads as that tick's name, by the rule the picture is drawn by
/// (<see cref="AxesTickSourceExtensions.NamesAt"/>). The names are the axes', never the series': the table asks the axes and
/// hands the answer to a series that has to place it (<see cref="ITableNamedByAxes"/>).
/// <para>Only the primary axes name categories, so a series on any other axis — a Y axis of the container past the
/// primary, on either side, or the top X axis — is named along neither that axis nor, through it, the primary
/// one.</para></summary>
/// <param name="Axes">The axes the series is drawn on.</param>
/// <param name="OnPrimaryX">Whether the series stands on the primary X axis.</param>
/// <param name="OnPrimaryY">Whether the series stands on the primary Y axis.</param>
internal readonly record struct AxisNames(Axes Axes, bool OnPrimaryX, bool OnPrimaryY)
{
    /// <summary>The names <paramref name="axes"/> shows along the axes <paramref name="series"/> stands on.</summary>
    /// <param name="axes">The axes the series is drawn on.</param>
    /// <param name="series">One of its series.</param>
    /// <returns>The names along the series' axes.</returns>
    /// <remarks>A series stands on the primary Y axis unless a Y axis slot past the primary holds it
    /// (<see cref="SeriesSlotExtensions.YSlotSeats"/>: every seat but the primary's numbers its series on a list of its
    /// own); a top X axis series stands on the primary Y axis.</remarks>
    internal static AxisNames Of(Axes axes, ISeries series) =>
        new(axes,
            !axes.XSecondarySeries.Contains(series),
            !axes.YSlotSeats().Any(seat => seat.Pair != SeriesAxisPair.Primary && seat.Slot.Series.Contains(series)));

    /// <summary>The names shown at <paramref name="positions"/> along <paramref name="axis"/>: one for each position
    /// (<see langword="null"/> where it cannot be placed), or <see langword="null"/> when that axis does not name every
    /// position that can be placed.</summary>
    /// <param name="axis">The axis the positions are along; <see cref="DataAxis.None"/> names nothing.</param>
    /// <param name="positions">The positions, in the axis' data units.</param>
    /// <returns>The names, or <see langword="null"/>.</returns>
    internal string?[]? Along(DataAxis axis, IReadOnlyList<double> positions) => axis switch
    {
        DataAxis.X when OnPrimaryX => Axes.NamesAt(isX: true, positions),
        DataAxis.Y when OnPrimaryY => Axes.NamesAt(isX: false, positions),
        _ => null,
    };
}
