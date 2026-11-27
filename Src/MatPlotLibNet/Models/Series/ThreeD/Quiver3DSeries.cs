// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a 3D quiver (vector field) series with arrows at each (X, Y, Z) position
/// pointing in the (U, V, W) direction.</summary>
public sealed class Quiver3DSeries : ChartSeries, I3DPointSeries, IHasColor, ISeriesKind<Quiver3DSeries>
{
    /// <inheritdoc />
    internal override CoordinateSystem? DrawnIn => CoordinateSystem.ThreeD;

    /// <inheritdoc />
    /// <remarks><c>Axes3D.quiver</c> asks neither cycle.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Mesh;

    /// <summary>X coordinates of arrow origins.</summary>
    public Vec X { get; }

    /// <summary>Y coordinates of arrow origins.</summary>
    public Vec Y { get; }

    /// <summary>Z coordinates of arrow origins.</summary>
    public Vec Z { get; }

    /// <summary>X components of the arrow direction vectors.</summary>
    public Vec U { get; }

    /// <summary>Y components of the arrow direction vectors.</summary>
    public Vec V { get; }

    /// <summary>Z components of the arrow direction vectors.</summary>
    public Vec W { get; }

    private const double RestingArrowLength = 1.0;
    /// <summary>Scale factor applied to arrow direction vectors. Default <see cref="RestingArrowLength"/>.</summary>
    public double ArrowLength { get; set => Change(ref field, value); } = RestingArrowLength;

    /// <summary>Arrow color. When <c>null</c> the theme's prop-cycler assigns one automatically.</summary>
    public Color? Color { get; set => Change(ref field, value); }

    // Explicit I3DPointSeries dispatch
    double[] I3DPointSeries.X => X;
    double[] I3DPointSeries.Y => Y;
    double[] I3DPointSeries.Z => Z;

    /// <summary>Initializes a new 3D quiver series with position and direction data.</summary>
    public Quiver3DSeries(Vec x, Vec y, Vec z, Vec u, Vec v, Vec w)
    {
        X = x; Y = y; Z = z;
        U = u; V = v; W = w;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        DrawableExtent() is { } box
            ? new(box.X.Lo, box.X.Hi, box.Y.Lo, box.Y.Hi, ZMin: box.Z.Lo, ZMax: box.Z.Hi)
            : default;

    /// <summary>The extent of the arrows that can be drawn, tail and head: an arrow with a coordinate that is missing
    /// (NaN) or infinite is not drawn, so it is left out on every axis. The range of the series and the projection its
    /// renderer fits on 2-D axes both come from here.</summary>
    /// <returns>The extent, or <see langword="null"/> when no arrow can be drawn.</returns>
    internal Box3D? DrawableExtent()
    {
        var x = Range1D.Empty;
        var y = Range1D.Empty;
        var z = Range1D.Empty;
        for (int i = 0; i < X.Length; i++)
        {
            double x0 = X[i], x1 = x0 + U[i] * ArrowLength;
            double y0 = Y[i], y1 = y0 + V[i] * ArrowLength;
            double z0 = Z[i], z1 = z0 + W[i] * ArrowLength;
            if (!(new Point(x0, y0).CanDrawLineTo(new Point(x1, y1)) && z0.IsDrawable() && z1.IsDrawable())) continue;

            x = x.Including(x0).Including(x1);
            y = y.Including(y0).Including(y1);
            z = z.Including(z0).Including(z1);
        }

        return x.IsEmpty ? null : new Box3D(x, y, z);
    }

    static string ISeriesKind<Quiver3DSeries>.Kind => "quiver3d";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        XData = X,
        YData = Y,
        ZData = Z,
        UData = U,
        VData = V,
        WData = W,
        ArrowLength = ArrowLength.OrNullAtRest(RestingArrowLength),
        Color = Color,
        Label = Label
    };

    /// <summary>The reader of a <see cref="Quiver3DSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static Quiver3DSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Quiver3D(dto.XData ?? [], dto.YData ?? [], dto.ZData ?? [],
            dto.UData ?? [], dto.VData ?? [], dto.WData ?? []);
        if (dto.ArrowLength is { } arrowLength) s.ArrowLength = arrowLength;
        if (dto.Color is { } color) s.Color = color;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>Where each arrow sits in the scene, and the vector it carries.</remarks>
    public override ChartDataTable? ToDataTable() =>
        ChartDataTable.XYZ(X.Data, Y.Data, Z.Data, "z", new NumberColumn("u", U.Data), new NumberColumn("v", V.Data), new NumberColumn("w", W.Data));

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
