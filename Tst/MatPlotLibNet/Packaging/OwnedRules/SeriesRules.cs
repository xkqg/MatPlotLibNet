// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Packaging.OwnedRules;

/// <summary>The rules of a series and the verbs that make one: a series is drawn by its renderer, a verb ends in the builder's own
/// tail, a group wears one colour, a band is composed by its factory, a stream keeps its samples in its ring, a hue groups its
/// observations in one place, a composite chart has one owner.</summary>
internal static class SeriesRules
{
/// <summary>A series puts its axes in the coordinate system its kind is drawn in at every seat it is added at, through one member,
    /// <c>Axes.TakeTheSystemOf</c>, which a Y axis slot and the top X axis both ask.</summary>
    private static readonly OwnedRule CoordinateSystemClaim = new(
        "the coordinate system a series added at any seat puts its axes in is claimed by Axes.TakeTheSystemOf: no seat reads a kind's DrawnIn to set it",
        @"\bDrawnIn:\s*\{\s*\}",
        "Src/MatPlotLibNet/Models/AxesSeatExtensions.cs") { Under = "Src/", Finds = [
        "        if (series is ChartSeries { DrawnIn: { } system }) CoordinateSystem = system;"] };

/// <summary>A band a verb or an indicator fills between two curves (a ridge, a stream, a confidence band, a channel) is made by
    /// <c>AreaSeries.Band</c>, outlined at the line's resting width, or <c>AreaSeries.Channel</c>, with no outline: the width given so
    /// the band keeps its own look under every theme is a value of the factory, typed once. Only the area itself, its reader and the
    /// two fill verbs construct one.</summary>
    private static readonly OwnedRule ComposedBand = new(
        "a band a verb or an indicator composes is AreaSeries.Band (outlined at the line's resting width) or AreaSeries.Channel (no outline): no verb constructs an area and gives it its width itself",
        @"\bnew AreaSeries\(",
        "Src/MatPlotLibNet/Models/Series/XY/AreaSeries.cs", "Src/MatPlotLibNet/Models/Series/XY/AxesXYVerbs.cs") { Under = "Src/", Finds = [
        "                var cells = new AreaSeries(x, top)"] };

/// <summary>Series drawn as one group wear one colour: <c>Axes.InColorGroup</c> draws the next group number and stamps every
    /// member once; no verb threads the number by hand.</summary>
    private static readonly OwnedRule ColorGroupStamp = new(
        "a group of series that wears one colour is stamped by Axes.InColorGroup: no verb draws the next group number or sets a series' group itself",
        @"\bNextColorGroup\(|\bColorGroup\s*=(?![=>])",
        "Src/MatPlotLibNet/Models/AxesSeatExtensions.cs", "Src/MatPlotLibNet/Models/Series/ChartSeries.cs") { Under = "Src/", Finds = [
        "            int group = ax.NextColorGroup();"] };

/// <summary>A builder sets an axis (its title, limits, colour, link, margin, scale, ticks) through <c>AxisVerbs</c>, the one set of axis
    /// verbs every axis builder composes: no builder writes an axis' setting itself.</summary>
    private static readonly OwnedRule BuilderAxisWrite = new(
        "a builder sets an axis through AxisVerbs, the one set of axis verbs every axis builder composes: no builder writes an axis' setting, colours, labels or links an axis itself",
        @"\b(?:XAxis|YAxis|ZAxis|SecondaryXAxis|Axis|_top)\.\w+\s*=(?![=>])|\bApplyAxisColor\(|\bLinkThrough\(|\.SetLabel\(|\bUse(?:SymlogScale|AutoDateTicks|DateFormat)\(",
        "Src/MatPlotLibNet/Builders/AxisVerbs.cs", "Src/MatPlotLibNet/Builders/TickVerbs.cs") { Under = "Src/MatPlotLibNet/Builders/", Finds = [
        "    public AxesBuilder SetXLabel(string label) { _axes.XAxis.Label = label; return this; }"] };

/// <summary>The subplots that share an axis are joined by one walk from a key to its axes, <c>Figure.ShareByKeys</c>, which the
    /// builder and the reader of a document both end in.</summary>
    private static readonly OwnedRule ShareByKey = new(
        "an axes that shares an axis with another subplot is joined to it by FigureShareExtensions (ShareByKeys by its key, ShareByPlace by the place a document names): neither the builder nor the reader walks the keys or the places itself",
        @"\.Share[XY]With\s*=(?![=>])",
        "Src/MatPlotLibNet/Models/FigureShareExtensions.cs") { Under = "Src/MatPlotLibNet/", Finds = [
        "                share.Axes.ShareXWith = xTarget;"] };

/// <summary>A line over its point budget (<c>MaxDisplayPoints</c>) is reduced in one place, <c>XYData.AtMost</c>: the cull of the view
    /// (a linear scan, or index arithmetic for a monotonic x) hands its samples to it, and no renderer starts an LTTB reduction itself.</summary>
    private static readonly OwnedRule PointBudget = new(
        "a line over its point budget (MaxDisplayPoints) is reduced by XYData.AtMost, which both the linear-scan cull and the monotonic slice end in: nothing else starts an LttbDownsampler",
        @"\bnew LttbDownsampler\(\)",
        "Src/MatPlotLibNet/Rendering/Downsampling/XYData.cs") { Under = "Src/", Finds = [
        "        return new LttbDownsampler().Downsample(culled.X, culled.Y, maxPoints.Value);",
        "        return new LttbDownsampler().Downsample(x, y, maxPoints.Value);"] };

/// <summary>The text of a value a series labels (a bar's, a contour line's, a heat map's cell) is <c>series.LabelOf(value)</c>: the
    /// format it was given, else the one its kind rests at (a bar's writes matplotlib's lower-case exponent marker), in the invariant
    /// culture. A renderer reads the format only to hand it on (the clustermap's panel).</summary>
    private static readonly OwnedRule LabelText = new(
        "the text of a value a series labels is series.LabelOf(value): no renderer writes format-or-default and the culture itself, nor asks whether a format was given (a bar's default is its kind's, matplotlib's %g; the clustermap hands its format to its heat map)",
        @"\.LabelFormat\b",
        "Src/MatPlotLibNet/Rendering/SeriesRenderers/Grid/ClustermapSeriesRenderer.cs") { Under = "Src/MatPlotLibNet/Rendering/SeriesRenderers/", Finds = [
        "        string text = line.Level.ToString(series.LabelFormat ?? \"G4\", CultureInfo.InvariantCulture);",
        "            string text = value.ToString(series.LabelFormat ?? \"F2\", CultureInfo.InvariantCulture);",
        "        series.LabelFormat is null ? series.LabelOf(value).Replace('E', 'e') : series.LabelOf(value);"] };

/// <summary>Where the label of a cell of a hierarchy stands in it (the pad, the baseline, the room for the text, the header strip of
    /// a treemap's interior tile) is <c>CellLabel</c>'s: no renderer of a hierarchy types the pad or the gap again.</summary>
    private static readonly OwnedRule CellLabelRule = new(
        "where the label of a cell of a hierarchy (a treemap tile, an icicle cell) stands in it, and how much room it has, is CellLabel's: no hierarchy's renderer types the pad, the baseline gap or the room itself",
        @"\bLabelPad\b|\bbounds\.X \+ 4\b|\bbounds\.Width - 8\b|\+ 16 <=|\bheaderH - 4\b|\bBox\.Y \+ LabelSize\b|\bnew\(4, 2\)",
        "Src/MatPlotLibNet/Rendering/SeriesRenderers/Hierarchical/HierarchicalLayout.cs") { Under = "Src/MatPlotLibNet/Rendering/SeriesRenderers/Hierarchical/", Finds = [
        "    private const double LabelPad = 4;",
        "                var label = FitLabel(node.Label, font, bounds.Width - 8, series.LabelFit);",
        "                    Ctx.DrawText(label, new Point(bounds.X + 4, bounds.Y + fontSize + 2),",
        "                && Ctx.ExtentOf(node.Headline, font).Width + Ctx.ExtentOf(node.Label, font).Width + 16 <= bounds.Width)",
        "            Ctx.DrawText(node.Label, new Point(bounds.X + 4, bounds.Y + headerH - 4),"] };

/// <summary>Whether a pie or a donut has a whole to draw is the one guard <c>sizes.DrawableTotal()</c>: no chart of the slices of a
    /// whole sums its drawable sizes and tests the sum itself.</summary>
    private static readonly OwnedRule WholeOfSlices = new(
        "the total of the slices of a whole, and whether there is a whole to draw, is sizes.DrawableTotal(): no chart sums its drawable sizes and tests the sum itself",
        @"\.DrawableValues\(\)\.Sum\(\)",
        "Src/MatPlotLibNet/Rendering/SeriesRenderers/Circular/PartsOfAWholeExtensions.cs") { Under = "Src/MatPlotLibNet/Rendering/SeriesRenderers/", Finds = [
        "        double total = series.Sizes.DrawableValues().Sum();"] };

/// <summary>A series of the library, a package's included, is drawn by its renderer: its <c>Accept</c> is one line that hands it to the
    /// visitor, by its <c>Visit</c> overload or, for a series of a package that the public visitor has no overload for, by
    /// <c>visitor.Draw(this, renderer)</c> (<c>ISeriesRendererHost</c>). No series draws itself: a block body, or a body that is not the
    /// hand-over, is a renderer written inside a model.</summary>
    private static readonly OwnedRule SeriesAccept = OwnedRule.Nowhere(
        "a series is drawn by its renderer: Accept hands the series to the visitor (visitor.Visit(this, area), or visitor.Draw(this, renderer) for a package's series through ISeriesRendererHost) and draws nothing itself",
        @"\bvoid Accept\(ISeriesVisitor (\w+), RenderArea \w+\)\s*(?:$|\{|=>(?>\s*)(?!\1\.(?:Visit|Draw)\(this,))", under: "Src/") with { Finds = [
        "    public override void Accept(ISeriesVisitor visitor, RenderArea area)",
        "    public override void Accept(ISeriesVisitor visitor, RenderArea area) { area.Context.DrawLines(points, style); }",
        "    public override void Accept(ISeriesVisitor visitor, RenderArea area) => area.Context.DrawLines(points, style);"], Ignores = [
        "    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);",
        "    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Draw(this, static context => new GeoPolygonRenderer(context));",
        "    public abstract void Accept(ISeriesVisitor visitor, RenderArea area);"] };

/// <summary>A streaming series keeps its samples, counts their changes and marks them for the figure's redraw in its
    /// StreamRing: no series holds a ring buffer or a version counter of its own beside it.</summary>
    private static readonly OwnedRule StreamKeptInItsRing = new(
        "a streaming series keeps its samples and counts every append, batch and clear in its StreamRing: no series holds a ring buffer or a version counter of its own",
        @"\bRingBuffer<|\bInterlocked\.(?:Increment|Read)\(\s*ref\s+_version\b",
        "Src/MatPlotLibNet/Models/Series/Streaming/StreamRing.cs") { Under = "Src/MatPlotLibNet/Models/", Finds = [
        "    private readonly RingBuffer<SignalSample> _samples;",
        "        Interlocked.Increment(ref _version);",
        "    public long Version => Interlocked.Read(ref _version);"] };

/// <summary>A streaming figure learns whether a frame is due by asking every series one question, IVersionedSeries: a test of
    /// which public streaming contract a series is appended through misses a kind that has another (the sampled signal was).</summary>
    private static readonly OwnedRule StreamAskedByOneQuestion = OwnedRule.Nowhere(
        "whether a series' data changed is asked through IVersionedSeries, whatever its kind: no site tests which public streaming contract a series has to read its version",
        @"\bis\s+IStreaming(?:Ohlc)?Series\b",
        under: "Src/") with { Finds = [
        "                    if (series is IStreamingSeries s)",
        "                    else if (series is IStreamingOhlcSeries o)"] };

/// <summary>A streaming line and a streaming scatter are one stream of points: what their JSON record carries of it (its kind,
    /// one snapshot's x and y, its capacity, its colour) and how a read restores it are written once, in StreamingSeries.</summary>
    private static readonly OwnedRule StreamOfPointsOnTheWire = new(
        "the record of a stream of points (kind, one snapshot's x and y, capacity, colour) and its restore are written by StreamingSeries: no streaming kind writes or reads them itself",
        @"\bXData\s*=\s*snapshot\.XData\b|\bAppendPoints\(\s*dto\.XData\b",
        "Src/MatPlotLibNet/Models/Series/Streaming/StreamingSeries.cs") { Under = "Src/MatPlotLibNet/Models/", Finds = [
        "            XData = snapshot.XData,"] };

/// <summary>Which samples of a line are drawn (the cull to the view, the point budget, the slice of a monotonic series) is the
    /// series renderer base's.</summary>
    private static readonly OwnedRule SamplesDrawnByTheBase = new(
        "which samples of a series are drawn (the cull to the view, the LTTB point budget, the slice of a monotonic series) is decided by SeriesRenderer (CulledToView, ApplyDownsampling, ApplyMonotonicDownsampling): no renderer culls or slices itself",
        @"\bViewportCuller\.|\bnew LttbDownsampler\(|\bMonotonicViewportSlicer\.",
        "Src/MatPlotLibNet/Rendering/SeriesRenderers/SeriesRenderer.cs") { Under = "Src/MatPlotLibNet/Rendering/SeriesRenderers/", Finds = [
        "            ? ViewportCuller.Cull(series.XData, series.YData, Transform.UnscaledXMin, Transform.UnscaledXMax)"] };

/// <summary>A verb of the axes builder ends in one of the builder's own tails, written once each: what its axes makes (a series,
    /// an overlay) goes to the caller's callback through <c>Configured</c>, which returns the builder; a streaming series through
    /// <c>Streamed</c>, which returns the series; an indicator through <c>ApplyIndicator</c>. So no verb wraps the axes' verb in a
    /// lambda for a tail to call, or holds an indicator, a made overlay or a new streaming series in a local to hand it on by hand.</summary>
    private static readonly OwnedRule VerbTailOfTheAxesBuilder = OwnedRule.Nowhere(
        "a verb of the axes builder ends in the builder's one tail for what it makes (Configured for a series or an overlay, Streamed for a streaming series, ApplyIndicator for an indicator): no verb restates a tail in statements of its own",
        @"\bAddSeries\(\s*ax\s*=>|^\s*var\s+indicator\s*=\s*new\s+Indicators\.|^\s*var\s+\w+\s*=\s*new\s+Streaming\w*Series\("
        + @"|^\s*var\s+\w+\s*=\s*_axes\.(?:Add\w+|Annotate|Ax[HV]\w+)\(",
        under: "Src/MatPlotLibNet/Builders/") with { Finds = [
        "        => AddSeries(ax => ax.Plot(x, y), configure);",
        "        var indicator = new Indicators.Rsi(prices, period);",
        "        var series = new StreamingLineSeries(capacity);",
        "        var line = _axes.AxHLine(y);",
        "        var annotation = _axes.Annotate(text, x, y);"] };

