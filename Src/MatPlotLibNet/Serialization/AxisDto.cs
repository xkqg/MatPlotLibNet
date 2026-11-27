// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Serialization;

/// <summary>Data-transfer object for an axis configuration (label, limits, scale, and the colours of its ticks and title).</summary>
internal sealed record AxisDto
{
    public string? Label { get; init; }
    public double? Min { get; init; }
    public double? Max { get; init; }
    public string? Scale { get; init; }
    /// <summary>Colour of the major tick marks; absent in a JSON written before axis colours existed.</summary>
    public Color? TickColor { get; init; }
    /// <summary>Colour of the major tick labels; absent in a JSON written before axis colours existed.</summary>
    public Color? TickLabelColor { get; init; }
    /// <summary>Colour of the axis title; absent in a JSON written before axis colours existed.</summary>
    public Color? LabelColor { get; init; }
    /// <summary>The axis's tick locator when it is one of the library's; absent for the default ticks, and for a locator
    /// written outside the library (code the JSON cannot carry).</summary>
    public TickLocatorDto? TickLocator { get; init; }
    /// <summary>The axis's tick formatter when it is one of the library's; absent for the default labels, and for a
    /// formatter written outside the library.</summary>
    public TickFormatterDto? TickFormatter { get; init; }
    /// <summary>The link of the axis (<see cref="Axis.Link"/>) when it is one of the library's named transforms: the axis shows
    /// the primary axis' range in another unit. Absent for an axis with a range of its own, and for a transform made of
    /// functions of the caller's own (code the JSON cannot carry). A 1.28.0 reader skips it.</summary>
    public AxisTransformDto? Link { get; init; }
    /// <summary><see langword="true"/> for an axis that runs from its maximum to its minimum.</summary>
    public bool? Inverted { get; init; }
    /// <summary>The data padding as a fraction of the range; absent when the theme's applies.</summary>
    public double? Margin { get; init; }
    /// <summary>The linear threshold of a symmetric-log axis; absent for an axis given none, which takes its theme's or
    /// <see cref="Rendering.SymlogTransform.DefaultLinThresh"/>.</summary>
    public double? SymLogLinThresh { get; init; }
    /// <summary>The major ticks, where they differ from a new axis's; their colours are <see cref="TickColor"/> and
    /// <see cref="TickLabelColor"/>.</summary>
    public TickConfigDto? MajorTicks { get; init; }
    /// <summary>The minor ticks, where they differ from a new axis's.</summary>
    public TickConfigDto? MinorTicks { get; init; }
    /// <summary>The style of the axis title, where it overrides anything; its colour is <see cref="LabelColor"/>.</summary>
    public TextStyleDto? LabelStyle { get; init; }

    /// <summary>The ticks, the minor ticks and the linear threshold of a new axis: the values an axis setting is left out
    /// at, and read back over.</summary>
    private static readonly TickConfig RestingMajorTicks = new Axis().MajorTicks;
    private static readonly TickConfig RestingMinorTicks = new Axis().MinorTicks;

    /// <summary>The JSON's form of <paramref name="axis"/>. A tick locator or formatter of the library describes itself; one
    /// written outside the library is left out (see <see cref="Axis.TickLocator"/>), and so is a link made of functions of the
    /// caller's own (see <see cref="Axis.Link"/>). The colours of the major ticks and of the label stay in the fields that
    /// carried them before the rest of the ticks and the label style reached the JSON.</summary>
    internal static AxisDto Of(Axis axis) => new()
    {
        Label = axis.Label,
        Min = axis.Min,
        Max = axis.Max,
        Scale = axis.Scale.ToWireName(),
        TickColor = axis.MajorTicks.Color,
        TickLabelColor = axis.MajorTicks.LabelColor,
        LabelColor = axis.LabelStyle?.Color,
        TickLocator = (axis.TickLocator as IJsonDescribed<TickLocatorDto>)?.Describe(),
        TickFormatter = (axis.TickFormatter as IJsonDescribed<TickFormatterDto>)?.Describe(),
        Link = (axis.Link as IJsonDescribed<AxisTransformDto>)?.Describe(),
        Inverted = axis.Inverted.OrNullAtRest(false),
        Margin = axis.Margin,
        SymLogLinThresh = axis.GivenSymLogLinThresh,
        MajorTicks = TickConfigDto.From(axis.MajorTicks with { Color = null, LabelColor = null }, RestingMajorTicks),
        MinorTicks = TickConfigDto.From(axis.MinorTicks, RestingMinorTicks),
        LabelStyle = TextStyleDto.From(axis.LabelStyle is { } style ? style with { Color = null } : null),
    };

    /// <summary>Reads this axis back onto <paramref name="axis"/>. The tick locator is made first, so that a formatter that
    /// writes what its locator chose is paired with the axis's own locator again.</summary>
    /// <param name="axis">The axis read into.</param>
    internal void ApplyTo(Axis axis)
    {
        axis.Label = Label;
        axis.Min = Min;
        axis.Max = Max;
        if (Scale.WireEnum<AxisScale>() is { } scale) axis.Scale = scale;
        if (Inverted is { } inverted) axis.Inverted = inverted;
        if (Margin is { } margin) axis.Margin = margin;
        if (SymLogLinThresh is { } linThresh) axis.SymLogLinThresh = linThresh;
        if (TickLocator is { } locator) axis.TickLocator = locator.Restore();
        if (TickFormatter is { } formatter) axis.TickFormatter = formatter.Restore(axis.TickLocator);
        if (Link?.Restore() is { } link) axis.Link = link;
        if (MajorTicks is { } majorTicks) axis.MajorTicks = majorTicks.ToTickConfig(RestingMajorTicks);
        if (MinorTicks is { } minorTicks) axis.MinorTicks = minorTicks.ToTickConfig(RestingMinorTicks);
        if (LabelStyle is { } labelStyle) axis.LabelStyle = labelStyle.ToTextStyle();
        if (TickColor is { } tickColor) axis.MajorTicks = axis.MajorTicks with { Color = tickColor };
        if (TickLabelColor is { } tickLabelColor) axis.MajorTicks = axis.MajorTicks with { LabelColor = tickLabelColor };
        if (LabelColor is { } labelColor) axis.LabelStyle = (axis.LabelStyle ?? new TextStyle()) with { Color = labelColor };
    }
}
