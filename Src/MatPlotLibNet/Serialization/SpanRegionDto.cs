// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Serialization;

/// <summary>Data-transfer object for a shaded span region (axhspan / axvspan) on a subplot: the figure JSON's form of a
/// <see cref="SpanRegion"/>, an entry of an axes' <c>spans</c>. A line style of none and a line width of 1 are left out, as a new
/// span has them.</summary>
internal sealed record SpanRegionDto
{
    public double Min { get; init; }
    public double Max { get; init; }
    public string? Orientation { get; init; }
    public double Alpha { get; init; }
    public string? LineStyle { get; init; }
    public double? LineWidth { get; init; }
    public string? Label { get; init; }
    /// <summary>The fill of the span; absent when it takes the theme's.</summary>
    public Color? Color { get; init; }
    /// <summary>The outline of the span; absent when it takes the fill colour.</summary>
    public Color? EdgeColor { get; init; }

    /// <summary>The JSON's form of <paramref name="span"/>.</summary>
    /// <param name="span">The span.</param>
    internal static SpanRegionDto From(SpanRegion span) => new()
    {
        Min = span.Min,
        Max = span.Max,
        Orientation = span.Orientation.ToWireName(),
        Alpha = span.Alpha,
        LineStyle = span.LineStyle.OrNullAtRest(SpanRegion.RestingLineStyle)?.ToWireName(),
        LineWidth = span.LineWidth.OrNullAtRest(SpanRegion.RestingLineWidth),
        Label = span.Label,
        Color = span.Color,
        EdgeColor = span.EdgeColor,
    };

    /// <summary>Adds the span this record describes to <paramref name="axes"/>: in the orientation the record names (in any case),
    /// horizontal when it names none.</summary>
    /// <param name="axes">The axes being read.</param>
    internal void AddTo(Axes axes)
    {
        var span = axes.Overlays.AddSpan(Min, Max, Orientation.WireEnum<Models.Orientation>() ?? Models.Orientation.Horizontal);
        span.Alpha = Alpha;
        if (LineStyle.WireEnum<Styling.LineStyle>() is { } lineStyle) span.LineStyle = lineStyle;
        if (LineWidth is { } lineWidth) span.LineWidth = lineWidth;
        span.Label = Label;
        span.Color = Color;
        span.EdgeColor = EdgeColor;
    }
}
