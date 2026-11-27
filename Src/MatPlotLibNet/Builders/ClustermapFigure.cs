// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet;

/// <summary>The layout of <see cref="FigureTemplates.Clustermap"/>: the matrix's rows and columns clustered by Ward's method over
/// their Euclidean distances, each clustering made into the tree its leaves name, and the <see cref="ClustermapSeries"/> that draws
/// the matrix in the trees' order with their dendrograms beside it and the names right of and under it, on a 700-pixel square.</summary>
internal sealed class ClustermapFigure : FacetedFigure
{
    private readonly double[,] _data;
    private readonly string[]? _rowLabels, _colLabels;

    /// <summary>Makes the clustermap of <paramref name="data"/>.</summary>
    /// <param name="data">The matrix to cluster and draw.</param>
    /// <param name="rowLabels">One name for each row, in the order given; <see langword="null"/> names none.</param>
    /// <param name="colLabels">One name for each column, in the order given; <see langword="null"/> names none.</param>
    /// <exception cref="ArgumentException">A list of labels has not as many labels as the matrix has rows, or columns.</exception>
    internal ClustermapFigure(double[,] data, string[]? rowLabels, string[]? colLabels)
    {
        rowLabels.ThrowIfNotOnePer(data.GetLength(0), nameof(rowLabels));
        colLabels.ThrowIfNotOnePer(data.GetLength(1), nameof(colLabels));
        _data = data;
        _rowLabels = rowLabels;
        _colLabels = colLabels;
    }

    /// <inheritdoc/>
    protected override void BuildCore(FigureBuilder fb)
    {
        var rows = HierarchicalClustering.Cluster(_data.RowDistances()).ToTree(_rowLabels);
        var columns = HierarchicalClustering.Cluster(_data.ColumnDistances()).ToTree(_colLabels);

        fb.WithSize(700, 700)
          .AddSubPlot(1, 1, 1, ax => ax.Clustermap(_data, map =>
          {
              map.RowTree = rows;
              map.ColumnTree = columns;
          }));
    }
}
