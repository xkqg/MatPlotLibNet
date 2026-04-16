// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a 3D quiver (vector field) series with arrows at each (X, Y, Z) position
/// pointing in the (U, V, W) direction.</summary>
public sealed class Quiver3DSeries : ChartSeries, I3DPointSeries, IHasColor
{
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

    /// <summary>Scale factor applied to arrow direction vectors. Default 1.0.</summary>
    public double ArrowLength { get; set; } = 1.0;

    /// <summary>Arrow color. When <c>null</c> the theme's prop-cycler assigns one automatically.</summary>
    public Color? Color { get; set; }

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
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (X.Length == 0) return new(null, null, null, null);

        double xMin = double.MaxValue, xMax = double.MinValue;
        double yMin = double.MaxValue, yMax = double.MinValue;
        double zMin = double.MaxValue, zMax = double.MinValue;

        for (int i = 0; i < X.Length; i++)
        {
            double x0 = X[i], x1 = x0 + U[i] * ArrowLength;
            double y0 = Y[i], y1 = y0 + V[i] * ArrowLength;
            double z0 = Z[i], z1 = z0 + W[i] * ArrowLength;

            xMin = Math.Min(xMin, Math.Min(x0, x1));
            xMax = Math.Max(xMax, Math.Max(x0, x1));
            yMin = Math.Min(yMin, Math.Min(y0, y1));
            yMax = Math.Max(yMax, Math.Max(y0, y1));
            zMin = Math.Min(zMin, Math.Min(z0, z1));
            zMax = Math.Max(zMax, Math.Max(z0, z1));
        }

        return new(xMin, xMax, yMin, yMax, ZMin: zMin, ZMax: zMax);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "quiver3d",
        XData = X,
        YData = Y,
        ZData = Z,
        UData = U,
        VData = V,
        WData = W,
        ArrowLength = ArrowLength != 1.0 ? ArrowLength : null,
        Color = Color,
        Label = Label
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
