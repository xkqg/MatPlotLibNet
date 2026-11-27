// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Packaging.OwnedRules;

/// <summary>The resting value of a setting (what a model holds when it is told nothing) is named once, as a constant of its owner,
/// and read by the property, the writer, the reader and every verb that makes it; its documentation names it by a cref.</summary>
internal static class RestingValueRules
{
/// <summary>The words a resting value is named by: <c>Resting*</c> (a constant of the model), <c>Rest</c> and <c>rest</c> (a record or
    /// an instance a wire form holds), <c>AtRest</c>, <c>Plain</c>, <c>Standard</c> and <c>Library</c> (a look or a style at its own
    /// default) and <c>Default*</c> (a constant that says it is one, <c>ChordLayout.DefaultPadding</c>).</summary>
    private const string RestingName = @"(?:Rest\w*|rest\w*|AtRest|Default\w+|Plain|Standard|Library)";

/// <summary>A literal a resting value is typed as: a number other than the type's own zero, <c>true</c> or a member of an enum.</summary>
    private const string RestingLiteral = @"(?![\w.]*" + RestingName + @"\b)(?:true|-?(?:[1-9][0-9_]*|0\.[0-9_]*[1-9][0-9_]*)(?:\.[0-9]+)?[dDfFmM]?|[A-Z]\w*(?:\.[A-Z]\w*)+)";

/// <summary>A setting a model keeps while it is given is one slot: <c>internal T? GivenX { get; set => Change(ref field, value); }</c>,
    /// which the public setting reads over its Resting constant. A hidden field with a getter over it and an accessor beside it says the
    /// same thing three times. Read in the models, the builders' options and the indicators.</summary>
    private const string HiddenGivenSlot = @"\bGiven\w+\s*=>\s*_\w+|\bget\s*=>\s*_\w+\s*\?\?";

