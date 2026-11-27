// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The verbs a chart of one axes takes on the figure itself, without <c>AddSubPlot</c>: each is the axes builder's verb of
/// the same name, taken on the figure's single axes at every <see cref="FigureBuilder.Build"/> (<c>OnDefaultAxes</c>), so the
/// two builders draw a series the same way because there is one way written. The list is closed: a new verb is the axes
/// builder's, and a chart reaches it through <c>AddSubPlot</c>.</summary>
public static class FigureBuilderDefaultAxesVerbs
{
    extension(FigureBuilder figure)
    {
        /// <summary>Adds a line series to the default axes.</summary>
        public FigureBuilder Plot(double[] x, double[] y, Action<LineSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.Plot(x, y, configure));

        /// <summary>Adds a scatter series to the default axes.</summary>
        public FigureBuilder Scatter(double[] x, double[] y, Action<ScatterSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.Scatter(x, y, configure));

        /// <summary>Adds a <see cref="SignalXYSeries"/> with monotonically ascending X values to the default axes.</summary>
        public FigureBuilder SignalXY(double[] x, double[] y, Action<SignalXYSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.SignalXY(x, y, configure));

        /// <summary>Adds a <see cref="SignalSeries"/> with uniform sample rate to the default axes.</summary>
        public FigureBuilder Signal(double[] y, double sampleRate = SignalSeries.RestingSampleRate, double xStart = SignalSeries.RestingXStart, Action<SignalSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.Signal(y, sampleRate, xStart, configure));

        /// <summary>Adds a line series with DateTime X values to the default axes; automatically activates the date X-axis.</summary>
        public FigureBuilder Plot(DateTime[] dates, double[] y, Action<LineSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.Plot(dates, y, configure));

        /// <summary>Adds a scatter series with DateTime X values to the default axes; automatically activates the date X-axis.</summary>
        public FigureBuilder Scatter(DateTime[] dates, double[] y, Action<ScatterSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.Scatter(dates, y, configure));

        /// <summary>Adds a bar series to the default axes.</summary>
        public FigureBuilder Bar(string[] categories, double[] values, Action<BarSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.Bar(categories, values, configure));

        /// <summary>Adds a single-value stat tile (a big headline number + label) to the default axes.</summary>
        public FigureBuilder StatTile(double value, Action<StatTileSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.StatTile(value, configure));

        /// <summary>Adds a single-row discrete-state timeline of coloured segments along the X axis to the default axes.</summary>
        /// <param name="segments">Ordered state segments; each defines a coloured rectangle spanning
        /// <c>[Start, End]</c> in data units with a centred label.</param>
        /// <param name="configure">Optional post-construction configurator.</param>
        public FigureBuilder StateTimeline(IReadOnlyList<StateSegment> segments, Action<StateTimelineSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.StateTimeline(segments, configure));

        /// <summary>Adds a histogram series to the default axes.</summary>
        public FigureBuilder Hist(double[] data, int bins = HistogramSeries.RestingBins, Action<HistogramSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.Hist(data, bins, configure));

        /// <summary>Adds an ECDF series to the default axes.</summary>
        public FigureBuilder Ecdf(double[] data, Action<EcdfSeries>? configure = null) => figure.OnDefaultAxes(ax => ax.Ecdf(data, configure));

        /// <summary>Adds a stacked area (stackplot) series to the default axes.</summary>
        public FigureBuilder StackPlot(double[] x, double[][] ySets, Action<StackedAreaSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.StackPlot(x, ySets, configure));

        /// <summary>Adds a pie series to the default axes.</summary>
        public FigureBuilder Pie(double[] sizes, string[]? labels = null, Action<PieSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.Pie(sizes, labels, configure));

        /// <summary>Adds an image series to the default axes.</summary>
        public FigureBuilder Image(double[,] data, Action<ImageSeries>? configure = null) => figure.OnDefaultAxes(ax => ax.Image(data, configure));

        /// <summary>Adds a 2D histogram (density) series to the default axes.</summary>
        public FigureBuilder Histogram2D(double[] x, double[] y, int bins = Histogram2DSeries.RestingBins, Action<Histogram2DSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.Histogram2D(x, y, bins, configure));

