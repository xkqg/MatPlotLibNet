// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet;



/// <summary>Pre-built figure layouts for common use cases.</summary>
/// <remarks>Each method returns a <see cref="FigureBuilder"/> that can be further customized before calling
/// <c>Build()</c>, <c>ToSvg()</c>, or <c>Save()</c>.</remarks>
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
        Action<AxesBuilder>? configureOscillatorPanel = null)
    {
        int n = close.Length;
        var labels = new string[n];
        for (int i = 0; i < n; i++) labels[i] = i.ToString();

        var builder = Plt.Create()
            .WithGridSpec(3, 1, heightRatios: [0.60, 0.15, 0.25])
            .TightLayout();   // gives each panel its own tick + label gutters

        if (title is not null) builder.WithTitle(title);

        // Tick labels centered inside each bar slot: ticks at k*step+0.5, formatted as integer bar index.
        // Step is chosen to yield ~10 visible ticks regardless of n.
        double tickStep = Math.Max(1, Math.Round((double)n / 10));
        var barLocator  = new MultipleLocator(tickStep, 0.5);
        var barFormatter = new BarCenterFormatter();

        builder.AddSubPlot(new GridPosition(0, 1, 0, 1), ax =>
        {
            ax.Candlestick(open, high, low, close, labels);
            ax.SetYLabel("Price");
            ax.SetXMargin(0);
            ax.SetXTickLocator(barLocator);
            ax.SetXTickFormatter(barFormatter);
            configurePricePanel?.Invoke(ax);
        });

        builder.AddSubPlot(new GridPosition(1, 2, 0, 1), ax =>
        {
            var volLabels = new string[n];
            for (int i = 0; i < n; i++) volLabels[i] = i.ToString();
            ax.Bar(volLabels, volume);
            ax.SetYLabel("Volume");
            ax.SetXMargin(0);
            ax.SetXTickLocator(barLocator);
            ax.SetXTickFormatter(barFormatter);
            configureVolumePanel?.Invoke(ax);
        });

        builder.AddSubPlot(new GridPosition(2, 3, 0, 1), ax =>
        {
            ax.SetYLabel("Oscillator");
            ax.SetXMargin(0);
            ax.SetXLim(0, n);  // align x range with price/volume panels
            ax.SetXTickLocator(barLocator);
            ax.SetXTickFormatter(barFormatter);
            ax.UseBarSlotX();  // panel indicators (RSI, etc.) will auto-align to bar centres
            configureOscillatorPanel?.Invoke(ax);
        });

        return builder;
    }

    /// <summary>Creates a clean single-panel scientific-paper figure:
    /// 150 DPI, tight layout, hidden top/right spines, caller-supplied data via <paramref name="configure"/>.</summary>
    /// <param name="configure">Callback that receives the subplot's <see cref="AxesBuilder"/> for plotting data, setting labels, etc.</param>
    /// <param name="title">Optional figure title.</param>
    /// <param name="width">Figure width in pixels (default 800).</param>
    /// <param name="height">Figure height in pixels (default 600).</param>
    public static FigureBuilder ScientificPaper(
        Action<AxesBuilder> configure,
        string? title = null,
        double width = 800, double height = 600)
    {
        var builder = Plt.Create()
            .WithSize(width, height)
            .WithDpi(150)
            .TightLayout();

        if (title is not null) builder.WithTitle(title);

        builder.AddSubPlot(1, 1, 1, ax =>
        {
            ax.HideTopSpine();
            ax.HideRightSpine();
            configure(ax);
        });

        return builder;
    }

    /// <summary>Creates a multi-panel scientific-paper figure: one configure callback per subplot (row-major).</summary>
    /// <param name="rows">Number of subplot rows.</param>
    /// <param name="cols">Number of subplot columns.</param>
    /// <param name="configures">One callback per subplot, in row-major order (length must equal <c>rows*cols</c>).</param>
    /// <param name="title">Optional figure title.</param>
    /// <param name="width">Figure width in pixels (default 800).</param>
    /// <param name="height">Figure height in pixels (default 600).</param>
    public static FigureBuilder ScientificPaper(
        int rows, int cols,
        Action<AxesBuilder>[] configures,
        string? title = null,
        double width = 800, double height = 600)
    {
        if (configures.Length != rows * cols)
            throw new ArgumentException(
                $"ScientificPaper expects {rows * cols} configure callbacks (one per subplot), got {configures.Length}.",
                nameof(configures));

        var builder = Plt.Create()
            .WithSize(width, height)
            .WithDpi(150)
            .TightLayout();

        if (title is not null) builder.WithTitle(title);

        for (int i = 0; i < rows * cols; i++)
        {
            int idx = i;
            builder.AddSubPlot(rows, cols, idx + 1, ax =>
            {
                ax.HideTopSpine();
                ax.HideRightSpine();
                configures[idx](ax);
            });
        }

        return builder;
    }

    /// <summary>
    /// Creates a joint distribution plot: center scatter with marginal histograms on the top and right edges.
    /// Layout: 2×2 GridSpec — top-left = X marginal, bottom-left = scatter, bottom-right = Y marginal.
    /// </summary>
    /// <param name="x">X data values.</param>
    /// <param name="y">Y data values.</param>
    /// <param name="title">Optional figure title.</param>
    /// <param name="bins">Number of histogram bins for the marginal distributions (default 30).</param>
    public static FigureBuilder JointPlot(double[] x, double[] y, string? title = null, int bins = 30) =>
        new JointPlotFigure(x, y) { Title = title, Bins = bins }.Build();

    /// <summary>Creates a vertically stacked sparkline dashboard with one row per series.</summary>
    /// <param name="series">Array of (label, values) tuples. Each tuple becomes one subplot row.</param>
    /// <param name="title">Optional figure title.</param>
    public static FigureBuilder SparklineDashboard(
        (string Label, double[] Values)[] series,
        string? title = null)
    {
        int n = series.Length;
        var builder = Plt.Create()
            .WithSize(600, 120 * n);

        if (title is not null) builder.WithTitle(title);

        for (int i = 0; i < n; i++)
        {
            int idx = i;
            var (label, values) = series[idx];
            builder.AddSubPlot(n, 1, idx + 1, ax =>
            {
                ax.Sparkline(values);
                ax.SetYLabel(label);
                ax.HideTopSpine();
                ax.HideRightSpine();
            });
        }

        return builder;
    }

    /// <summary>Creates an N×N pair plot: diagonal panels show histograms, off-diagonal panels show scatter plots.</summary>
    /// <param name="columns">Array of data columns (each is one variable).</param>
    /// <param name="columnNames">Optional variable names for axis labels.</param>
    /// <param name="bins">Number of histogram bins for diagonal panels (default 20).</param>
    public static FigureBuilder PairPlot(double[][] columns, string[]? columnNames = null, int bins = 20) =>
        new PairPlotFigure(columns) { ColumnNames = columnNames, Bins = bins }.Build();

    /// <summary>Creates a facet grid: one subplot per unique category, grouped into columns.</summary>
    /// <param name="x">X values for all observations.</param>
    /// <param name="y">Y values for all observations.</param>
    /// <param name="category">Category label for each observation.</param>
    /// <param name="plotFunc">Action that adds the series to each subplot, receiving filtered X and Y for the category.</param>
    /// <param name="cols">Maximum number of columns in the grid (default 3).</param>
    public static FigureBuilder FacetGrid(double[] x, double[] y, string[] category,
        Action<AxesBuilder, double[], double[]> plotFunc, int cols = 3) =>
        new FacetGridFigure(x, y, category, plotFunc) { MaxCols = cols }.Build();

    /// <summary>Creates a clustermap: a heatmap with hierarchical dendrograms on the row and column margins.</summary>
    /// <param name="data">The N×M data matrix to cluster and display.</param>
    /// <param name="rowLabels">Optional row labels.</param>
    /// <param name="colLabels">Optional column labels.</param>
    public static FigureBuilder Clustermap(double[,] data, string[]? rowLabels = null, string[]? colLabels = null)
    {
        int rows = data.GetLength(0), cols = data.GetLength(1);

        // Compute pairwise Euclidean distance matrices for rows and columns
        var rowDist = ComputeDistanceMatrix(data, byRow: true);
        var colDist = ComputeDistanceMatrix(data, byRow: false);

        var rowDendrogram = HierarchicalClustering.Cluster(rowDist);
        var colDendrogram = HierarchicalClustering.Cluster(colDist);

        // Reorder data by dendrogram leaf order
        int[] rowOrder = rowDendrogram.LeafOrder.Length == rows
            ? rowDendrogram.LeafOrder
            : Enumerable.Range(0, rows).ToArray();
        int[] colOrder = colDendrogram.LeafOrder.Length == cols
            ? colDendrogram.LeafOrder
            : Enumerable.Range(0, cols).ToArray();

        var reordered = data.Permute(rowOrder, colOrder);

        var builder = Plt.Create()
            .WithSize(700, 700)
            .WithGridSpec(2, 2, heightRatios: [1.0, 4.0], widthRatios: [1.0, 4.0]);

        // Top-left: empty placeholder
        builder.AddSubPlot(GridPosition.Single(0, 0), _ => { });

        // Top-right: column dendrogram (render merges as horizontal lines)
        builder.AddSubPlot(GridPosition.Single(0, 1), ax =>
        {
            RenderDendrogram(ax, colDendrogram, isColumn: true, n: cols);
        });

        // Bottom-left: row dendrogram
        builder.AddSubPlot(GridPosition.Single(1, 0), ax =>
        {
            RenderDendrogram(ax, rowDendrogram, isColumn: false, n: rows);
        });

        // Bottom-right: heatmap (reordered)
        builder.AddSubPlot(GridPosition.Single(1, 1), ax =>
        {
            ax.Heatmap(reordered);
        });

        return builder;
    }

    // Computes pairwise Euclidean distance matrix for rows or columns.
    private static double[,] ComputeDistanceMatrix(double[,] data, bool byRow)
    {
        int rows = data.GetLength(0), cols = data.GetLength(1);
        int n = byRow ? rows : cols;
        int m = byRow ? cols : rows;
        var dist = new double[n, n];

        for (int i = 0; i < n; i++)
        for (int j = i + 1; j < n; j++)
        {
            double sum = 0;
            for (int k = 0; k < m; k++)
            {
                double diff = byRow ? data[i, k] - data[j, k] : data[k, i] - data[k, j];
                sum += diff * diff;
            }
            double d = Math.Sqrt(sum);
            dist[i, j] = d;
            dist[j, i] = d;
        }

        return dist;
    }

    // Renders a dendrogram as a series of horizontal (column) or vertical (row) lines.
    private static void RenderDendrogram(AxesBuilder ax, Dendrogram dendrogram, bool isColumn, int n)
    {
        if (dendrogram.Merges.Length == 0) return;

        // Map cluster index → x/y position (center of its leaves)
        var positions = new Dictionary<int, double>(2 * n);
        for (int i = 0; i < n; i++)
            positions[i] = dendrogram.LeafOrder.Length > 0
                ? Array.IndexOf(dendrogram.LeafOrder, i)
                : i;

        int clusterIdx = n;
        var xCoords = new List<double[]>();
        var yCoords = new List<double[]>();

        foreach (var merge in dendrogram.Merges)
        {
            double posL = positions.TryGetValue(merge.Left, out double pl) ? pl : 0;
            double posR = positions.TryGetValue(merge.Right, out double pr) ? pr : 0;
            double posNew = (posL + posR) / 2.0;
            positions[clusterIdx] = posNew;
            clusterIdx++;

            // Each merge draws an inverted-U: down from merge.Distance to leaves at posL, posR
            double h = merge.Distance;
            if (isColumn)
            {
                // Column dendrogram: x = leaf position, y = height
                xCoords.Add([posL, posL, posR, posR]);
                yCoords.Add([0, h, h, 0]);
            }
            else
            {
                // Row dendrogram: x = height (reversed), y = leaf position
                xCoords.Add([0, h, h, 0]);
                yCoords.Add([posL, posL, posR, posR]);
            }
        }

        // Draw each U-shaped connector as a line series
        foreach (var (xs, ys) in xCoords.Zip(yCoords))
            ax.Plot(xs, ys);
    }
}