    /// <summary>Lines of the documentation that name a start by a cref, or type a number of another rule: no prose rule of a start
    /// reads them as one.</summary>
    private static readonly string[] StartsNamedByACref =
    [
        "    /// <param name=\"capacity\">Maximum data points retained. Default <see cref=\"StreamingSeries.RestingCapacity\"/>.</param>",
        "    /// <param name=\"width\">Figure width in pixels (default <see cref=\"Figure.RestingWidth\"/>).</param>",
        "    /// <summary>The number of bins. Default <see cref=\"RestingBins\"/>.</summary>",
        "    /// <summary>The size of each marker, in points. Default <see cref=\"MarkerLook.RestingSize\"/>.</summary>",
        "    /// <summary>Ambient light intensity [0, 1]. Default 0.3.</summary>",
        "    /// <summary>Width of each node rectangle, in pixels. Default 20.</summary>",
    ];

/// <summary>A default a verb, its layers and the value it makes all take (the light's ambient and diffuse, the symlog threshold,
    /// the date format, the style of an axis break) is one constant of its owner, read by every declaration.</summary>
    private static readonly OwnedRule DefaultArgumentTyped = OwnedRule.Nowhere(
        "a default the layers of a verb and the value it makes take is one Resting or Default constant of its owner: no declaration types the light's ambient or diffuse, the symlog threshold, a date format or a break style",
        @"\b(?i:ambient|diffuse|linthresh)\s*=\s*\d|\bBreakStyle\s+[Ss]tyle\s*=\s*BreakStyle\.|\bstring\s+format\s*=\s*""|\bDateFormat\s*\{[^}]*\}\s*=\s*""",
        under: "Src/") with { Finds = [
        "    public AxesBuilder WithSymlogYScale(double linthresh = 1.0)"] };

/// <summary>The resting value a wire form leaves out is named, never typed again. A value at rest is left out of the JSON by
    /// <c>x.OrNullAtRest(rest)</c>, where <c>rest</c> is what the model holds when it is told nothing: a <c>Resting</c> constant of the
    /// model, a record of the wire form's own (<c>Rest.Width</c>), or a look at its default. The one number a model starts from is
    /// read by its property, its writer, its reader and every verb that makes it, so a change to it moves them all.</summary>
    private static readonly OwnedRule RestingValueNamed = OwnedRule.Nowhere(
        "the resting value of a setting is named once, as a Resting constant of its model (or a record or a look the wire form holds at rest) and read by the property, the writer, the reader and every verb that makes it: no wire form compares a setting with a number, a flag or an enum member it types again",
        @"\.OrNullAtRest\((?!(?:false|0(?:\.0)?)\)|[^)]*\b" + RestingName + @"\b)"
        + @"|(?<v>[\w.]+)\s*==\s*" + RestingLiteral + @"\s*\?\s*null\s*:\s*\k<v>\b"
        + @"|(?<v>[\w.]+)\s*!=\s*" + RestingLiteral + @"\s*\?\s*\k<v>(?:\.To\w+\(\))?\s*:\s*null"
        + @"|\bdto\.\w+\s*\?\?\s*" + RestingLiteral + @"(?![\w.(])",
        under: "Src/") with { Finds = [
        "        Alpha = Alpha.OrNullAtRest(1.0),",
        "        Bins = Bins.OrNullAtRest(10),",
        "        ShowOutliers = ShowOutliers.OrNullAtRest(true),",
        "        HistType = HistType.OrNullAtRest(HistType.Bar),",
        "        Orientation = Orientation.OrNullAtRest(BarOrientation.Vertical)?.ToWireName(),",
        "        Degree = Degree == 1 ? null : Degree,",
        "        Alpha = Alpha != 0.8 ? Alpha : null,",
        "        Elevation = axes.Elevation != 30 ? axes.Elevation : null,",
        "        Azimuth = axes.Azimuth != -60 ? axes.Azimuth : null,",
        "        MaskMode = MaskMode != HeatmapMaskMode.None ? MaskMode.ToString() : null,",
        "        LineStyle = line.LineStyle != Styling.LineStyle.Solid ? line.LineStyle.ToWireName() : null,",
        "        Position = spine.Position == SpinePosition.Edge ? null : spine.Position.ToWireName(),",
        "        s.LineWidth = dto.LineWidth ?? 2.0;",
        "        var s = axes.Hist(dto.Data ?? [], dto.Bins ?? 10);",
        "        var s = axes.AddSeries(new StreamingLineSeries(dto.Capacity ?? 10_000));",
        "        s.Baseline = dto.Baseline ?? StackedBaseline.Zero;"], Ignores = [
        "        Alpha = Alpha.OrNullAtRest(RestingAlpha),",
        "        Density = Density.OrNullAtRest(false),",
        "        SignalXStart = XStart.OrNullAtRest(0.0),",
        "        Visible = patch.Visible.OrNullAtRest(rest.Visible),",
        "        Width = Width.OrNullAtRest(Rest.Width),",
        "        GridSize = GridSize.OrNullAtRest(HexbinSeries.RestingGridSize),",
        "        Iterations = LayoutIterations.OrNullAtRest(ForceDirectedGraphLayout.DefaultIterations),",
        "        LineStyle = LineStyle.OrNullAtRest(SpineConfig.RestingLineStyle),",
        "        Background = theme.Background.OrNullAtRest(Plain.Background),",
        "        Spacing = figure.Spacing == RestingFigure.Spacing ? null : SubPlotSpacingDto.From(figure.Spacing),",
        "        var s = axes.Gauge(dto.GaugeValue ?? 0);",
        "        s.Expanded = dto.TileExpanded ?? false;",
        "        s.Rows = dto.WaffleRows ?? RestingRows;",
        "        s.Baseline = dto.Baseline ?? RestingBaseline;",
        "        s.Padding = dto.ChordPadding ?? ChordLayout.DefaultPadding;"] };

/// <summary>A document read back takes what it names and leaves the rest as the model started it: a reader that falls back on a
    /// number, a flag or an enum member it types itself restates the model's own start.</summary>
    private static readonly OwnedRule ReaderFallback = OwnedRule.Nowhere(
        "a reader of a record that names a setting by x ?? value reads the model's resting value by name: it never types a number, a flag or an enum member the model already holds",
        @"\b(?:Numerator|Base|LineStyle|StrokeJoin|Cycles|Position)\s*\?\?\s*" + RestingLiteral + @"(?![\w.(])",
        under: "Src/MatPlotLibNet/Serialization/") with { Finds = [
        "    internal override AxisTransform Restore() => AxisTransform.Reciprocal(Numerator ?? 1);",
        "    internal override AxisTransform Restore() => AxisTransform.Logarithm(Base ?? 10);",
        "        LineStyle = LineStyle ?? Styling.LineStyle.Solid,",
        "        Join = StrokeJoin ?? JoinStyle.Miter,"], Ignores = [
        "    internal override AxisTransform Restore() => AxisTransform.Reciprocal(Numerator ?? AxisTransform.RestingNumerator);",
        "        Join = StrokeJoin ?? StrokeLook.Library.Join,",
        "        LineStyle = LineStyle ?? SpineConfig.RestingLineStyle,"] };

/// <summary>A setting a series or a patch keeps while it is given and reads as its resting value while it is not (the given slot)
    /// reads the resting value by name.</summary>
    private static readonly OwnedRule GivenSlotFallback = OwnedRule.Nowhere(
        "the value a model's given slot (a series', a patch's) reads while nothing was given is its Resting constant: the getter never types a number, a flag or an enum member the model already holds",
        @"\b_\w+\s*\?\?\s*" + RestingLiteral + @"(?![\w.(])",
        under: "Src/MatPlotLibNet/Models/") with { Finds = [
        "    public double MarkerSize { get => _markerSize ?? 6; set => Change(ref _markerSize, value); }",
        "    public bool ShowWireframe { get => _wireframe ?? true; set => Change(ref _wireframe, value); }",
        "    public double MarkerSize { get => _markerSize ?? 36; set => Change(ref _markerSize, value); }",
        "        get => _alpha ?? 1.0;",
        "        get => _edgeColor ?? Colors.Transparent;"], Ignores = [
        "    public double MarkerSize { get => _markerSize ?? RestingMarkerSize; set => Change(ref _markerSize, value); }",
        "    public double LineWidth { get => _lineWidth ?? WidthRule.Resting; set => Change(ref _lineWidth, value); }",
        "    internal double OuterValue => _outer ?? 0;"] };

/// <summary>The marker a series draws where it names no size is 6 points: a line, a streaming, polar or ternary scatter, a Piper
    /// diagram, a 3-D scatter and stem, and the dot a residual plot or a pair grid's scatter cells rest at. A scatter's size is an area
    /// (36), another rule.</summary>
    private static readonly OwnedRule MarkerSizeAtRest = new(
        "the size of a marker a series draws where it names none is MarkerLook.RestingSize: no series types the 6 points again",
        @"\bdouble\s+MarkerSize\b.*(?:\?\?|\}\s*=)\s*6(?:\.0)?\b|\bRestingMarkerSize\s*=\s*6\b|\bRestingSize\s*=\s*6\b|\bRestingDot\s+\w+\s*=\s*new(?:\s*RestingDot)?\(\s*6(?:\.0)?\s*\)",
        "Src/MatPlotLibNet/Styling/Looks/MarkerLook.cs") { Under = "Src/MatPlotLibNet/", Finds = [
        "    public double MarkerSize { get => _markerSize ?? 6; set => Change(ref _markerSize, value); }",
        "    public double MarkerSize { get; set => Change(ref field, value); } = 6;",
        "    private const double RestingMarkerSize = 6;",
        "    private static readonly RestingDot Rests = new(6);"], Ignores = [
        "    public double MarkerSize { get; set => Change(ref field, value); } = MarkerLook.RestingSize;",
        "        var points = new ScatterSeries(pointX, pointY) { MarkerSize = 6, Alpha = 1 };",
        "    public double MarkerSize { get => _markerSize ?? 36; set => Change(ref _markerSize, value); }",
        "    private static readonly RestingDot Rests = new(MarkerLook.RestingSize);",
        "    private static readonly RestingDot Rests = new(8);",
        "    private static readonly RestingDot Rests = new(5, 0.8, 0.2);"] };

/// <summary>How big a figure is when it is told nothing.</summary>
    private static readonly OwnedRule FigureSizeAtRest = new(
        "the size of a figure that is told none is Figure.RestingWidth and Figure.RestingHeight: no entry point, template, reader or rc parameter types 800 by 600 again",
        @"(?<!Min)(?:[wW]idth|[hH]eight)\w*\]?\s*=\s*(?:800|600)\b|(?<!Min)(?:Width|Height)\s*\{.*\}\s*=\s*(?:800|600)\b"
        + @"|\\?""(?:width|height)\\?""\s*:\s*(?:800|600)\b",
        "Src/MatPlotLibNet/Models/Figure.cs") { Under = "Src/", Finds = [
        "    public static Figure Figure(double width = 800, double height = 600) =>",
        "        double width = 800, double height = 600)",
        "    private const double DefaultWidth = 800;",
        "        [RcParamKeys.FigureFigSizeHeight] = 600.0,",
        "    public double Height { get; set => Change(ref field, value); } = 600;",
        "          \"width\": 800,",
        "        [Description(\"e.g. {\\\"width\\\":800,\\\"height\\\":600,\\\"subPlots\\\":[]}\")]"], Ignores = [
        "    private const int MinWidth = 800;",
        "    public static Figure Figure(double width = Models.Figure.RestingWidth, double height = Models.Figure.RestingHeight) =>",
        "        builder.WithSize(600, 120 * n);"] };

/// <summary>The resolution a figure is told when it names none, and the one a PNG's dpi is measured against.</summary>
    private static readonly OwnedRule ReferenceDpiAtRest = new(
        "the resolution a figure starts at, and the one a PNG's dpi is measured against, is Figure.ReferenceDpi: no reader, size check or text of a tool types 96 again",
        @"[dD]pi\w*\W{0,8}96\b|\b96\s*[dD]pi\b|\b96 by default",
        "Src/MatPlotLibNet/Models/Figure.cs") { Under = "Src/", Finds = [
        "        [\"dpi\"] = \"the resolution, 96 by default: a PNG is drawn with dpi / 96 pixels\",",
        "    private const double DefaultDpi = 96;",
        "        double pixelsAcross = width * dpi / 96;",
        "    internal const int ReferenceDpi = 96;"], Ignores = [
        "        [\"dpi\"] = $\"the resolution, {Figure.ReferenceDpi} by default\",",
        "        double pixelsAcross = width * dpi / Figure.ReferenceDpi;",
        "        int scale = 96 * items;"] };

/// <summary>The steps a colour bar's gradient is drawn in where nothing says more.</summary>
    private static readonly OwnedRule ColorBarStepsAtRest = new(
        "the steps a colour bar's gradient is drawn in where the theme says nothing more are ColorBarStrip.RestingSteps: no renderer or factory types 50 again",
        @"\b(?:[sS]teps|RestingSteps)\s*=\s*50\b",
        "Src/MatPlotLibNet/Rendering/ColorBarRendering/ColorBarStrip.cs") { Under = "Src/", Finds = [
        "                                Rect plotArea, IRenderContext ctx, Theme theme, int steps = 50)",
        "    internal const int RestingSteps = 50;"] };

/// <summary>The curvature a connecting arc is drawn with where it is told none.</summary>
    private static readonly OwnedRule ConnectionRadAtRest = new(
        "the curvature of a connecting arc that is told none is Annotation.RestingConnectionRad: the model, the writer and each kind of connecting path read it, none types 0.3 again",
        @"\b(?:[rR]ad|ConnectionRad|RestingConnectionRad)\s*=\s*0\.3\b|\bConnectionRad\s*\{.*\}\s*=\s*0\.3\b",
        "Src/MatPlotLibNet/Models/Annotation.cs") { Under = "Src/", Finds = [
        "    internal abstract IReadOnlyList<PathSegment> Between(Point from, Point to, double rad = 0.3);",
        "    public double ConnectionRad { get; set => Change(ref field, value); } = 0.3;",
        "        double rad = 0.3;"] };