        /// <summary>Adds an error bar series to the default axes.</summary>
        public FigureBuilder ErrorBar(double[] x, double[] y, double[] yErrorLow, double[] yErrorHigh, Action<ErrorBarSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.ErrorBar(x, y, yErrorLow, yErrorHigh, configure));

        /// <summary>Adds a step-function series to the default axes.</summary>
        public FigureBuilder Step(double[] x, double[] y, Action<StepSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.Step(x, y, configure));

        /// <summary>Adds a filled area (fill-between) series to the default axes.</summary>
        public FigureBuilder FillBetween(double[] x, double[] y, double[]? y2 = null, Action<AreaSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.FillBetween(x, y, y2, configure));

        /// <summary>Adds a filled contour series to the default axes.</summary>
        public FigureBuilder Contourf(double[] x, double[] y, double[,] z, Action<ContourfSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.Contourf(x, y, z, configure));

        /// <summary>Adds a kernel density estimation (KDE) series to the default axes.</summary>
        public FigureBuilder Kde(double[] data, Action<KdeSeries>? configure = null) => figure.OnDefaultAxes(ax => ax.Kde(data, configure));

        /// <summary>Adds a treemap series to the default axes.</summary>
        public FigureBuilder Treemap(TreeNode root, Action<TreemapSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.Treemap(root, configure));

        /// <summary>Adds a sunburst series to the default axes.</summary>
        public FigureBuilder Sunburst(TreeNode root, Action<SunburstSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.Sunburst(root, configure));

        /// <summary>Adds a dendrogram series to the default axes.</summary>
        /// <param name="root">The root <see cref="TreeNode"/>. Internal nodes carry their merge distance in <see cref="TreeNode.Value"/>.</param>
        /// <param name="configure">Optional callback to configure orientation, cut-height, and clustering colours.</param>
        /// <returns>This <see cref="FigureBuilder"/> for chaining.</returns>
        public FigureBuilder Dendrogram(TreeNode root, Action<DendrogramSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.Dendrogram(root, configure));

        /// <summary>Adds a clustermap series to the default axes (heatmap with optional row/column dendrograms).</summary>
        /// <param name="data">The 2D data matrix to render as the heatmap.</param>
        /// <param name="configure">Optional configuration callback for the series.</param>
        public FigureBuilder Clustermap(double[,] data, Action<ClustermapSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.Clustermap(data, configure));

        /// <summary>Adds a pair-grid series to the default axes (N×N matrix of histograms + scatters).</summary>
        /// <param name="variables">The N input variables (one <c>double[]</c> per variable, all equal length).</param>
        /// <param name="configure">Optional configuration callback for the series.</param>
        /// <returns>This <see cref="FigureBuilder"/> for chaining.</returns>
        public FigureBuilder PairGrid(double[][] variables, Action<PairGridSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.PairGrid(variables, configure));

        /// <summary>Adds a network-graph series to the default axes (nodes + edges in 2D).</summary>
        /// <param name="nodes">The graph's nodes.</param>
        /// <param name="edges">The graph's edges.</param>
        /// <param name="configure">Optional configuration callback for the series.</param>
        /// <returns>This <see cref="FigureBuilder"/> for chaining.</returns>
        public FigureBuilder NetworkGraph(
            IReadOnlyList<GraphNode> nodes,
            IReadOnlyList<GraphEdge> edges,
            Action<NetworkGraphSeries>? configure = null) => figure.OnDefaultAxes(ax => ax.NetworkGraph(nodes, edges, configure));

        /// <summary>Adds a Relative Rotation Graph series to the default axes.</summary>
        /// <param name="assetCloses">Per-asset close price series.</param>
        /// <param name="benchmarkCloses">Benchmark close price series (e.g. BTC).</param>
        /// <param name="assetLabels">Display labels — one per asset.</param>
        /// <param name="configure">Optional configuration callback.</param>
        /// <returns>This <see cref="FigureBuilder"/> for chaining.</returns>
        public FigureBuilder RelativeRotation(
            double[][] assetCloses,
            double[] benchmarkCloses,
            string[] assetLabels,
            Action<RelativeRotationSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.RelativeRotation(assetCloses, benchmarkCloses, assetLabels, configure));

