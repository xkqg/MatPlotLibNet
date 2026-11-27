// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a 3D wireframe plot rendered as grid lines on a surface.</summary>
public sealed class WireframeSeries : GridSeries3D, IHasColor, ISeriesKind<WireframeSeries>, IWidthGiven
{
    /// <inheritdoc />
    /// <remarks><c>plot_wireframe</c> draws in <c>lines.color</c> and asks neither cycle.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Mesh;

    /// <inheritdoc />
    public Color? Color { get; set => Change(ref field, value); }

    /// <summary>The thickness of the grid lines, in pixels, read and drawn as a line's (<see cref="LineSeries.LineWidth"/>) but resting at
    /// the library's half-pixel width of a collection.</summary>
    public double LineWidth { get => this.ReportedLineWidth; set => GivenLineWidth = value; }

    /// <summary>How wide the grid lines are drawn: as a collection of lines (<c>plot_wireframe</c>'s <c>Line3DCollection</c> takes
    /// <c>lines.linewidth</c> and never the cycler's).</summary>
    LineWidthRule IWidthGiven.WidthRule => LineWidthRule.HalfPixelCollection;

    /// <summary>The width the series was given; <see langword="null"/> when it was given none, and its rule decides.</summary>
    internal double? GivenLineWidth { get; private set => Change(ref field, value); }

    double? IWidthGiven.GivenLineWidth => GivenLineWidth;

    /// <summary>Initializes a new wireframe series with the specified grid data.</summary>
    public WireframeSeries(double[] x, double[] y, double[,] z) : base(x, y, z) { }

    static string ISeriesKind<WireframeSeries>.Kind => "wireframe";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        XData = X,
        YData = Y,
        ZGridData = Z.To2DList(),
        Color = Color,
        LineWidth = GivenLineWidth,
        Label = Label
    };

    /// <summary>The reader of a <see cref="WireframeSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static WireframeSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var z = dto.ZGridData.From2DList();
        var s = axes.Wireframe(dto.XData ?? [0.0, 1.0], dto.YData ?? [0.0, 1.0],
            z.GetLength(0) > 0 ? z : new double[,] { { 0, 0 }, { 0, 0 } });
        if (dto.Color is { } color) s.Color = color;
        if (dto.LineWidth is { } lineWidth) s.LineWidth = lineWidth;
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