    /// <summary>The share a band or an interval covers where it is told no level.</summary>
    private static readonly OwnedRule ConfidenceLevelAtRest = new(
        "the confidence level of a band or an interval that is told none is ConfidenceBand.DefaultLevel: no series, template or data frame verb types 95 percent again",
        @"[lL]evel\w*\s*(?:=|==|!=)\s*0\.95\b|\bConfidenceLevel\s*\{.*\}\s*=\s*0\.95\b",
        "Src/MatPlotLibNet/Numerics/ConfidenceBand.cs") { Under = "Src/", Finds = [
        "    public double ConfidenceLevel { get; set => Change(ref field, value); } = 0.95;",
        "        double[] x, double[] y, double[] coefficients, double[] evalX, double level = 0.95)",
        "        ConfidenceLevel = ConfidenceLevel == 0.95 ? null : ConfidenceLevel,"] };

/// <summary>The share a band or an interval covers where it is told no level.</summary>
    /// <summary>How many points a line is thinned to when its downsampling is asked for with no number.</summary>
    private static readonly OwnedRule PointBudgetAtRest = new(
        "the point budget a line is thinned to when told none is XYSeries.DefaultPointBudget: no verb types 2000 again",
        @"\b(?:maxPoints|DefaultPointBudget)\s*=\s*[0-9]",
        "Src/MatPlotLibNet/Models/Series/XY/XYSeries.cs") { Under = "Src/", Finds = [
        "    public AxesBuilder WithDownsampling(int maxPoints = 2000)"] };