        /// <summary>Adds a Sankey diagram series to the default axes.</summary>
        public FigureBuilder Sankey(SankeyNode[] nodes, SankeyLink[] links, Action<SankeySeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.Sankey(nodes, links, configure));

        /// <summary>Adds a word cloud to the default axes: each word set in a size that follows its weight.</summary>
        public FigureBuilder WordCloud(string[] words, double[] weights, Action<WordCloudSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.WordCloud(words, weights, configure));

        /// <summary>Adds a chord diagram of the flows between groups to the default axes.</summary>
        public FigureBuilder Chord(string[] names, double[,] flows, Action<ChordSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.Chord(names, flows, configure));

        /// <summary>Adds a polar line series to the default axes.</summary>
        public FigureBuilder PolarPlot(double[] r, double[] theta, Action<PolarLineSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.PolarPlot(r, theta, configure));

        /// <summary>Adds a polar scatter series to the default axes.</summary>
        public FigureBuilder PolarScatter(double[] r, double[] theta, Action<PolarScatterSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.PolarScatter(r, theta, configure));

        /// <summary>Adds a line through compositions of three parts to the default axes, which becomes a ternary diagram.</summary>
        public FigureBuilder TernaryPlot(double[] top, double[] left, double[] right, Action<TernaryLineSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.TernaryPlot(top, left, right, configure));

        /// <summary>Adds a marker on each composition of three parts to the default axes, which becomes a ternary diagram.</summary>
        public FigureBuilder TernaryScatter(double[] top, double[] left, double[] right, Action<TernaryScatterSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.TernaryScatter(top, left, right, configure));

        /// <summary>Adds a Piper diagram of the given waters (in mg/L) to the default axes.</summary>
        public FigureBuilder Piper(IReadOnlyList<WaterSample> samples, Action<PiperSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.Piper(samples, configure));

        /// <summary>Adds a polar bar series to the default axes.</summary>
        public FigureBuilder PolarBar(double[] r, double[] theta, Action<PolarBarSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.PolarBar(r, theta, configure));

        /// <summary>Adds a 3D surface series to the default axes.</summary>
        public FigureBuilder Surface(double[] x, double[] y, double[,] z, Action<SurfaceSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.Surface(x, y, z, configure));

        /// <summary>Adds a 3D wireframe series to the default axes.</summary>
        public FigureBuilder Wireframe(double[] x, double[] y, double[,] z, Action<WireframeSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.Wireframe(x, y, z, configure));

        /// <summary>Adds a 3D scatter series to the default axes.</summary>
        public FigureBuilder Scatter3D(double[] x, double[] y, double[] z, Action<Scatter3DSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.Scatter3D(x, y, z, configure));

        /// <summary>Adds a 3D stem series to the default axes.</summary>
        public FigureBuilder Stem3D(double[] x, double[] y, double[] z, Action<Stem3DSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.Stem3D(x, y, z, configure));

        /// <summary>Adds a 3D bar series to the default axes.</summary>
        public FigureBuilder Bar3D(double[] x, double[] y, double[] z, Action<Bar3DSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.Bar3D(x, y, z, configure));

        /// <summary>Sets camera elevation, azimuth, and optional perspective distance on the default axes.</summary>
        /// <param name="elevation">Camera elevation above the XY plane in degrees.</param>
        /// <param name="azimuth">Camera azimuth rotation around the Z axis in degrees.</param>
        /// <param name="distance">The camera's distance from the centre of the box, at least 2. Null keeps matplotlib's default
        /// camera, at <see cref="Camera3D.RestingDistance"/>. The projection is a perspective one either way; a closer camera bends the box more and keeps its size,
        /// as matplotlib's focal length does (a distance d draws as focal length d / 10).</param>
        public FigureBuilder WithCamera(double elevation = Camera3D.RestingElevation, double azimuth = Camera3D.RestingAzimuth, double? distance = null) =>
            figure.OnDefaultAxes(ax => ax.WithCamera(elevation, azimuth, distance));

