// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>Pre-built figure layouts for common use cases. Each is a factory: it makes the figure of its layout and builds it, so a
/// layout is written once, in its figure.</summary>
/// <remarks>Each method returns a <see cref="FigureBuilder"/> that can be further customized before calling
/// <c>Build()</c>, <c>ToSvg()</c>, or <c>Save(path)</c>.</remarks>
public static class FigureTemplates
{
    /// <summary>Creates a 3-panel financial dashboard: price (60%), volume (15%), oscillator (25%) with shared X axis.</summary>
    /// <param name="open">Open prices.</param>
    /// <param name="high">High prices.</param>
    /// <param name="low">Low prices.</param>
    /// <param name="close">Close prices.</param>
    /// <param name="volume">Volume data.</param>
    /// <param name="title">Optional figure title.</param>
    /// <param name="configurePricePanel">Optional customization for the price axes.</param>
    /// <param name="configureVolumePanel">Optional customization for the volume axes.</param>
    /// <param name="configureOscillatorPanel">Optional customization for the oscillator axes.</param>
    public static FigureBuilder FinancialDashboard(
        double[] open, double[] high, double[] low, double[] close, double[] volume,
        string? title = null,
        Action<AxesBuilder>? configurePricePanel = null,
        Action<AxesBuilder>? configureVolumePanel = null,
        Action<AxesBuilder>? configureOscillatorPanel = null) =>
        new FinancialDashboardFigure(open, high, low, close, volume)
        {
            Title = title,
            ConfigurePrice = configurePricePanel,
            ConfigureVolume = configureVolumePanel,
            ConfigureOscillator = configureOscillatorPanel,
        }.Build();

    /// <summary>Creates a clean single-panel scientific-paper figure:
    /// tight layout, hidden top/right spines, caller-supplied data via <paramref name="configure"/>. It sets the figure
    /// to 150 dpi, so a PNG of it is drawn with 150/96 of the pixels of <paramref name="width"/> by
    /// <paramref name="height"/> (1250 by 938 for the default size); an SVG and a PDF are the size given.</summary>
    /// <param name="configure">Callback that receives the subplot's <see cref="AxesBuilder"/> for plotting data, setting labels, etc.</param>
    /// <param name="title">Optional figure title.</param>
    /// <param name="width">Figure width in pixels (default <see cref="Figure.RestingWidth"/>).</param>
    /// <param name="height">Figure height in pixels (default <see cref="Figure.RestingHeight"/>).</param>
    public static FigureBuilder ScientificPaper(
        Action<AxesBuilder> configure,
        string? title = null,
        double width = Figure.RestingWidth, double height = Figure.RestingHeight) =>
        new ScientificPaperFigure(1, 1, [configure]) { Title = title, Width = width, Height = height }.Build();

    /// <summary>Creates a multi-panel scientific-paper figure: one configure callback per subplot (row-major). Like the
    /// single-panel figure it sets 150 dpi, so a PNG of it has 150/96 of the pixels of <paramref name="width"/> by
    /// <paramref name="height"/>.</summary>
    /// <param name="rows">Number of subplot rows.</param>
    /// <param name="cols">Number of subplot columns.</param>
    /// <param name="configures">One callback per subplot, in row-major order (length must equal <c>rows*cols</c>).</param>
    /// <param name="title">Optional figure title.</param>
    /// <param name="width">Figure width in pixels (default <see cref="Figure.RestingWidth"/>).</param>
    /// <param name="height">Figure height in pixels (default <see cref="Figure.RestingHeight"/>).</param>
    public static FigureBuilder ScientificPaper(
        int rows, int cols,
        Action<AxesBuilder>[] configures,
        string? title = null,
        double width = Figure.RestingWidth, double height = Figure.RestingHeight) =>
        new ScientificPaperFigure(rows, cols, configures) { Title = title, Width = width, Height = height }.Build();

