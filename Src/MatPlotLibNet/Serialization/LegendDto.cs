// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Serialization;

/// <summary>The figure JSON's form of an axes' <see cref="Legend"/>: each property is written only when it differs from
/// a new legend's, and the legend is left out whole while it is a new legend's.</summary>
internal sealed record LegendDto
{
    public bool? Visible { get; init; }
    public LegendPosition? Position { get; init; }
    public int? NCols { get; init; }
    public double? FontSize { get; init; }
    public string? Title { get; init; }
    public double? TitleFontSize { get; init; }
    public bool? FrameOn { get; init; }
    public double? FrameAlpha { get; init; }
    public bool? FancyBox { get; init; }
    public bool? Shadow { get; init; }
    public Color? EdgeColor { get; init; }
    public Color? FaceColor { get; init; }
    public double? MarkerScale { get; init; }
    public double? LabelSpacing { get; init; }
    public double? ColumnSpacing { get; init; }
    public bool? LegendValues { get; init; }

    private static readonly Legend Rest = new();

    /// <summary>The JSON's form of <paramref name="legend"/>, or <see langword="null"/> when it is a new legend's.</summary>
    internal static LegendDto? From(Legend legend) => legend == Rest ? null : new()
    {
        Visible = legend.Visible.OrNullAtRest(Rest.Visible),
        Position = legend.Position.OrNullAtRest(Rest.Position),
        NCols = legend.NCols.OrNullAtRest(Rest.NCols),
        FontSize = legend.FontSize,
        Title = legend.Title,
        TitleFontSize = legend.TitleFontSize,
        FrameOn = legend.FrameOn.OrNullAtRest(Rest.FrameOn),
        FrameAlpha = legend.FrameAlpha.OrNullAtRest(Rest.FrameAlpha),
        FancyBox = legend.FancyBox.OrNullAtRest(Rest.FancyBox),
        Shadow = legend.Shadow.OrNullAtRest(Rest.Shadow),
        EdgeColor = legend.EdgeColor,
        FaceColor = legend.FaceColor,
        MarkerScale = legend.MarkerScale.OrNullAtRest(Rest.MarkerScale),
        LabelSpacing = legend.LabelSpacing.OrNullAtRest(Rest.LabelSpacing),
        ColumnSpacing = legend.ColumnSpacing.OrNullAtRest(Rest.ColumnSpacing),
        LegendValues = legend.LegendValues.OrNullAtRest(Rest.LegendValues),
    };

    /// <summary>The legend this record describes.</summary>
    internal Legend ToLegend() => new()
    {
        Visible = Visible ?? Rest.Visible,
        Position = Position ?? Rest.Position,
        NCols = NCols ?? Rest.NCols,
        FontSize = FontSize,
        Title = Title,
        TitleFontSize = TitleFontSize,
        FrameOn = FrameOn ?? Rest.FrameOn,
        FrameAlpha = FrameAlpha ?? Rest.FrameAlpha,
        FancyBox = FancyBox ?? Rest.FancyBox,
        Shadow = Shadow ?? Rest.Shadow,
        EdgeColor = EdgeColor,
        FaceColor = FaceColor,
        MarkerScale = MarkerScale ?? Rest.MarkerScale,
        LabelSpacing = LabelSpacing ?? Rest.LabelSpacing,
        ColumnSpacing = ColumnSpacing ?? Rest.ColumnSpacing,
        LegendValues = LegendValues ?? Rest.LegendValues,
    };
}
