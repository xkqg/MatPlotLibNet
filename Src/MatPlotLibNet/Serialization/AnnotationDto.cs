// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Serialization;

/// <summary>Data-transfer object for an <see cref="Models.Annotation"/> text label optionally linked to an arrow target: an entry
/// of an axes' <c>annotations</c>. A value at the one a new annotation has is left out.</summary>
internal sealed record AnnotationDto
{
    public string? Text { get; init; }
    public double X { get; init; }
    public double Y { get; init; }
    public double? ArrowTargetX { get; init; }
    public double? ArrowTargetY { get; init; }
    public string? ConnectionStyle { get; init; }
    public double? ConnectionRad { get; init; }
    public double? ArrowHeadSize { get; init; }
    public string? BoxStyle { get; init; }
    public double? BoxPadding { get; init; }
    public double? BoxCornerRadius { get; init; }
    /// <summary>The coordinate system X and Y are read in; absent for data coordinates.</summary>
    public AnnotationCoordinates? Coordinates { get; init; }
    /// <summary>The font of the text; absent when the text takes the theme's.</summary>
    public FontDto? Font { get; init; }
    /// <summary>The colour of the text (and of the arrow, unless it has its own); absent for the theme's text colour.</summary>
    public Color? Color { get; init; }
    /// <summary>The colour of the arrow; absent when it takes the text colour.</summary>
    public Color? ArrowColor { get; init; }
    /// <summary>How the text stands against its point; absent for left.</summary>
    public Rendering.TextAlignment? Alignment { get; init; }
    /// <summary>How the text stands against its point vertically; absent for the baseline.</summary>
    public Rendering.TextVerticalAlignment? VerticalAlignment { get; init; }
    /// <summary>How many pixels the text stands right of its point; absent for none.</summary>
    public double? OffsetX { get; init; }
    /// <summary>How many pixels the text stands below its point; absent for none.</summary>
    public double? OffsetY { get; init; }
    /// <summary>Whether the text moves out of the way of what stands where it was put; absent for no.</summary>
    public bool? AvoidOverlap { get; init; }
    /// <summary>The text's rotation in degrees; absent for none.</summary>
    public double? Rotation { get; init; }
    /// <summary>The arrow's style; absent for the simple arrow.</summary>
    public ArrowStyle? ArrowStyle { get; init; }
    /// <summary>The fill behind the text when it has no callout box.</summary>
    public Color? BackgroundColor { get; init; }
    /// <summary>The fill of the callout box.</summary>
    public Color? BoxFaceColor { get; init; }
    /// <summary>The outline of the callout box.</summary>
    public Color? BoxEdgeColor { get; init; }
    /// <summary>The outline width of the callout box; absent for what a new <see cref="Models.Annotation"/> holds.</summary>
    public double? BoxLineWidth { get; init; }

    /// <summary>What a new annotation has for every setting that is not its text and its place.</summary>
    private static readonly Models.Annotation Rest = new("", 0, 0);

    /// <summary>The JSON's form of <paramref name="a"/>: what it says and where, and everything it is drawn with. A value at the
    /// one a new annotation has is left out.</summary>
    /// <param name="a">The annotation.</param>
    internal static AnnotationDto From(Models.Annotation a) => new()
    {
        Text = a.Text, X = a.X, Y = a.Y,
        ArrowTargetX = a.ArrowTargetX, ArrowTargetY = a.ArrowTargetY,
        ConnectionStyle = a.ConnectionStyle.OrNullAtRest(Rest.ConnectionStyle)?.ToWireName(),
        ConnectionRad = a.ConnectionRad.OrNullAtRest(Rest.ConnectionRad),
        ArrowHeadSize = a.ArrowHeadSize.OrNullAtRest(Rest.ArrowHeadSize),
        BoxStyle = a.BoxStyle.OrNullAtRest(Rest.BoxStyle)?.ToWireName(),
        BoxPadding = a.BoxPadding.OrNullAtRest(Rest.BoxPadding),
        BoxCornerRadius = a.BoxCornerRadius.OrNullAtRest(Rest.BoxCornerRadius),
        Coordinates = a.Coordinates.OrNullAtRest(Rest.Coordinates),
        Font = FontDto.From(a.Font),
        Color = a.Color,
        ArrowColor = a.ArrowColor,
        Alignment = a.Alignment.OrNullAtRest(Rest.Alignment),
        VerticalAlignment = a.VerticalAlignment.OrNullAtRest(Rest.VerticalAlignment),
        OffsetX = a.OffsetX.OrNullAtRest(Rest.OffsetX),
        OffsetY = a.OffsetY.OrNullAtRest(Rest.OffsetY),
        AvoidOverlap = a.AvoidOverlap.OrNullAtRest(Rest.AvoidOverlap),
        Rotation = a.Rotation.OrNullAtRest(Rest.Rotation),
        ArrowStyle = a.ArrowStyle.OrNullAtRest(Rest.ArrowStyle),
        BackgroundColor = a.BackgroundColor,
        BoxFaceColor = a.BoxFaceColor,
        BoxEdgeColor = a.BoxEdgeColor,
        BoxLineWidth = a.BoxLineWidth.OrNullAtRest(Rest.BoxLineWidth),
    };

    /// <summary>Adds the annotation this record describes to <paramref name="axes"/>, each setting it leaves out at a new
    /// annotation's value.</summary>
    /// <param name="axes">The axes being read.</param>
    internal void AddTo(Axes axes)
    {
        var ann = axes.Annotate(Text ?? "", X, Y);
        ann.ArrowTargetX = ArrowTargetX;
        ann.ArrowTargetY = ArrowTargetY;
        if (ConnectionStyle.WireEnum<Models.ConnectionStyle>() is { } connectionStyle) ann.ConnectionStyle = connectionStyle;
        if (ConnectionRad is { } connectionRad) ann.ConnectionRad = connectionRad;
        if (ArrowHeadSize is { } arrowHeadSize) ann.ArrowHeadSize = arrowHeadSize;
        if (BoxStyle.WireEnum<Models.BoxStyle>() is { } boxStyle) ann.BoxStyle = boxStyle;
        if (BoxPadding is { } boxPadding) ann.BoxPadding = boxPadding;
        if (BoxCornerRadius is { } boxCornerRadius) ann.BoxCornerRadius = boxCornerRadius;
        if (Coordinates is { } coordinates) ann.Coordinates = coordinates;
        if (Font is { } font) ann.Font = font.ToFont();
        ann.Color = Color;
        ann.ArrowColor = ArrowColor;
        if (Alignment is { } alignment) ann.Alignment = alignment;
        if (VerticalAlignment is { } verticalAlignment) ann.VerticalAlignment = verticalAlignment;
        if (OffsetX is { } offsetX) ann.OffsetX = offsetX;
        if (OffsetY is { } offsetY) ann.OffsetY = offsetY;
        if (AvoidOverlap is { } avoidOverlap) ann.AvoidOverlap = avoidOverlap;
        if (Rotation is { } rotation) ann.Rotation = rotation;
        if (ArrowStyle is { } arrowStyle) ann.ArrowStyle = arrowStyle;
        ann.BackgroundColor = BackgroundColor;
        ann.BoxFaceColor = BoxFaceColor;
        ann.BoxEdgeColor = BoxEdgeColor;
        if (BoxLineWidth is { } boxLineWidth) ann.BoxLineWidth = boxLineWidth;
    }
}
