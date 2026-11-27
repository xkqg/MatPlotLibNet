// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>
/// The faceted figure of a pair plot: the <see cref="PairGridSeries"/> of its columns on one axes, 200 pixels a variable square.
/// The diagonal shows a histogram of each variable and the cells off it a scatter of each pair; a hue label per observation splits
/// the grid by hue, and the column names name the variables under and beside it.
/// </summary>
/// <example>
/// <code>
/// var fb = new PairPlotFigure(columns) { ColumnNames = ["X","Y","Z"], Hue = groups }.Build();
/// fb.ToSvg();
/// </code>
/// </example>
public sealed class PairPlotFigure : FacetedFigure
{
    private readonly double[][] _columns;

    /// <summary>Initializes a new pair plot with the given data columns.</summary>
    public PairPlotFigure(double[][] columns) { _columns = columns; }

    /// <summary>Optional variable names, drawn under the grid and beside it (<see cref="PairGridSeries.Labels"/>).</summary>
    public string[]? ColumnNames { get; init; }

    /// <summary>How many bins the diagonal histograms have when told no number: the default of <see cref="Bins"/> and of the
    /// <c>bins</c> argument of <c>FigureTemplates.PairPlot</c>.</summary>
    internal const int RestingBins = PairGridSeries.RestingDiagonalBins;

    /// <summary>Number of histogram bins for diagonal panels (default <see cref="RestingBins"/>).</summary>
    public int Bins { get; init; } = RestingBins;

    /// <summary>
    /// Optional hue label per observation. When set, the grid is split by hue: one group per label, in the order first seen, named
    /// by the label and coloured from <see cref="FacetedFigure.Palette"/> (the theme's cycle when none is given).
    /// </summary>
    public string[]? Hue { get; init; }

    /// <inheritdoc/>
    protected override void BuildCore(FigureBuilder fb)
    {
        int n = _columns.Length;
        if (n == 0) return;

        var hue = Hue is { } labels ? HueGrouper.Codes(labels) : (HueCodes?)null;
        fb.WithSize(200 * n, 200 * n)
          .AddSubPlot(1, 1, 1, ax => ax.PairGrid(_columns, grid =>
          {
              grid.Labels = ColumnNames;
              grid.DiagonalBins = Bins;
              grid.HuePalette = Palette;
              grid.HueGroups = hue?.Groups;
              grid.HueLabels = hue?.Labels;
          }));
    }
}