        /// <summary>Attaches a directional light source for per-face shading on 3D surfaces and bars on the default axes.</summary>
        /// <param name="dx">X component of the light direction.</param>
        /// <param name="dy">Y component of the light direction.</param>
        /// <param name="dz">Z component of the light direction.</param>
        /// <param name="ambient">Ambient light intensity [0, 1]. Default <see cref="Rendering.Lighting.DirectionalLight.RestingAmbient"/>.</param>
        /// <param name="diffuse">Diffuse light intensity [0, 1]. Default <see cref="Rendering.Lighting.DirectionalLight.RestingDiffuse"/>.</param>
        public FigureBuilder WithLighting(double dx, double dy, double dz,
            double ambient = Rendering.Lighting.DirectionalLight.RestingAmbient, double diffuse = Rendering.Lighting.DirectionalLight.RestingDiffuse) =>
            figure.OnDefaultAxes(ax => ax.WithLighting(dx, dy, dz, ambient, diffuse));

        /// <summary>Adds a rug plot series to the default axes.</summary>
        public FigureBuilder Rugplot(double[] data, Action<RugplotSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.Rugplot(data, configure));

        /// <summary>Adds a strip plot series to the default axes.</summary>
        public FigureBuilder Stripplot(double[][] datasets, Action<StripplotSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.Stripplot(datasets, configure));

        /// <summary>Adds an event plot series to the default axes.</summary>
        public FigureBuilder Eventplot(double[][] positions, Action<EventplotSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.Eventplot(positions, configure));

        /// <summary>Adds a broken-bar series to the default axes (matplotlib <c>broken_barh</c>).</summary>
        /// <param name="ranges">One <see cref="BarRange"/> array per row; each inner entry defines a
        /// horizontal segment by <c>(Start, Width)</c> in data units.</param>
        /// <param name="configure">Optional post-construction configurator (colour, labels, bar height).</param>
        public FigureBuilder BrokenBarH(BarRange[][] ranges, Action<BrokenBarSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.BrokenBarH(ranges, configure));

        /// <summary>Adds a count plot series to the default axes.</summary>
        public FigureBuilder Countplot(string[] values, Action<CountSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.Countplot(values, configure));

        /// <summary>Adds a pseudocolor mesh series to the default axes.</summary>
        public FigureBuilder Pcolormesh(double[] x, double[] y, double[,] c, Action<PcolormeshSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.Pcolormesh(x, y, c, configure));

        /// <summary>Adds a residual plot series to the default axes.</summary>
        public FigureBuilder Residplot(double[] x, double[] y, Action<ResidualSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.Residplot(x, y, configure));

        /// <summary>Adds a point plot series to the default axes.</summary>
        public FigureBuilder Pointplot(double[][] datasets, Action<PointplotSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.Pointplot(datasets, configure));

        /// <summary>Adds a swarm plot series to the default axes.</summary>
        public FigureBuilder Swarmplot(double[][] datasets, Action<SwarmplotSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.Swarmplot(datasets, configure));

        /// <summary>Adds a spectrogram series to the default axes.</summary>
        public FigureBuilder Spectrogram(double[] signal, int sampleRate = SpectrogramSeries.RestingSampleRate, Action<SpectrogramSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.Spectrogram(signal, sampleRate, configure));

        /// <summary>Adds a table series to the default axes.</summary>
        public FigureBuilder Table(string[][] cellData, Action<TableSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.Table(cellData, configure));

        /// <summary>Adds a contour series on a triangular mesh to the default axes.</summary>
        public FigureBuilder Tricontour(double[] x, double[] y, double[] z, Action<TricontourSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.Tricontour(x, y, z, configure));

        /// <summary>Adds a pseudocolor series on a triangular mesh to the default axes.</summary>
        public FigureBuilder Tripcolor(double[] x, double[] y, double[] z, Action<TripcolorSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.Tripcolor(x, y, z, configure));

        /// <summary>Adds a quiver key series to the default axes.</summary>
        public FigureBuilder QuiverKey(double x, double y, double u, string label, Action<QuiverKeySeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.QuiverKey(x, y, u, label, configure));

        /// <summary>Adds a wind barb series to the default axes.</summary>
        public FigureBuilder Barbs(double[] x, double[] y, double[] speed, double[] direction, Action<BarbsSeries>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.Barbs(x, y, speed, direction, configure));

        /// <summary>Adds a text annotation at the specified data coordinates on the default axes.</summary>
        public FigureBuilder Annotate(string text, double x, double y, Action<Annotation>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.Annotate(text, x, y, configure));

