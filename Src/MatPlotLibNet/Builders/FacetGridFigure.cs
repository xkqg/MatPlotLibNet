// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet;

/// <summary>
/// A faceted figure preset that renders one subplot per unique category value, arranged in a
/// column-wrapped grid. The caller supplies a <c>plotFunc</c> that receives the filtered X/Y
/// arrays for each category panel.
/// </summary>
/// <example>
/// <code>
/// var fb = new FacetGridFigure(x, y, category, (ax, fx, fy) => ax.Scatter(fx, fy)) { MaxCols = 2 }.Build();
/// fb.ToSvg();
/// </code>
/// </example>
public sealed class FacetGridFigure : FacetedFigure
{
    private readonly double[] _x;
    private readonly double[] _y;
    private readonly string[] _category;
    private readonly Action<AxesBuilder, double[], double[]> _plotFunc;

    /// <summary>
    /// Initializes a new facet grid with the given data and per-panel plot function.
    /// </summary>
    /// <param name="x">X values for all observations.</param>
    /// <param name="y">Y values for all observations.</param>
    /// <param name="category">Category label per observation — one panel per unique value.</param>
    /// <param name="plotFunc">Action invoked per panel with filtered X and Y arrays.</param>
    public FacetGridFigure(double[] x, double[] y, string[] category,
                           Action<AxesBuilder, double[], double[]> plotFunc)
    {
        _x        = x;
        _y        = y;
        _category = category;
        _plotFunc = plotFunc;
    }

    /// <summary>Maximum number of columns in the grid (default 3).</summary>
    public int MaxCols { get; init; } = 3;

    /// <summary>
    /// Optional hue labels per observation — stored as a forward-compatible hook.
    /// In v1.0 hue is not forwarded to <c>plotFunc</c> (signature is fixed at two arrays);
    /// richer overloads are deferred to v1.1.
    /// </summary>
    /// <remarks>Assigning <see cref="Hue"/> in v1.0 is a no-op for the plot function but silently
    /// accepted so existing code does not need to be updated when v1.1 hue support ships.</remarks>
    public string[]? Hue { get; init; }

    /// <inheritdoc/>
    protected override void BuildCore(FigureBuilder fb)
    {
        var categories = _category.Distinct().Order().ToArray();
        int numCats  = categories.Length;
        int numCols  = Math.Min(MaxCols, numCats);
        int numRows  = (numCats + numCols - 1) / numCols;

        fb.WithSize(300 * numCols, 250 * numRows)
          .WithGridSpec(numRows, numCols);

        for (int i = 0; i < numCats; i++)
        {
            int    idx = i;
            string cat = categories[idx];
            int    row = idx / numCols;
            int    col = idx % numCols;

            var filteredX = _x.Where((_, j) => _category[j] == cat).ToArray();
            var filteredY = _y.Where((_, j) => _category[j] == cat).ToArray();

            fb.AddSubPlot(GridPosition.Single(row, col), ax =>
            {
                _plotFunc(ax, filteredX, filteredY);
                ax.WithTitle(cat);
                ConfigurePanelDefaults(ax);
            });
        }
    }
}
