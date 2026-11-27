// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Base class for series rendered in polar coordinates (r, theta).</summary>
public abstract class PolarSeries : ChartSeries, IPolarSeries
{
    /// <inheritdoc />
    internal override CoordinateSystem? DrawnIn => CoordinateSystem.Polar;

    /// <inheritdoc />
    public double[] R { get; }

    /// <inheritdoc />
    public double[] Theta { get; }

    /// <summary>Initializes with R and Theta data arrays.</summary>
    protected PolarSeries(double[] r, double[] theta) { R = r; Theta = theta; }

    /// <inheritdoc />
    /// <remarks>Angle first, then the radius under the series' own label — the order the reader thinks in.</remarks>
    public override ChartDataTable? ToDataTable() =>
        ChartDataTable.FromNumberColumns([new("theta"), new(Label ?? "r")], [Theta, R]);

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) => default;

    /// <inheritdoc />
    /// <remarks>The smallest and the largest radius that can be placed, below zero too (a scale that reaches them draws them, as
    /// matplotlib's polar axes does); a missing or infinite radius is left out, as it is not drawn. None when no radius can be
    /// placed.</remarks>
    internal override RadialExtent? RadialExtent => R.DrawableRange() is { } reach ? new RadialExtent(reach.Lo, reach.Hi, Sticky: false) : null;
}
