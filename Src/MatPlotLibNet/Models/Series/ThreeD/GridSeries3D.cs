// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Base class for 3D grid series with X[], Y[], Z[,] data.</summary>
public abstract class GridSeries3D : ChartSeries, I3DGridSeries
{
    /// <inheritdoc />
    internal override CoordinateSystem? DrawnIn => CoordinateSystem.ThreeD;

    /// <inheritdoc />
    public double[] X { get; }

    /// <inheritdoc />
    public double[] Y { get; }

    /// <inheritdoc />
    public double[,] Z { get; }

    /// <summary>Initializes with X, Y, and Z data.</summary>
    protected GridSeries3D(double[] x, double[] y, double[,] z) { X = x; Y = y; Z = z; }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) => default;

    /// <inheritdoc />
    /// <remarks>Long form over the surface grid: the x and y a vertex sits at, and its height.</remarks>
    public override ChartDataTable? ToDataTable() => ChartDataTable.OverGrid(X, Y, new GridValues(Label ?? "z", Z));
}