    private static readonly OwnedRule SequenceTabledByPosition = new(
        "a sequence of values read by its position is tabled by ChartDataTable.FromIndexedValues: no series writes the index column and the loop itself",
        @"new\(""index""\)",
        "Src/MatPlotLibNet/Models/ChartDataTable.cs") { Under = "Src/", Finds = [
        "        return new ChartDataTable(null, [new(\"index\"), new(Label ?? \"value\", Axis: DataAxis.X)], rows);"] };

    /// <summary>A column whose values are positions along an axis (the point's <c>x</c> and <c>y</c>, a bar's start, a price) is
    /// <c>ChartDataColumn.Position(axis, header)</c>, which also owns the bare header (<c>x</c>, <c>y</c>) of a position the series gives
    /// no name of its own; <c>ChartDataTable.XY</c> is the table of a series whose columns are numbers read by index and whose first two
    /// are the point. A column that copies the axis of another (the table keyed by names) and the wire name of the enum are not it.</summary>
    private static readonly OwnedRule PointColumns = new(
        "a column whose values are positions along an axis is ChartDataColumn.Position(axis, header), which owns the bare header x or y: no series types a column's axis (named or positional), a column headed x or y, or the bare header itself",
        @"\bAxis:\s|\bDataColumnKind\.\w+,\s*DataAxis\.[XY]\b|\b(?:Position\(DataAxis\.[XY],|XY\()\s*(?:Label \?\? )?""[xy]""|\bnew\(""[xy]""\)|\bHeader == ""[xy]""",
        "Src/MatPlotLibNet/Models/ChartDataColumn.cs") { Under = "Src/", Finds = [
        "            [new(\"x\", DataColumnKind.Number, DataAxis.X), new(\"y\", DataColumnKind.Number, DataAxis.Y), new(Label ?? \"value\")],",
        "        return new ChartDataTable(null, [new(\"task\", DataColumnKind.Text), new(\"start\", Axis: DataAxis.X), new(\"end\", Axis: DataAxis.X)], rows);",
        "            new(\"open\", Axis: DataAxis.Y), new(\"high\", Axis: DataAxis.Y), new(\"low\", Axis: DataAxis.Y), new(\"close\", Axis: DataAxis.Y),",
        "            columns.Add(new(given.Header, Axis: given.Axis));",
        "        ChartDataTable.XY(\"y\", X, Y);",
        "        ChartDataTable.XY(Label ?? \"y\", XData, YData);",
        "        : ChartDataTable.FromNumberColumns([ChartDataColumn.Position(DataAxis.Y), ChartDataColumn.Position(DataAxis.X, Label ?? \"x\")], [YData, XData]);",
        "            [new(\"x\"), new(Label ?? \"y\")], [snapshot.XData, snapshot.YData]);",
        "        string header = column.Header == \"x\" && axis.Label is { Length: > 0 } label ? label : column.Header;"], Ignores = [
        "        headers[0] = ChartDataColumn.Position(DataAxis.X);",
        "            [ChartDataColumn.Position(DataAxis.Y), ChartDataColumn.Position(DataAxis.X, Label ?? \"x1\"), ChartDataColumn.Position(DataAxis.X, \"x2\")], [YData, XData, second])",
        "    public override ChartDataTable? ToDataTable() => ChartDataTable.FromIndexedValues(ChartDataColumn.Position(DataAxis.X, Label ?? \"value\"), Data);",
        "        ChartDataTable.XY(Label, XData, YData);",
        "        ChartDataTable.XY(null, X.Data, Y.Data, new NumberColumn(\"z\", Z.Data));",
        "                [ChartDataColumn.Position(DataAxis.X), ChartDataColumn.Position(DataAxis.X, \"left\"), ChartDataColumn.Position(DataAxis.X, \"right\"), new(Label ?? \"y\")], [.. rows]);",
        "            c == key ? new ChartDataColumn(header, DataColumnKind.Text, table.Columns[c].Axis)",
        "        return new ChartDataColumn(header, kind, column.Axis);",
        "        DataAxis.X => \"x\","] };

