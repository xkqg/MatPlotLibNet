// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Serialization;

/// <summary>How the figure JSON names a patch of an axes (an entry of the <c>patches</c> list of a subplot): <c>"type"</c> says
/// which kind of patch it is, the other fields carry what it was made with and what it was styled with. The record is the style
/// every patch has, and each field is written only while it differs from the value a new patch of the kind has, so a patch at rest
/// writes its kind and its geometry and nothing more, and an axes without patches writes no list. An entry of a kind this build
/// does not know (JSON from a later release, or a typo) or with no kind reads as this base record, which adds no patch: it is
/// skipped, the other patches still load, and the skip is reported through <see cref="ChartDiagnostics"/>.</summary>
/// <remarks>The base is not abstract on purpose: a reader falls back to the contract of the base type for a kind it does not know,
/// and an abstract base would fail the whole figure instead (<c>NormalizerDto</c> and <c>TickLocatorDto</c> are built the same
/// way). Each layer of the stack owns its own fields, both ways: a constructor that writes them from the patch against a patch
/// at rest, and the step of <c>AddTo</c> that sets them on the patch it has made. The style fields are the enum-valued ones
/// <see cref="EdgeLineStyle"/> and <see cref="Hatch"/> (and <see cref="ArrowDto.Shape"/>), written as lower-camel names.</remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type", IgnoreUnrecognizedTypeDiscriminators = true)]
[JsonDerivedType(typeof(RectangleDto), "rectangle")]
[JsonDerivedType(typeof(PolygonDto), "polygon")]
[JsonDerivedType(typeof(ArrowDto), "arrow")]
[JsonDerivedType(typeof(CircleDto), "circle")]
[JsonDerivedType(typeof(EllipseDto), "ellipse")]
[JsonDerivedType(typeof(WedgeDto), "wedge")]
[JsonDerivedType(typeof(ArcDto), "arc")]
internal record PatchDto
{
    /// <summary>The record a reader makes: every field absent until the JSON sets it.</summary>
    public PatchDto() { }

    /// <summary>The record a writer makes: the style of <paramref name="patch"/>, each field left out while it equals the one of
    /// <paramref name="rest"/>, a patch of the same kind as it is made.</summary>
    /// <param name="patch">The patch being written.</param>
    /// <param name="rest">A new patch of the same kind.</param>
    private protected PatchDto(Patch patch, Patch rest)
    {
        EdgeColor = patch.EdgeColor.OrNullAtRest(rest.EdgeColor);
        EdgeWidth = patch.EdgeWidth.OrNullAtRest(rest.EdgeWidth);
        EdgeLineStyle = patch.EdgeLineStyle.OrNullAtRest(rest.EdgeLineStyle);
        Alpha = patch.Alpha.OrNullAtRest(rest.Alpha);
        Label = patch.Label;
        Hatch = patch.Hatch.OrNullAtRest(rest.Hatch);
        HatchColor = patch.HatchColor;
    }

    /// <summary>The colour of the outline; absent for the one a new patch of the kind has (none, and black for an arrow and an
    /// arc).</summary>
    public Color? EdgeColor { get; init; }

    /// <summary>The width of the outline in pixels; absent for 1.</summary>
    public double? EdgeWidth { get; init; }

    /// <summary>The dash pattern of the outline; absent for solid.</summary>
    public LineStyle? EdgeLineStyle { get; init; }

    /// <summary>The opacity of the whole patch, 0 to 1; absent for 1. A value outside that range is refused when it is read, as
    /// the patch refuses it.</summary>
    public double? Alpha { get; init; }

    /// <summary>The name a legend lists the patch under; absent for none.</summary>
    public string? Label { get; init; }

    /// <summary>The pattern drawn over the face; absent for none.</summary>
    public HatchPattern? Hatch { get; init; }

    /// <summary>The colour of the hatch; absent for the edge colour.</summary>
    public Color? HatchColor { get; init; }

    /// <summary>Adds the patch this record describes to <paramref name="axes"/>. The base record describes no patch: it is what a
    /// kind this build does not know reads as, so it adds nothing and says so.</summary>
    /// <param name="axes">The axes being read.</param>
    internal virtual void AddTo(Axes axes) =>
        ChartDiagnostics.Emit(new ChartDiagnostic(nameof(ChartSerializer),
            "A patch of a kind this build does not know was skipped during deserialization.", null));

