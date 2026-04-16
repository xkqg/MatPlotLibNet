// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>A single 3D text annotation positioned at (X, Y, Z).</summary>
public readonly record struct Text3DAnnotation(double X, double Y, double Z, string Text);

/// <summary>Represents a collection of 3D text annotations rendered at specified positions in three-dimensional space.</summary>
public sealed class Text3DSeries : ChartSeries, IHasColor
{
    /// <summary>The text annotations to render in 3D space.</summary>
    public List<Text3DAnnotation> Annotations { get; }

    /// <summary>Font size in points for the annotation text. Default 10.</summary>
    public double FontSize { get; set; } = 10;

    /// <summary>Text color for the annotations.</summary>
    public Color? Color { get; set; }

    /// <summary>Initializes a new 3D text series with the specified annotations.</summary>
    public Text3DSeries(List<Text3DAnnotation> annotations)
    {
        Annotations = annotations;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (Annotations.Count == 0) return new(null, null, null, null);

        double xMin = double.MaxValue, xMax = double.MinValue;
        double yMin = double.MaxValue, yMax = double.MinValue;
        double zMin = double.MaxValue, zMax = double.MinValue;

        foreach (var a in Annotations)
        {
            if (a.X < xMin) xMin = a.X;
            if (a.X > xMax) xMax = a.X;
            if (a.Y < yMin) yMin = a.Y;
            if (a.Y > yMax) yMax = a.Y;
            if (a.Z < zMin) zMin = a.Z;
            if (a.Z > zMax) zMax = a.Z;
        }

        return new(xMin, xMax, yMin, yMax, ZMin: zMin, ZMax: zMax);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "text3d",
        Text3DAnnotations = Annotations.Select(a => new Text3DAnnotationDto(a.X, a.Y, a.Z, a.Text)).ToList(),
        MarkerSize = FontSize != 10 ? FontSize : null,
        Color = Color,
        Label = Label
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