    /// <summary>A verb that acts on the series added last (a bar's labels, an XY series' downsampling, the colour map and the normaliser
    /// of a mappable one, the prices an indicator is computed over) finds it through <c>LastSeriesOf</c> (the last series of a kind) or
    /// <c>LastSeriesIs</c> (the last series, when it is of the kind), which say that it does nothing when there is none.</summary>
    private static readonly OwnedRule LastSeries = new(
        "the series added last, of a kind, is found by Axes.LastSeriesOf or LastSeriesIs: no verb searches the series list from the end itself",
        @"\bSeries\.(?:Last|LastOrDefault)\(|\bSeries\[\^1\]|\bSeries\[[\w.]*Series\.Count - 1\]",
        "Src/MatPlotLibNet/Models/AxesLastSeriesExtensions.cs") { Under = "Src/", Finds = [
        "        if (_axes.Series.LastOrDefault(s => s is BarSeries) is BarSeries bar)",
        "        if (_axes.Series.LastOrDefault() is Models.Series.IColormappable c) c.ColorMap = colorMap;",
        "        internal double[] PriceData => axes.Series.LastOrDefault(s => s is IPriceSeries) is IPriceSeries price",
        "        var last = axes.Series[^1];"], Ignores = [
        "        if (_axes.LastSeriesOf<BarSeries>() is { } bar)",
        "        double total = running.LastOrDefault(r => !double.IsNaN(r), 0);"] };

