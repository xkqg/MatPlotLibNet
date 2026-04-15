// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a 3D bar chart series — rectangular prisms rising from the XY-plane.</summary>
public sealed class Bar3DSeries : XYZSeries, IHasColor
{
    public double BarWidth { get; set; } = 0.5;

    public Color? Color { get; set; }

    /// <summary>Initializes a new instance of <see cref="Bar3DSeries"/>.</summary>
    public Bar3DSeries(Vec x, Vec y, Vec z) : base(x, y, z) { }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (X.Length == 0) return new(null, null, null, null);
        // matplotlib bar3d convention: X[i] / Y[i] are the LEFT-FRONT corner of the bar,
        // not the centre. Bar spans [X, X+BarWidth] × [Y, Y+BarWidth] × [0, Z]. This aligns
        // X/Y tick labels with bar edges instead of centres.
        return new Box3D(
            X: new(X.Min(), X.Max() + BarWidth),
            Y: new(Y.Min(), Y.Max() + BarWidth),
            Z: new(Math.Min(0, Z.Min()), Math.Max(0, Z.Max()))
        ).ToContribution(stickyZMin: 0);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "bar3d",
        XData = X,
        YData = Y,
        ZData = Z,
        BarWidth = BarWidth,
        Color = Color
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
