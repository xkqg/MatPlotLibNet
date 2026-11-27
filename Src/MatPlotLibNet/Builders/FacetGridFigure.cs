// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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

    /// <summary>How many columns the grid has at most when told no number: the default of <see cref="MaxCols"/> and of the
    /// <c>cols</c> argument of <c>FigureTemplates.FacetGrid</c>.</summary>
    internal const int RestingMaxCols = 3;

    /// <summary>Maximum number of columns in the grid (default <see cref="RestingMaxCols"/>).</summary>
    public int MaxCols { get; init; } = RestingMaxCols;

    /// <summary>
    /// Optional hue labels per observation. They are accepted and kept but not used: the
    /// <c>plotFunc</c> of each panel receives the filtered X and Y arrays only, so assigning
    /// <see cref="Hue"/> changes no panel.
    /// </summary>
    public string[]? Hue { get; init; }

    /// <inheritdoc/>
    protected override void BuildCore(FigureBuilder fb)
    {
        var categories = _category.Distinct().Order().ToArray();
        int numCats  = categories.Length;
        // Zero categories used to divide by zero at the grid arithmetic (measured 2026-08-30) — an empty
        // input is refused with the reason, where the caller can see it (matplotlib's FacetGrid raises too).
        if (numCats == 0)
        {
            throw new InvalidOperationException("A facet grid needs at least one category; the input carried none.");
        }
        var wrap = PanelWrap.Filled(numCats, MaxCols);

        fb.WithSize(300 * wrap.Columns, 250 * wrap.Rows)
          .WithGridSpec(wrap.Rows, wrap.Columns);

        for (int i = 0; i < numCats; i++)
        {
            string cat = categories[i];

            var filteredX = _x.Where((_, j) => _category[j] == cat).ToArray();
            var filteredY = _y.Where((_, j) => _category[j] == cat).ToArray();

            fb.AddSubPlot(wrap.At(i), ax =>
            {
                _plotFunc(ax, filteredX, filteredY);
                ax.WithTitle(cat);
                ConfigurePanelDefaults(ax);
            });
        }
    }
}
