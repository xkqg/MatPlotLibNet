// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Serialization;

/// <summary>Data-transfer object for a single axis spine (border line) on a subplot.</summary>
internal sealed record SpineConfigDto
{
    public bool Visible { get; init; } = true;
    public string? Position { get; init; }
    public double PositionValue { get; init; }
    /// <summary>The width the spine was given; absent when it was given none and follows its theme (and in a JSON written before
    /// a spine could follow it, which names every width).</summary>
    public double? LineWidth { get; init; }
    public Color? Color { get; init; }
    /// <summary>The spine's line style; absent for solid, and in a JSON written before spines had one.</summary>
    public LineStyle? LineStyle { get; init; }
    /// <summary>Where the spine begins along its own axis, in data units; absent when it starts at the edge of the plot.</summary>
    public double? BoundsFrom { get; init; }
    /// <summary>Where the spine ends along its own axis, in data units; absent when it ends at the edge of the plot.</summary>
    public double? BoundsTo { get; init; }

    /// <summary>The JSON's form of <paramref name="spine"/>: its position named in lower case, left out at the edge, and its
    /// line style left out while it is solid.</summary>
    internal static SpineConfigDto Of(SpineConfig spine) => new()
    {
        Visible = spine.Visible,
        Position = spine.Position.OrNullAtRest(SpineConfig.RestingPosition)?.ToWireName(),
        PositionValue = spine.PositionValue,
        LineWidth = spine.GivenLineWidth,
        Color = spine.Color,
        LineStyle = spine.LineStyle.OrNullAtRest(SpineConfig.RestingLineStyle),
        BoundsFrom = spine.BoundsFrom,
        BoundsTo = spine.BoundsTo,
    };

    /// <summary>The spine this record describes; a position it does not name stands at the edge.</summary>
    internal SpineConfig ToSpine() => WithGivenWidth(new()
    {
        Visible = Visible,
        Position = Position.WireEnum<SpinePosition>() ?? SpineConfig.RestingPosition,
        PositionValue = PositionValue,
        Color = Color,
        LineStyle = LineStyle ?? SpineConfig.RestingLineStyle,
        BoundsFrom = BoundsFrom,
        BoundsTo = BoundsTo,
    });

    /// <summary><paramref name="spine"/> with the width this record names, if it names one: a spine without one follows its
    /// theme, which a width set to the default would not.</summary>
    private SpineConfig WithGivenWidth(SpineConfig spine) => LineWidth is { } width ? spine with { LineWidth = width } : spine;
}
