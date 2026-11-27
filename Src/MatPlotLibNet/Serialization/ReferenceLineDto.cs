// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Serialization;

/// <summary>Data-transfer object for a horizontal or vertical reference line drawn across a subplot: the figure JSON's form of a
/// <see cref="ReferenceLine"/>, an entry of an axes' <c>referenceLines</c>. It writes itself (<see cref="From"/>) and reads
/// itself back onto an axes (<see cref="AddTo"/>), so the two directions live side by side.</summary>
internal sealed record ReferenceLineDto
{
    public double Value { get; init; }
    public string? Orientation { get; init; }
    public string? LineStyle { get; init; }
    /// <summary>The width the line was given (<see cref="ReferenceLine.GivenLineWidth"/>); absent while none was and the theme decides.</summary>
    public double? LineWidth { get; init; }
    public string? Label { get; init; }
    /// <summary>The line's colour; absent when it takes the theme's.</summary>
    public Color? Color { get; init; }
    /// <summary>The colour a verb chose for the line where none was named (<see cref="ReferenceLine.VerbColor"/>); absent when no verb did.</summary>
    public Color? VerbColor { get; init; }

    /// <summary>The JSON's form of <paramref name="line"/>.</summary>
    /// <param name="line">The reference line.</param>
    /// <returns>The record of the line.</returns>
    internal static ReferenceLineDto From(ReferenceLine line) => new()
    {
        Value = line.Value,
        Orientation = line.Orientation.ToWireName(),
        LineStyle = line.LineStyle.ToWireName(),
        LineWidth = line.GivenLineWidth,
        Label = line.Label,
        Color = line.Color,
        VerbColor = line.VerbColor,
    };

    /// <summary>Adds the reference line this record describes to <paramref name="axes"/>: in the orientation the record names (in any case),
    /// horizontal when it names none.</summary>
    /// <param name="axes">The axes being read.</param>
    internal void AddTo(Axes axes)
    {
        var line = axes.Overlays.AddLine(Value, Orientation.WireEnum<Models.Orientation>() ?? Models.Orientation.Horizontal);
        if (LineStyle.WireEnum<Styling.LineStyle>() is { } lineStyle) line.LineStyle = lineStyle;
        if (LineWidth is { } width) line.LineWidth = width;
        line.Label = Label;
        line.Color = Color;
        line.VerbColor = VerbColor;
    }
}
