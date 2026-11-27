// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet;

/// <summary>
/// Abstract base for faceted figure presets. Subclasses implement <see cref="BuildCore"/> to add
/// sub-plots; shared helpers (<see cref="AddScatters"/>, <see cref="AddLines"/>,
/// <see cref="AddHistograms"/>) handle hue-grouping via <see cref="HueGrouper"/>.
/// </summary>
/// <example>
/// One panel per category, scatter per panel, max 2 columns:
/// <code>
/// string svg = new FacetGridFigure(x, y, category,
///         (ax, fx, fy) => ax.Scatter(fx, fy)) { MaxCols = 2 }
///     .Build().ToSvg();
/// </code>
/// Joint distribution with hue grouping:
/// <code>
/// string svg = new JointPlotFigure(x, y) { Title = "Age vs Income", Hue = hueLabels }
///     .Build().ToSvg();
/// </code>
/// N×N pair plot with custom column names:
/// <code>
/// string svg = new PairPlotFigure(columns)
///     { ColumnNames = ["Age", "Income", "Score"], Hue = groupLabels }
///     .Build().ToSvg();
/// </code>
/// </example>
public abstract class FacetedFigure
{
    /// <summary>Optional figure title.</summary>
    public string? Title { get; init; }

    /// <summary>Color palette to use for hue groups. Defaults to Tab10 when <see langword="null"/>.</summary>
    public Color[]? Palette { get; init; }

    /// <summary>Figure width in pixels. Given together with <see cref="Height"/>, it wins over the size the figure's layout picks
    /// for itself.</summary>
    public double? Width { get; init; }

    /// <summary>Figure height in pixels. Given together with <see cref="Width"/>, it wins over the size the figure's layout picks
    /// for itself.</summary>
    public double? Height { get; init; }

    /// <summary>Builds the <see cref="FigureBuilder"/> shell and delegates panel construction to <see cref="BuildCore"/>; a size
    /// the caller gave is set after it, so it wins over the one the layout picks.</summary>
    public FigureBuilder Build()
    {
        var fb = Plt.Create();
        if (Title is not null) fb.WithTitle(Title);
        BuildCore(fb);
        if (Width is { } w && Height is { } h) fb.WithSize(w, h);
        return fb;
    }

    /// <summary>Adds sub-plots to the supplied <paramref name="fb"/>. Called by <see cref="Build"/>.</summary>
    protected abstract void BuildCore(FigureBuilder fb);

    /// <summary>Hides top and right spines — shared panel aesthetic for all faceted figures.</summary>
    protected static void ConfigurePanelDefaults(AxesBuilder ax)
    {
        ax.OpenFrame();
    }

    /// <summary>
    /// Adds scatter series to <paramref name="ax"/>. When <paramref name="hueLabels"/> is
    /// provided, one series per unique hue value is added (colors from <see cref="Palette"/>).
    /// </summary>
    /// <param name="ax">The target axes panel.</param>
    /// <param name="x">X data values, one per observation.</param>
    /// <param name="y">Y data values, one per observation.</param>
    /// <param name="hueLabels">Per-observation group labels; <see langword="null"/> adds a single un-grouped series.</param>
    protected void AddScatters(AxesBuilder ax, double[] x, double[] y, string[]? hueLabels) =>
        new HueSplit(x, y, hueLabels, Palette).Draw<ScatterSeries>((xs, ys, dress) => ax.Scatter(xs, ys, dress));

    /// <summary>
    /// Adds line series to <paramref name="ax"/>. When <paramref name="hueLabels"/> is
    /// provided, one series per unique hue value is added (colors from <see cref="Palette"/>).
    /// </summary>
    /// <param name="ax">The target axes panel.</param>
    /// <param name="x">X data values, one per observation.</param>
    /// <param name="y">Y data values, one per observation.</param>
    /// <param name="hueLabels">Per-observation group labels; <see langword="null"/> adds a single un-grouped series.</param>
    protected void AddLines(AxesBuilder ax, double[] x, double[] y, string[]? hueLabels) =>
        new HueSplit(x, y, hueLabels, Palette).Draw<LineSeries>((xs, ys, dress) => ax.Plot(xs, ys, dress));

    /// <summary>
    /// Adds histogram series to <paramref name="ax"/>. When <paramref name="hueLabels"/> is
    /// provided, one overlapping histogram per unique hue value is added at alpha 0.7.
    /// </summary>
    /// <param name="ax">The target axes panel.</param>
    /// <param name="values">Numeric values to bin, one per observation.</param>
    /// <param name="bins">Number of histogram bins.</param>
    /// <param name="hueLabels">Per-observation group labels; <see langword="null"/> adds a single un-grouped histogram.</param>
    protected void AddHistograms(AxesBuilder ax, double[] values, int bins, string[]? hueLabels) =>
        new HueSplit(values, values, hueLabels, Palette).DrawOverlapping((vs, dress) => ax.Hist(vs, bins, dress));
}
