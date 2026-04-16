// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a triangulated surface from unstructured (x, y, z) point data.</summary>
public sealed class Trisurf3DSeries : XYZSeries, IColormappable, INormalizable, IHasAlpha, IHasEdgeColor, IHasColor
{
    public IColorMap? ColorMap { get; set; }

    public double Alpha { get; set; } = 0.8;

    public bool ShowWireframe { get; set; } = true;

    public Color? EdgeColor { get; set; }

    public INormalizer? Normalizer { get; set; }

    /// <summary>Fallback solid color when no colormap is assigned.</summary>
    public Color? Color { get; set; }

    /// <summary>Initializes a new triangulated surface series with the specified data.</summary>
    public Trisurf3DSeries(Vec x, Vec y, Vec z) : base(x, y, z) { }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "trisurf",
        XData = X,
        YData = Y,
        ZData = Z,
        Color = Color,
        ShowWireframe = ShowWireframe ? null : (bool?)false,
        Alpha = Alpha != 0.8 ? Alpha : null,
        Label = Label
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
