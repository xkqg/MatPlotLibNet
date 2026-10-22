// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Serialization;

/// <summary>The figure JSON's form of a <see cref="TickConfig"/> (an axis's major or minor ticks): each property is
/// written only when it differs from the ticks a new axis has in that seat, and read back over those same ticks. The
/// major and the minor ticks of a new axis differ (minor ticks are hidden, shorter and thinner), so the seat's own
/// resting ticks are what a property is measured against.</summary>
internal sealed record TickConfigDto
{
    public bool? Visible { get; init; }
    public double? Spacing { get; init; }
    public string? Format { get; init; }
    public TickDirection? Direction { get; init; }
    public double? Length { get; init; }
    public double? Width { get; init; }
    public Color? Color { get; init; }
    public double? LabelSize { get; init; }
    public Color? LabelColor { get; init; }
    public double? Pad { get; init; }
    public bool? Mirror { get; init; }
    public double? LabelRotation { get; init; }

    /// <summary>The JSON's form of <paramref name="ticks"/>, or <see langword="null"/> when they equal
    /// <paramref name="resting"/>.</summary>
    /// <param name="ticks">The ticks the axis holds.</param>
    /// <param name="resting">The ticks a new axis holds in the same seat.</param>
    internal static TickConfigDto? From(TickConfig ticks, TickConfig resting) => ticks == resting ? null : new()
    {
        Visible = ticks.Visible.OrNullAtRest(resting.Visible),
        Spacing = ticks.Spacing.OrNullAtRest(resting.Spacing),
        Format = ticks.Format.OrNullAtRest(resting.Format),
        Direction = ticks.Direction.OrNullAtRest(resting.Direction),
        Length = ticks.Length.OrNullAtRest(resting.Length),
        Width = ticks.Width.OrNullAtRest(resting.Width),
        Color = ticks.Color.OrNullAtRest(resting.Color),
        LabelSize = ticks.LabelSize.OrNullAtRest(resting.LabelSize),
        LabelColor = ticks.LabelColor.OrNullAtRest(resting.LabelColor),
        Pad = ticks.Pad.OrNullAtRest(resting.Pad),
        Mirror = ticks.Mirror.OrNullAtRest(resting.Mirror),
        LabelRotation = ticks.LabelRotation.OrNullAtRest(resting.LabelRotation),
    };

    /// <summary>The ticks this record describes, over <paramref name="resting"/>.</summary>
    /// <param name="resting">The ticks a new axis holds in the seat being read.</param>
    internal TickConfig ToTickConfig(TickConfig resting) => resting with
    {
        Visible = Visible ?? resting.Visible,
        Spacing = Spacing ?? resting.Spacing,
        Format = Format ?? resting.Format,
        Direction = Direction ?? resting.Direction,
        Length = Length ?? resting.Length,
        Width = Width ?? resting.Width,
        Color = Color ?? resting.Color,
        LabelSize = LabelSize ?? resting.LabelSize,
        LabelColor = LabelColor ?? resting.LabelColor,
        Pad = Pad ?? resting.Pad,
        Mirror = Mirror ?? resting.Mirror,
        LabelRotation = LabelRotation ?? resting.LabelRotation,
    };
}