    /// <summary>
    /// Creates a joint distribution plot: center scatter with marginal histograms on the top and right edges.
    /// Layout: 2×2 GridSpec — top-left = X marginal, bottom-left = scatter, bottom-right = Y marginal.
    /// </summary>
    /// <param name="x">X data values.</param>
    /// <param name="y">Y data values.</param>
    /// <param name="title">Optional figure title.</param>
    /// <param name="bins">Number of histogram bins for the marginal distributions (default <see cref="JointPlotFigure.RestingBins"/>).</param>
    public static FigureBuilder JointPlot(double[] x, double[] y, string? title = null, int bins = JointPlotFigure.RestingBins) =>
        new JointPlotFigure(x, y) { Title = title, Bins = bins }.Build();

    /// <summary>Creates a vertically stacked sparkline dashboard with one row per named sample: the sample's values drawn
    /// as a sparkline, its label on the row's Y axis, the rows in the order given from top to bottom. The figure is 600
    /// pixels wide and 120 pixels tall for each row.</summary>
    /// <example>
    /// <code>
    /// NamedSample[] metrics =
    /// [
    ///     new("CPU %",    [12, 35, 28, 61, 44]),
    ///     new("Memory %", [48, 49, 52, 51, 55]),
    /// ];
    /// string svg = FigureTemplates.SparklineDashboard(metrics, title: "Server Metrics").ToSvg();
    /// </code>
    /// </example>
    /// <param name="series">One <see cref="NamedSample"/> per row. Each becomes one subplot row.</param>
    /// <param name="title">Optional figure title.</param>
    /// <exception cref="ArgumentNullException"><paramref name="series"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="series"/> holds no row, or a row carries no values (its array is
    /// <see langword="null"/>, as in <c>default(NamedSample)</c>); the message names the row.</exception>
    public static FigureBuilder SparklineDashboard(
        IReadOnlyList<NamedSample> series,
        string? title = null) =>
        new SparklineDashboardFigure(series) { Title = title }.Build();

    /// <summary>Creates an N×N pair plot: diagonal panels show histograms, off-diagonal panels show scatter plots.</summary>
    /// <param name="columns">Array of data columns (each is one variable).</param>
    /// <param name="columnNames">Optional variable names for axis labels.</param>
    /// <param name="bins">Number of histogram bins for diagonal panels (default <see cref="PairPlotFigure.RestingBins"/>).</param>
    public static FigureBuilder PairPlot(double[][] columns, string[]? columnNames = null, int bins = PairPlotFigure.RestingBins) =>
        new PairPlotFigure(columns) { ColumnNames = columnNames, Bins = bins }.Build();

    /// <summary>Creates a facet grid: one subplot per unique category, grouped into columns.</summary>
    /// <param name="x">X values for all observations.</param>
    /// <param name="y">Y values for all observations.</param>
    /// <param name="category">Category label for each observation.</param>
    /// <param name="plotFunc">Action that adds the series to each subplot, receiving filtered X and Y for the category.</param>
    /// <param name="cols">Maximum number of columns in the grid (default <see cref="FacetGridFigure.RestingMaxCols"/>).</param>
    public static FigureBuilder FacetGrid(double[] x, double[] y, string[] category,
        Action<AxesBuilder, double[], double[]> plotFunc, int cols = FacetGridFigure.RestingMaxCols) =>
        new FacetGridFigure(x, y, category, plotFunc) { MaxCols = cols }.Build();

    /// <summary>Creates a clustermap: the <see cref="ClustermapSeries"/> of <paramref name="data"/>, its rows and its columns each
    /// clustered by Ward's method over their Euclidean distances, drawn in the clustering's order with a dendrogram of each beside the
    /// matrix.</summary>
    /// <param name="data">The N×M data matrix to cluster and display.</param>
    /// <param name="rowLabels">Optional row labels, one for each row of <paramref name="data"/> in the order it was given; the map
    /// reorders its rows by the clustering and draws each label right of the row it names.</param>
    /// <param name="colLabels">Optional column labels, one for each column of <paramref name="data"/> in the order it was given;
    /// each is drawn under the column it names, wherever the clustering put it.</param>
    /// <exception cref="ArgumentException">A list of labels has not as many labels as the matrix has rows, or columns.</exception>
    public static FigureBuilder Clustermap(double[,] data, string[]? rowLabels = null, string[]? colLabels = null) =>
        new ClustermapFigure(data, rowLabels, colLabels).Build();
}