    /// <summary>The rules of the area, in the order the scan reads them.</summary>
    internal static OwnedRule[] All { get; } =
    [
        CoordinateSystemClaim,
        ComposedBand,
        ColorGroupStamp,
        BuilderAxisWrite,
        ShareByKey,
        PointBudget,
        LabelText,
        CellLabelRule,
        WholeOfSlices,
        SeriesAccept,
        new("a series verb of an axis past the primary (a Y slot, the top axis) refuses on a linked axis, makes the series, hands it to the caller's configure and returns it through AxisVerbs.Seat: no builder spells the refusal itself",
            @"\bRefuseSeriesWhenLinked\(", "Src/MatPlotLibNet/Builders/AxisVerbs.cs") { Under = "Src/MatPlotLibNet/", Finds = [
            "        _axes.SecondaryXAxis!.RefuseSeriesWhenLinked(nameof(x));",
            "        RefuseSeriesWhenLinked(_axes.SecondaryXAxis!, nameof(x));"] },
        OwnedRule.Nowhere("the coordinate system a kind of series is drawn in is its ChartSeries.DrawnIn, which Axes.AddSeries puts the axes in: no verb or reader of a series sets the axes' coordinate system itself",
            @"\baxes\.CoordinateSystem\s*=\s*CoordinateSystem\.", under: "Src/MatPlotLibNet/Models/Series/") with { Finds = [
            "            axes.CoordinateSystem = CoordinateSystem.Polar;"] },
        StreamKeptInItsRing,
        StreamAskedByOneQuestion,
        StreamOfPointsOnTheWire,
        SamplesDrawnByTheBase,
        VerbTailOfTheAxesBuilder,
        SequenceTabledByPosition,
        PointColumns,
        LastSeries,
        new("observations are grouped by their hue label in the order the labels are first seen in HueGrouper: no figure buckets them by hand",
            @"\bkeyOrder\b|\bbuckets\.TryGetValue\(",
            "Src/MatPlotLibNet/Data/HueGrouper.cs") { Under = "Src/", Finds = [
            "        var keyOrder = new List<string>();"] },
        new("the palette of hue groups falls back to the ten-colour cycle in HueGrouper: no figure picks the fallback itself",
            @"\bHueGrouper\.DefaultPalette\b|\bpalette\s+is\s+\{\s*Length:\s*>\s*0\s*\}|\bPalette\s+is\s+\{\s*Length:\s*>\s*0\s*\}",
            "Src/MatPlotLibNet/Data/HueGrouper.cs") { Under = "Src/", Finds = [
            "        var palette  = Palette is { Length: > 0 } ? Palette : HueGrouper.DefaultPalette;"] },
        new("a series of a hue group wears the group's colour and label, and a histogram of one its overlap opacity, in HueSplit: no figure dresses it by hand",
            @"\bColor\s*=\s*(?:g|group)\.Color\b|(?:\bAlpha|\bOverlapAlpha)\s*=\s*0\.7\b",
            "Src/MatPlotLibNet/Data/HueSplit.cs") { Under = "Src/", Finds = [
            "            ax.Scatter(g.X, g.Y, s => { s.Color = g.Color; s.Label = g.Label; });"] },
        OwnedRule.Nowhere("hue labels are numbered in the order they are first seen by HueGrouper.Codes: the data-frame package numbers none itself",
            @"\.Distinct\(\)|\bidMap\b",
            under: "Src/MatPlotLibNet.DataFrame/") with { Finds = [
            "                var unique = hueValues.Distinct().OrderBy(x => x, StringComparer.Ordinal).ToArray();"] },
        new("a matrix is clustered by HierarchicalClustering, called by the figure that clusters a clustermap's rows and columns",
            @"\bHierarchicalClustering\.Cluster\(",
            "Src/MatPlotLibNet/Builders/ClustermapFigure.cs") { Under = "Src/", Finds = [
            "        var rowDendrogram = HierarchicalClustering.Cluster(rowDist);"] },
        new("a dendrogram's merges are read by the tree they become (ToTree): a chart is drawn from the tree",
            @"\.Merges\b",
            "Src/MatPlotLibNet/Models/DendrogramExtensions.cs") { Under = "Src/", Finds = [
            "        if (dendrogram.Merges.Length == 0) return;"] },
        new("an item of a table that was given no name is called by its place counted from 1 (column 3, sample 2, the bare 4): ArrayElementExtensions.NameOr, no series numbers it itself",
            @"\(\w+ \+ 1\)\.InvariantText\(\)|\bFromText\([^;]*\{\(?\w+ \+ 1[)}]",
            "Src/MatPlotLibNet/Extensions/ArrayElementExtensions.cs") { Under = "Src/", Finds = [
            "            string header = ColumnHeaders.ElementOr(c, static n => \"column \" + (n + 1).InvariantText());",
            "                DataCell.FromText(Samples[i].Label ?? $\"sample {(i + 1).InvariantText()}\"),",
            "            var label = DataCell.FromText((d + 1).InvariantText());",
            "                DataCell.FromText(string.Invariant($\"image {i + 1}\")),"] },
        new("the category of a row of a table is its text first and its numbers after it in ChartDataTable.ByCategory: no series writes the category cell of a row itself",
            @"\bDataCell\.FromText\(\w*[Cc]ategories\[",
            "Src/MatPlotLibNet/Models/ChartDataTable.cs") { Under = "Src/", Finds = [
            "                ? [DataCell.FromText(Categories[i]), DataCell.FromNumber(Values[i]), DataCell.FromNumber(levels[i])]"] },
        new("the samples of a group are rows under the name of their group in ChartDataTable.OfGroups: no series holds the group's name cell and loops its samples itself",
            @"\bvar (?:label|row|name) = DataCell\.FromText\(",
            "Src/MatPlotLibNet/Models/ChartDataTable.cs") { Under = "Src/", Finds = [
            "            var label = DataCell.FromText(AssetLabels[a]);",
            "            var row = DataCell.FromText((r + 1).InvariantText());"] },
        new("the table of points over a plane that carry a height is x, y and z in ChartDataTable.XYZ: no series writes the height column of it itself",
            @"new NumberColumn\((?:(?:Label \?\? )?""z""|heightName),",
            "Src/MatPlotLibNet/Models/ChartDataTable.cs") { Under = "Src/", Finds = [
            "        ChartDataTable.XY(null, X.Data, Y.Data, new NumberColumn(Label ?? \"z\", Z.Data));"] },
    ];
}