    /// <summary>Sets the style this record carries on <paramref name="patch"/>, which keeps what it was made with for a field
    /// that is absent.</summary>
    /// <param name="patch">The patch just made from the geometry.</param>
    /// <typeparam name="TPatch">The kind of patch.</typeparam>
    private protected TPatch Style<TPatch>(TPatch patch) where TPatch : Patch
    {
        patch.EdgeColor = EdgeColor ?? patch.EdgeColor;
        patch.EdgeWidth = EdgeWidth ?? patch.EdgeWidth;
        patch.EdgeLineStyle = EdgeLineStyle ?? patch.EdgeLineStyle;
        patch.Alpha = Alpha ?? patch.Alpha;
        patch.Label = Label;
        patch.Hatch = Hatch ?? patch.Hatch;
        patch.HatchColor = HatchColor;
        return patch;
    }
}

/// <summary>The style of a patch that has a face: <see cref="PatchDto"/> and the colour of the face.</summary>
internal abstract record FilledPatchDto : PatchDto
{
    /// <summary>The record a reader makes: every field absent until the JSON sets it.</summary>
    protected FilledPatchDto() { }

    /// <summary>The record a writer makes: the style of <paramref name="patch"/> and its face.</summary>
    /// <param name="patch">The patch being written.</param>
    /// <param name="rest">A new patch of the same kind.</param>
    private protected FilledPatchDto(FilledPatch patch, FilledPatch rest) : base(patch, rest)
    {
        FaceColor = patch.FaceColor.OrNullAtRest(rest.FaceColor);
    }

    /// <summary>The colour of the face; absent for the first colour of the cycle.</summary>
    public Color? FaceColor { get; init; }

    /// <summary>Sets the face this record carries and then the style on <paramref name="patch"/>.</summary>
    /// <param name="patch">The patch just made from the geometry.</param>
    /// <typeparam name="TPatch">The kind of patch.</typeparam>
    private protected TPatch Fill<TPatch>(TPatch patch) where TPatch : FilledPatch
    {
        patch.FaceColor = FaceColor ?? patch.FaceColor;
        return Style(patch);
    }
}

/// <summary>Describes a <see cref="Rectangle"/>.</summary>
internal sealed record RectangleDto : FilledPatchDto
{
    private static readonly Rectangle Rest = new(default, 0, 0);

    /// <summary>The record a reader makes.</summary>
    public RectangleDto() { }

    /// <summary>The record a writer makes.</summary>
    /// <param name="rectangle">The rectangle being written.</param>
    internal RectangleDto(Rectangle rectangle) : base(rectangle, Rest)
    {
        Anchor = rectangle.Anchor;
        Width = rectangle.Width;
        Height = rectangle.Height;
        Angle = rectangle.Angle.OrNullAtRest(Rest.Angle);
    }

    /// <summary>The corner the rectangle is measured from, in data units.</summary>
    public DataPoint Anchor { get; init; }

    /// <summary>The width along X, in data units.</summary>
    public double Width { get; init; }

    /// <summary>The height along Y, in data units.</summary>
    public double Height { get; init; }

    /// <summary>The angle the rectangle is turned through about its anchor, in degrees; absent for 0.</summary>
    public double? Angle { get; init; }

    /// <inheritdoc />
    internal override void AddTo(Axes axes) => axes.AddPatch(Fill(new Rectangle(Anchor, Width, Height) { Angle = Angle ?? Rest.Angle }));
}

/// <summary>Describes a <see cref="Polygon"/>.</summary>
internal sealed record PolygonDto : FilledPatchDto
{
    private static readonly Polygon Rest = new([]);

    /// <summary>The record a reader makes.</summary>
    public PolygonDto() { }

    /// <summary>The record a writer makes.</summary>
    /// <param name="polygon">The polygon being written.</param>
    internal PolygonDto(Polygon polygon) : base(polygon, Rest)
    {
        Vertices = [.. polygon.Vertices];
        Closed = polygon.Closed.OrNullAtRest(Rest.Closed);
    }

    /// <summary>The vertices in order, in data units; absent reads as none.</summary>
    public List<DataPoint>? Vertices { get; init; }