    private static readonly OwnedRule PointBudgetProse = OwnedRule.InDocumentation(
        "the prose of the point budget a line is thinned to when told none names XYSeries.DefaultPointBudget with a cref: no sentence types 2000 as its default again",
        @"\b[Dd]efault\w*(?:\s+is)?\s+2,?000\b",
        under: "Src/") with { Finds = [
        "    /// <param name=\"maxPoints\">Maximum number of points to display. Default is 2000.</param>"] };

/// <summary>How many points a streaming output keeps where it is told no capacity.</summary>
    private static readonly OwnedRule CapacityAtRest = OwnedRule.Nowhere(
        "the capacity of a streaming series or indicator that is told none is the Resting constant of its series (StreamingSeries.RestingCapacity for a line, a scatter and every indicator): no verb, reader or indicator types it again",
        @"\bcapacity\s*=\s*[0-9][0-9_]{3,}\b|\bCapacity\s*\?\?\s*[0-9][0-9_]{3,}\b",
        under: "Src/") with { Finds = [
        "    public StreamingAtr(int period = 14, int capacity = 10_000) : base(capacity)",
        "    public StreamingPlot(int capacity = 100_000, double sampleRate = 1.0)",
        "        var s = axes.AddSeries(new StreamingCandlestickSeries(dto.Capacity ?? 5_000));"], Ignores = [
        "    public StreamingAtr(int period = 14, int capacity = StreamingSeries.RestingCapacity) : base(capacity)",
        "internal sealed class GappedLine(int capacity = 0)"] };

/// <summary>The prose of a setting names where its start lives, never the number. A sentence that says "Default 10,000" beside a
    /// constant that owns the number is a second spelling the code scans cannot see: change the constant and the sentence is wrong. The
    /// sentence reads a <c>cref</c> to the owner instead, so the number is typed once. These rows read the XML documentation of the
    /// families whose start a constant owns: a streaming capacity, a figure's size, a histogram's bins, a marker's size, a confidence
    /// level, a colour bar's steps, a connecting arc's curvature and a signal's sample rate and start.</summary>
    private static readonly OwnedRule CapacityProse = OwnedRule.InDocumentation(
        "the prose of a streaming capacity names its Resting constant with a cref: no sentence types the number of points a streaming output keeps again",
        @"\bcapacity\b.*\b[Dd]efault\w*\s*\(?\s*[0-9][0-9,_]{3,}",
        under: "Src/") with { Finds = [
        "    /// <param name=\"capacity\">Maximum data points retained. Default 10,000.</param>",
        "    /// <param name=\"capacity\">Maximum samples retained. Default 100,000.</param>",
        "    /// <param name=\"capacity\">Maximum bars retained. Default 5,000.</param>"], Ignores = StartsNamedByACref };

