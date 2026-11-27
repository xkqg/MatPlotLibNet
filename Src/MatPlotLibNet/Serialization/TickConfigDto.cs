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
    /// <summary>Whether the labels stand on the axis' own side; <see langword="false"/> leaves them to the opposite side.</summary>
    public bool? LabelsOnOwnSide { get; init; }
    public double? LabelRotation { get; init; }

    /// <summary>The JSON's form of <paramref name="ticks"/>, or <see langword="null"/> when they equal
    /// <paramref name="resting"/>.</summary>
    /// <param name="ticks">The ticks the axis holds.</param>
    /// <param name="resting">The ticks a new axis holds in the same seat.</param>
    /// <returns>The record of the ticks, or <see langword="null"/> when they are the resting ticks.</returns>
    internal static TickConfigDto? From(TickConfig ticks, TickConfig resting) => ticks.DescribedUnlessAtRest(resting, Describe);

    /// <summary>The JSON's form of ticks away from <paramref name="resting"/>, each property against the resting one's.</summary>
    private static TickConfigDto Describe(TickConfig ticks, TickConfig resting) => new()
    {
        Visible = ticks.GivenVisible.OrNullAtRest(resting.GivenVisible),
        Spacing = ticks.Spacing.OrNullAtRest(resting.Spacing),
        Format = ticks.Format.OrNullAtRest(resting.Format),
        Direction = ticks.GivenDirection.OrNullAtRest(resting.GivenDirection),
        Length = ticks.GivenLength.OrNullAtRest(resting.GivenLength),
        Width = ticks.GivenWidth.OrNullAtRest(resting.GivenWidth),
        Color = ticks.Color.OrNullAtRest(resting.Color),
        LabelSize = ticks.LabelSize.OrNullAtRest(resting.LabelSize),
        LabelColor = ticks.LabelColor.OrNullAtRest(resting.LabelColor),
        Pad = ticks.GivenPad.OrNullAtRest(resting.GivenPad),
        Mirror = ticks.Mirror.OrNullAtRest(resting.Mirror),
        LabelsOnOwnSide = ticks.LabelsOnOwnSide.OrNullAtRest(resting.LabelsOnOwnSide),
        LabelRotation = ticks.LabelRotation.OrNullAtRest(resting.LabelRotation),
    };

    /// <summary>The ticks this record describes, over <paramref name="resting"/>.</summary>
    /// <param name="resting">The ticks a new axis holds in the seat being read.</param>
    /// <returns>The ticks of the record.</returns>
    internal TickConfig ToTickConfig(TickConfig resting)
    {
        var ticks = resting with
        {
            Spacing = Spacing ?? resting.Spacing,
            Format = Format ?? resting.Format,
            Color = Color ?? resting.Color,
            LabelSize = LabelSize ?? resting.LabelSize,
            LabelColor = LabelColor ?? resting.LabelColor,
            Mirror = Mirror ?? resting.Mirror,
            LabelsOnOwnSide = LabelsOnOwnSide ?? resting.LabelsOnOwnSide,
            LabelRotation = LabelRotation ?? resting.LabelRotation,
        };
        // A visibility, a direction, a length, a width or a pad the record does not write was not given, and is left so: the
        // ticks then draw as their kind, their scale and their theme say.
        if (Visible is { } visible) ticks = ticks with { Visible = visible };
        if (Direction is { } direction) ticks = ticks with { Direction = direction };
        if (Length is { } length) ticks = ticks with { Length = length };
        if (Width is { } width) ticks = ticks with { Width = width };
        if (Pad is { } pad) ticks = ticks with { Pad = pad };
        return ticks;
    }
}