    /// <summary>Whether the outline closes back to its first vertex; absent for true.</summary>
    public bool? Closed { get; init; }

    /// <inheritdoc />
    internal override void AddTo(Axes axes) => axes.AddPatch(Fill(new Polygon(Vertices ?? []) { Closed = Closed ?? Rest.Closed }));
}

/// <summary>Describes an <see cref="Arrow"/>. The sizes of the head are absent for a head that follows the shaft, which is not
/// the same as a head of size 0.</summary>
internal sealed record ArrowDto : FilledPatchDto
{
    private static readonly Arrow Rest = new(default, 0, 0);

    /// <summary>The record a reader makes.</summary>
    public ArrowDto() { }

    /// <summary>The record a writer makes.</summary>
    /// <param name="arrow">The arrow being written.</param>
    internal ArrowDto(Arrow arrow) : base(arrow, Rest)
    {
        Start = arrow.Start;
        Dx = arrow.Dx;
        Dy = arrow.Dy;
        Width = arrow.Width.OrNullAtRest(Rest.Width);
        HeadWidth = arrow.HeadWidth;
        HeadLength = arrow.HeadLength;
        LengthIncludesHead = arrow.LengthIncludesHead.OrNullAtRest(Rest.LengthIncludesHead);
        Shape = arrow.Shape.OrNullAtRest(Rest.Shape);
        Overhang = arrow.Overhang.OrNullAtRest(Rest.Overhang);
    }

    /// <summary>Where the shaft starts, in data units.</summary>
    public DataPoint Start { get; init; }

    /// <summary>How far the shaft runs along X, in data units.</summary>
    public double Dx { get; init; }

    /// <summary>How far the shaft runs along Y, in data units.</summary>
    public double Dy { get; init; }

    /// <summary>The width of the shaft, in data units; absent for 0.001.</summary>
    public double? Width { get; init; }

    /// <summary>The width of the head, in data units; absent for three times the width of the shaft.</summary>
    public double? HeadWidth { get; init; }

    /// <summary>The length of the head, in data units; absent for one and a half times the width of the head.</summary>
    public double? HeadLength { get; init; }

    /// <summary>Whether the tip stands on the end point; absent for false.</summary>
    public bool? LengthIncludesHead { get; init; }

    /// <summary>Which part of the arrow is drawn; absent for the full arrow.</summary>
    public ArrowHeadShape? Shape { get; init; }

    /// <summary>How far the back of the head is swept towards the tip, as a fraction of its length; absent for 0.</summary>
    public double? Overhang { get; init; }

    /// <inheritdoc />
    internal override void AddTo(Axes axes) => axes.AddPatch(Fill(new Arrow(Start, Dx, Dy)
    {
        Width = Width ?? Rest.Width,
        HeadWidth = HeadWidth,
        HeadLength = HeadLength,
        LengthIncludesHead = LengthIncludesHead ?? Rest.LengthIncludesHead,
        Shape = Shape ?? Rest.Shape,
        Overhang = Overhang ?? Rest.Overhang,
    }));
}

/// <summary>Describes a <see cref="Circle"/>.</summary>
internal sealed record CircleDto : FilledPatchDto
{
    private static readonly Circle Rest = new(default, 0);

    /// <summary>The record a reader makes.</summary>
    public CircleDto() { }

    /// <summary>The record a writer makes.</summary>
    /// <param name="circle">The circle being written.</param>
    internal CircleDto(Circle circle) : base(circle, Rest)
    {
        Center = circle.Center;
        Radius = circle.Radius;
    }

    /// <summary>The centre, in data units.</summary>
    public DataPoint Center { get; init; }

    /// <summary>The radius, in data units.</summary>
    public double Radius { get; init; }

    /// <inheritdoc />
    internal override void AddTo(Axes axes) => axes.AddPatch(Fill(new Circle(Center, Radius)));
}

/// <summary>Describes an <see cref="Ellipse"/>.</summary>
internal sealed record EllipseDto : FilledPatchDto
{
    private static readonly Ellipse Rest = new(default, 0, 0);

    /// <summary>The record a reader makes.</summary>
    public EllipseDto() { }

