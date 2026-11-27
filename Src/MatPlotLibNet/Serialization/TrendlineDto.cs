// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Tools;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Serialization;

/// <summary>Data-transfer object for a <see cref="Trendline"/>: an entry of an axes' <c>trendlines</c>. The colour travels as a
/// hex string, and a solid line style is left out, as a new trendline has it.</summary>
internal sealed record TrendlineDto
{
    public double X1 { get; init; }
    public double Y1 { get; init; }
    public double X2 { get; init; }
    public double Y2 { get; init; }
    public string? Color { get; init; }
    public string? LineStyle { get; init; }
    public double LineWidth { get; init; } = 1.0;
    public string? Label { get; init; }
    public bool IsExtended { get; init; }

    /// <summary>The JSON's form of <paramref name="line"/>.</summary>
    /// <param name="line">The trendline.</param>
    internal static TrendlineDto From(Trendline line) => new()
    {
        X1 = line.X1, Y1 = line.Y1, X2 = line.X2, Y2 = line.Y2,
        Color = line.Color is { } color ? color.ToHex() : null,
        LineStyle = line.LineStyle.OrNullAtRest(Trendline.RestingLineStyle)?.ToWireName(),
        LineWidth = line.LineWidth,
        Label = line.Label,
        IsExtended = line.IsExtended,
    };

    /// <summary>Adds the trendline this record describes to <paramref name="axes"/>.</summary>
    /// <param name="axes">The axes being read.</param>
    internal void AddTo(Axes axes)
    {
        var line = axes.AddTrendline(X1, Y1, X2, Y2);
        if (Color is { } hex) line.Color = Styling.Color.FromHex(hex);
        if (LineStyle.WireEnum<Styling.LineStyle>() is { } lineStyle) line.LineStyle = lineStyle;
        line.LineWidth = LineWidth;
        line.Label = Label;
        line.IsExtended = IsExtended;
    }
}