        /// <summary>Adds a text annotation with an arrow pointing to the given target coordinates on the default axes.</summary>
        public FigureBuilder Annotate(string text, double x, double y, double arrowX, double arrowY,
            Action<Annotation>? configure = null) => figure.OnDefaultAxes(ax => ax.Annotate(text, x, y, arrowX, arrowY, configure));

        /// <summary>Adds a horizontal reference line at the specified Y value on the default axes.
        /// <para>The automatic range of the Y axis holds the line, as matplotlib's <c>axhline</c> updates the data limits,
        /// so a line beyond the data is drawn inside the plot; a line beyond a limit you set is not drawn, nor its
        /// label.</para></summary>
        public FigureBuilder AxHLine(double y, Action<ReferenceLine>? configure = null) => figure.OnDefaultAxes(ax => ax.AxHLine(y, configure));

        /// <summary>Adds a vertical reference line at the specified X value on the default axes.
        /// <para>The automatic range of the X axis holds the line, as matplotlib's <c>axvline</c> updates the data limits,
        /// so a line beyond the data is drawn inside the plot; a line beyond a limit you set is not drawn, nor its
        /// label.</para></summary>
        public FigureBuilder AxVLine(double x, Action<ReferenceLine>? configure = null) => figure.OnDefaultAxes(ax => ax.AxVLine(x, configure));

        /// <summary>Adds a horizontal shaded span between the specified Y values on the default axes.</summary>
        public FigureBuilder AxHSpan(double yMin, double yMax, Action<SpanRegion>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.AxHSpan(yMin, yMax, configure));

        /// <summary>Adds a vertical shaded span between the specified X values on the default axes.</summary>
        public FigureBuilder AxVSpan(double xMin, double xMax, Action<SpanRegion>? configure = null) =>
            figure.OnDefaultAxes(ax => ax.AxVSpan(xMin, xMax, configure));

        /// <summary>Adds a patch the caller has made (a rectangle, a polygon, an arrow: a shape in data coordinates) to the default
        /// axes, as matplotlib's <c>add_patch</c> does; <see cref="AxesBuilderOverlayVerbs.AddPatch"/> adds one to a subplot.</summary>
        /// <param name="patch">The patch to add.</param>
        /// <returns>This builder, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="patch"/> is null.</exception>
        /// <example>
        /// <code>
        /// Plt.Create()
        ///     .Plot(x, y)
        ///     .AddPatch(new Polygon([new(0, 0), new(2, 0), new(1, 1.5)]) { Alpha = 0.4 })
        ///     .ToSvg();
        /// </code>
        /// </example>
        public FigureBuilder AddPatch(Models.Patches.Patch patch)
        {
            ArgumentNullException.ThrowIfNull(patch);
            return figure.OnDefaultAxes(ax => ax.AddPatch(patch));
        }

        /// <summary>Adds a threshold marker on the default axes: a dashed <see cref="Models.ReferenceLine"/>
        /// at <paramref name="value"/> plus a shaded <see cref="Models.SpanRegion"/> covering the breach
        /// zone on the <paramref name="breach"/> side. An optional text <paramref name="label"/> is added
        /// as an annotation at the threshold value.</summary>
        /// <param name="value">The threshold value in data coordinates.</param>
        /// <param name="orientation">Horizontal (constant Y) or Vertical (constant X).</param>
        /// <param name="breach">Which side of the threshold is the breach zone.</param>
        /// <param name="color">Optional color for the line and span. Null uses the renderer cycle color.</param>
        /// <param name="label">Optional label text. Null adds no annotation.</param>
        public FigureBuilder Threshold(double value, Models.Orientation orientation, Models.ThresholdBreach breach,
            Styling.Color? color = null, string? label = null) => figure.OnDefaultAxes(ax => ax.Threshold(value, orientation, breach, color, label));

        /// <summary>Enables or disables the legend-values suffix on the default axes — appends each
        /// labelled series' last data value to its legend entry, e.g. "Signal" becomes "Signal = 2.70"
        /// (InvariantCulture, F2 format). Default is <see langword="false"/>.</summary>
        public FigureBuilder WithLegendValues(bool enabled = true) => figure.OnDefaultAxes(ax => ax.WithLegendValues(enabled));
    }
}
