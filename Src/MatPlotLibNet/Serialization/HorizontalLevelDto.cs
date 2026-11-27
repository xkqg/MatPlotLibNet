// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Tools;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Serialization;

/// <summary>Data-transfer object for a <see cref="HorizontalLevel"/>: an entry of an axes' <c>horizontalLevels</c>. The colour
/// travels as a hex string, and a dashed line style is left out, as a new level has it.</summary>
internal sealed record HorizontalLevelDto
{
    public double Value { get; init; }
    public string? Color { get; init; }
    public string? LineStyle { get; init; }
    public double LineWidth { get; init; } = 1.0;
    public string? Label { get; init; }

    /// <summary>The JSON's form of <paramref name="level"/>.</summary>
    /// <param name="level">The horizontal level.</param>
    internal static HorizontalLevelDto From(HorizontalLevel level) => new()
    {
        Value = level.Value,
        Color = level.Color is { } color ? color.ToHex() : null,
        LineStyle = level.LineStyle.OrNullAtRest(HorizontalLevel.RestingLineStyle)?.ToWireName(),
        LineWidth = level.LineWidth,
        Label = level.Label,
    };

    /// <summary>Adds the level this record describes to <paramref name="axes"/>.</summary>
    /// <param name="axes">The axes being read.</param>
    internal void AddTo(Axes axes)
    {
        var level = axes.AddLevel(Value);
        if (Color is { } hex) level.Color = Styling.Color.FromHex(hex);
        if (LineStyle.WireEnum<Styling.LineStyle>() is { } lineStyle) level.LineStyle = lineStyle;
        level.LineWidth = LineWidth;
        level.Label = Label;
    }
}