    private static readonly OwnedRule FigureSizeProse = OwnedRule.InDocumentation(
        "the prose of a figure's width and height names Figure.RestingWidth and Figure.RestingHeight with a cref: no sentence types 800 or 600 as a default again",
        @"\b[Dd]efault\w*\s+(?:800|600)\b",
        under: "Src/") with { Finds = [
        "    /// <param name=\"width\">Figure width in pixels (default 800).</param>",
        "    /// <summary>The height of the figure in layout units. Default 600.</summary>"], Ignores = StartsNamedByACref };

    private static readonly OwnedRule BinsProse = OwnedRule.InDocumentation(
        "the prose of the bins of a histogram, of a joint plot's margins and of a pair plot's diagonal names their Resting constant with a cref: no sentence types the default count of bins again",
        @"\b(?:marginal distributions|diagonal panels|diagonal histograms|equal-width bins)\b.*\b[Dd]efault\w*\s+(?:<c>)?\d+\b",
        under: "Src/") with { Finds = [
        "    /// <summary>Number of histogram bins for the marginal distributions (default 30).</summary>",
        "    /// <summary>The number of equal-width bins the range of the data is divided into. Default 10.</summary>",
        "    /// <summary>Bin count for diagonal histograms. Default <c>20</c>.</summary>"], Ignores = StartsNamedByACref };

