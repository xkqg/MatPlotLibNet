// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>A single 3D text annotation positioned at (X, Y, Z).</summary>
public readonly record struct Text3DAnnotation(double X, double Y, double Z, string Text);

/// <summary>Represents a collection of 3D text annotations rendered at specified positions in three-dimensional space.</summary>
public sealed class Text3DSeries : ChartSeries, IHasColor, ISeriesKind<Text3DSeries>, IGivenTextSize
{
    /// <inheritdoc />
    internal override CoordinateSystem? DrawnIn => CoordinateSystem.ThreeD;

    /// <inheritdoc />
    /// <remarks><c>Axes3D.text</c> asks neither cycle.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Mesh;

    /// <summary>The text annotations to render in 3D space.</summary>
    public List<Text3DAnnotation> Annotations { get; }

    /// <summary>Font size for the annotation text, in pixels (the unit of <see cref="Font.Size"/>). Default <see cref="RestingFontSize"/>. Given no size, the
    /// text is the size of the theme's text under the matplotlib themes, as matplotlib's <c>Axes3D.text</c> takes <c>font.size</c>
    /// (<c>TextLook.SeriesTextAtFontSize</c>), and <see cref="RestingFontSize"/> under every other theme; a size that is given, the default included, is drawn as
    /// given.</summary>
    public double FontSize { get => GivenFontSize ?? RestingFontSize; set => GivenFontSize = value; }

    /// <summary>The text size a new series holds under a theme that names none.</summary>
    private const double RestingFontSize = 10;

    /// <summary>The text size that was given, <see langword="null"/> when none was.</summary>
    internal double? GivenFontSize { get; private set => Change(ref field, value); }

    double? IGivenTextSize.GivenTextSize => GivenFontSize;

    double IGivenTextSize.RestingTextSize => RestingFontSize;

    /// <summary>Text color for the annotations.</summary>
    public Color? Color { get; set => Change(ref field, value); }

    /// <summary>Initializes a new 3D text series with the specified annotations.</summary>
    public Text3DSeries(List<Text3DAnnotation> annotations)
    {
        Annotations = annotations;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (Annotations.Count == 0) return default;

        // Over the drawable coordinates only: a missing one (NaN) and an infinity are left out.
        return DataRangeContribution.Spanning(Annotations.Select(a => a.X).ToArray().DrawableRange(),
            Annotations.Select(a => a.Y).ToArray().DrawableRange(), Annotations.Select(a => a.Z).ToArray().DrawableRange());
    }

    static string ISeriesKind<Text3DSeries>.Kind => "text3d";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        Text3DAnnotations = Annotations.Select(a => new Text3DAnnotationDto(a.X, a.Y, a.Z, a.Text)).ToList(),
        MarkerSize = GivenFontSize,
        Color = Color,
        Label = Label
    };

    /// <summary>The reader of a <see cref="Text3DSeries"/> that <see cref="SeriesRegistry"/> takes; it also switches the axes to a 3D coordinate system.</summary>
    internal static Text3DSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var annotations = dto.Text3DAnnotations?
            .Select(a => new Text3DAnnotation(a.X, a.Y, a.Z, a.Text))
            .ToList() ?? [];
        var s = new Text3DSeries(annotations);
        if (dto.MarkerSize is { } markerSize) s.FontSize = markerSize;
        if (dto.Color is { } color) s.Color = color;
        axes.AddSeries(s);
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
