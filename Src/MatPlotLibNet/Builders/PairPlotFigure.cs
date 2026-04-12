// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet;

/// <summary>
/// A faceted figure preset that renders an N×N pair plot: diagonal panels show histograms;
/// off-diagonal panels show scatter plots. Supports optional hue grouping on off-diagonal panels.
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

    /// <summary>Optional variable names shown as axis labels on the grid edges.</summary>
    public string[]? ColumnNames { get; init; }

    /// <summary>Number of histogram bins for diagonal panels (default 20).</summary>
    public int Bins { get; init; } = 20;

    /// <summary>
    /// Optional hue label per observation. When set, off-diagonal scatter panels are rendered
    /// with one series per unique hue value.
    /// </summary>
    public string[]? Hue { get; init; }

    /// <inheritdoc/>
    protected override void BuildCore(FigureBuilder fb)
    {
        int n = _columns.Length;
        if (n == 0) return;

        fb.WithSize(200 * n, 200 * n)
          .WithGridSpec(n, n);

        for (int row = 0; row < n; row++)
        for (int col = 0; col < n; col++)
        {
            int r = row, c = col;
            fb.AddSubPlot(GridPosition.Single(r, c), ax =>
            {
                if (r == c)
                {
                    AddHistograms(ax, _columns[r], Bins, null);
                }
                else
                {
                    AddScatters(ax, _columns[c], _columns[r], Hue);
                }

                if (ColumnNames is not null)
                {
                    if (c == 0) ax.SetYLabel(ColumnNames[r]);
                    if (r == n - 1) ax.SetXLabel(ColumnNames[c]);
                }

                ConfigurePanelDefaults(ax);
            });
        }
    }
}