    private static readonly OwnedRule MarkerSizeProse = OwnedRule.InDocumentation(
        "the prose of a marker's size names MarkerLook.RestingSize with a cref: no sentence types the 6 points of a marker again, as its default or as the size that is drawn as given",
        @"\b[Mm]arker\b.*\b[Dd]efault\w*\s+6\b|\b6 (?:included|unless said otherwise|pixels under every other theme)\b",
        under: "Src/") with { Finds = [
        "    /// <summary>The size of each marker, in points. Default 6.</summary>",
        "    /// squared under the classic theme); a size that is given, 6 included, is drawn as given.</summary>",
        "    /// <summary>The diameter of a marker, in points; 6 unless said otherwise. Given no size, the marker is the size its theme says",
        "    /// default style and 4.47 under classic, and 6 pixels under every other theme; a size that is given, the default included.</summary>"], Ignores = StartsNamedByACref };

    private static readonly OwnedRule ConfidenceLevelProse = OwnedRule.InDocumentation(
        "the prose of a confidence level names ConfidenceBand.DefaultLevel with a cref: no sentence types 0.95 as a default again",
        @"\b[Dd]efault\w*\s+0\.95\b|\bleaves it out for 0\.95\b",
        under: "Src/") with { Finds = [
        "        /// <param name=\"level\">Confidence level (default 0.95).</param>",
        "    /// <summary>The confidence level of the band. Default 0.95 for a 95 percent band.</summary>",
        "    /// percent. A regression leaves it out for 0.95.</summary>"], Ignores = StartsNamedByACref };

    private static readonly OwnedRule ColorBarStepsProse = OwnedRule.InDocumentation(
        "the prose of a colour bar's gradient steps names ColorBarStrip.RestingSteps with a cref: no sentence types the 50 steps again",
        @"\b[Ss]teps\b.*\b[Dd]efault\w*\s+50\b",
        under: "Src/MatPlotLibNet/Rendering/ColorBarRendering/") with { Finds = [
        "    /// <summary>The number of gradient steps along the colorbar (default 50).</summary>"] };

    private static readonly OwnedRule ConnectionRadProse = OwnedRule.InDocumentation(
        "the prose of a connecting arc's curvature names Annotation.RestingConnectionRad with a cref: no sentence types 0.3 as its default again",
        @"\b[Dd]efault\w*\s+0\.3\b",
        under: "Src/MatPlotLibNet/Models/Annotation.cs") with { Finds = [
        "    /// it is the rounding of the corner as a fraction of the shorter leg. Default 0.3; the straight"] };