    /// <summary>The record a writer makes.</summary>
    /// <param name="ellipse">The ellipse being written.</param>
    internal EllipseDto(Ellipse ellipse) : base(ellipse, Rest)
    {
        Center = ellipse.Center;
        Width = ellipse.Width;
        Height = ellipse.Height;
        Angle = ellipse.Angle.OrNullAtRest(Rest.Angle);
    }

    /// <summary>The centre, in data units.</summary>
    public DataPoint Center { get; init; }

    /// <summary>The full width along X before the ellipse is turned, in data units.</summary>
    public double Width { get; init; }

    /// <summary>The full height along Y before the ellipse is turned, in data units.</summary>
    public double Height { get; init; }

    /// <summary>The angle the ellipse is turned through about its centre, in degrees; absent for 0.</summary>
    public double? Angle { get; init; }

    /// <inheritdoc />
    internal override void AddTo(Axes axes) => axes.AddPatch(Fill(new Ellipse(Center, Width, Height) { Angle = Angle ?? Rest.Angle }));
}

/// <summary>Describes a <see cref="Wedge"/>. The width is absent for a wedge that reaches the centre, which is not the same as a
/// ring of width 0.</summary>
internal sealed record WedgeDto : FilledPatchDto
{
    private static readonly Wedge Rest = new(default, 0, 0, 0);

    /// <summary>The record a reader makes.</summary>
    public WedgeDto() { }

    /// <summary>The record a writer makes.</summary>
    /// <param name="wedge">The wedge being written.</param>
    internal WedgeDto(Wedge wedge) : base(wedge, Rest)
    {
        Center = wedge.Center;
        Radius = wedge.Radius;
        Theta1 = wedge.Theta1;
        Theta2 = wedge.Theta2;
        Width = wedge.Width;
    }

    /// <summary>The centre of the disc, in data units.</summary>
    public DataPoint Center { get; init; }

    /// <summary>The radius of the disc, in data units.</summary>
    public double Radius { get; init; }

    /// <summary>The angle the wedge starts at, in degrees counter-clockwise from +X.</summary>
    public double Theta1 { get; init; }

    /// <summary>The angle the wedge ends at, in degrees counter-clockwise from +X.</summary>
    public double Theta2 { get; init; }

    /// <summary>How far the wedge reaches in from its radius, in data units; absent for a wedge that reaches the centre.</summary>
    public double? Width { get; init; }

    /// <inheritdoc />
    internal override void AddTo(Axes axes) => axes.AddPatch(Fill(new Wedge(Center, Radius, Theta1, Theta2) { Width = Width }));
}

/// <summary>Describes an <see cref="Arc"/>, which has an edge and no face.</summary>
internal sealed record ArcDto : PatchDto
{
    private static readonly Arc Rest = new(default, 0, 0);

    /// <summary>The record a reader makes.</summary>
    public ArcDto() { }

    /// <summary>The record a writer makes.</summary>
    /// <param name="arc">The arc being written.</param>
    internal ArcDto(Arc arc) : base(arc, Rest)
    {
        Center = arc.Center;
        Width = arc.Width;
        Height = arc.Height;
        Angle = arc.Angle.OrNullAtRest(Rest.Angle);
        Theta1 = arc.Theta1.OrNullAtRest(Rest.Theta1);
        Theta2 = arc.Theta2.OrNullAtRest(Rest.Theta2);
    }

    /// <summary>The centre of the ellipse, in data units.</summary>
    public DataPoint Center { get; init; }

    /// <summary>The full width of the ellipse along X before it is turned, in data units.</summary>
    public double Width { get; init; }

    /// <summary>The full height of the ellipse along Y before it is turned, in data units.</summary>
    public double Height { get; init; }

    /// <summary>The angle the ellipse is turned through about its centre, in degrees; absent for 0.</summary>
    public double? Angle { get; init; }

    /// <summary>The direction the arc starts in, in degrees; absent for 0.</summary>
    public double? Theta1 { get; init; }

    /// <summary>The direction the arc ends in, in degrees; absent for 360.</summary>
    public double? Theta2 { get; init; }

    /// <inheritdoc />
    internal override void AddTo(Axes axes) => axes.AddPatch(Style(new Arc(Center, Width, Height)
    {
        Angle = Angle ?? Rest.Angle,
        Theta1 = Theta1 ?? Rest.Theta1,
        Theta2 = Theta2 ?? Rest.Theta2,
    }));
}