    private static readonly OwnedRule SignalStartProse = OwnedRule.InDocumentation(
        "the prose of a signal's sample rate and first X names the Resting constants of its series with a cref: no sentence types 1 or 0 as their defaults again",
        @"\b(?:sampleRate|xStart)\b.*\b[Dd]efaults?(?:\s+to)?\s+[01](?:\.0)?\b|\bsignal series leaves it out for 0\b",
        under: "Src/") with { Finds = [
        "    /// <param name=\"sampleRate\">Samples per X-unit. Default 1.0.</param>",
        "        /// <param name=\"xStart\">X value of the first sample. Defaults to 0.</param>",
        "    /// <summary>The X value of the first sample of a signal series; a signal series leaves it out for 0.</summary>"], Ignores = StartsNamedByACref };

    private static readonly OwnedRule GivenSlotOfAModel = OwnedRule.Nowhere(
        "a setting a model (a series, an axes part, a patch) keeps while it is given is one field-backed slot GivenX that the public setting reads over its Resting constant: no hidden field with a getter over it and an accessor beside it",
        HiddenGivenSlot, under: "Src/MatPlotLibNet/Models/") with { Finds = [
        "    internal double? GivenAlpha => _alpha;"] };

    private static readonly OwnedRule GivenSlotOfAnOption = OwnedRule.Nowhere(
        "a setting of a builder's options kept while it is given is one field-backed slot: no hidden field with a getter over it",
        HiddenGivenSlot, under: "Src/MatPlotLibNet/Builders/") with { Finds = [
        "        get => _maxLags ?? 10;"] };

    private static readonly OwnedRule GivenSlotOfAnIndicator = OwnedRule.Nowhere(
        "a setting of an indicator kept while it is given is one field-backed slot: no hidden field with a getter over it",
        HiddenGivenSlot, under: "Src/MatPlotLibNet/Indicators/") with { Finds = [
        "    public int Period { get => _period ?? RestingPeriod; set => _period = value; }"] };

/// <summary>How a value a theme's look names meets the value a caller gave and the one a model rests at is the look's own member.</summary>
    private static readonly OwnedRule GivenOverLook = OwnedRule.Nowhere(
        "how a value a theme's look names meets the value a caller gave and the value a model rests at is said by the look (VOver(given, resting), VOr(resting)): no model, renderer or layout writes given ?? look ?? resting itself",
        @"\?\?\s*\(?\s*!?\s*(?:Context\.)?[tT]heme\.Looks\.\w+\??\.\w+\s*(?:\?\?|[;,)])|\bLooks\.\w+\??\.\w+\s*\?\?", under: "Src/") with { Finds = [
        "        double width = series.LineWidth ?? theme.Looks.Lines.Width ?? RestingWidth;"] };

    private static readonly OwnedRule LeftOutAtRest = new(
        "a value the JSON leaves out while it is at rest goes through RestingValueExtensions (OrNullAtRest, DescribedUnlessAtRest): no writer compares a value, a record or a list with its rest and writes null itself",
        @"(?:==|!=)\s*(?:Rest|Plain|resting|Resting\w*)(?:\.\w+)*\s*\?\s*(?:null\s*:|[^:;]*:\s*null\b)|SequenceEqual\([^)]*\)\s*\?\s*null",
        "Src/MatPlotLibNet/Serialization/RestingValueExtensions.cs") { Under = "Src/MatPlotLibNet/", Finds = [
        "        CycledProperties?.LineWidth is { } width && width != RestingLineWidth ? width : null;"] };

    /// <summary>The rules of the area, in the order the scan reads them.</summary>
    internal static OwnedRule[] All { get; } =
    [
        DefaultArgumentTyped,
        RestingValueNamed,
        ReaderFallback,
        GivenSlotFallback,
        MarkerSizeAtRest,
        FigureSizeAtRest,
        ReferenceDpiAtRest,
        ColorBarStepsAtRest,
        ConnectionRadAtRest,
        ConfidenceLevelAtRest,
        PointBudgetAtRest,
        PointBudgetProse,
        CapacityAtRest,
        CapacityProse,
        FigureSizeProse,
        BinsProse,
        MarkerSizeProse,
        ConfidenceLevelProse,
        ColorBarStepsProse,
        ConnectionRadProse,
        SignalStartProse,
        GivenSlotOfAModel,
        GivenSlotOfAnOption,
        GivenSlotOfAnIndicator,
        GivenOverLook,
        LeftOutAtRest,
    ];
}
