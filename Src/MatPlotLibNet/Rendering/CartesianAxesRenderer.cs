// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>Renders axes using the standard Cartesian (X, Y) coordinate system.</summary>
/// <remarks>Not sealed so that inset axes can answer the end-label question differently
/// (<c>CartesianInsetAxesRenderer</c>); the drawing itself (<see cref="Render"/>) is sealed.</remarks>
public class CartesianAxesRenderer : AxesRenderer
{
    /// <summary>Initializes a new Cartesian axes renderer.</summary>
    public CartesianAxesRenderer(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme)
        : base(axes, plotArea, ctx, theme) { }

    /// <inheritdoc />
    /// <remarks>Cartesian axes draw end-of-line labels, so a series that asked for one leaves the legend here.</remarks>
    internal override bool DrawsDirectLabels => true;

    /// <summary>The end labels of the axes being rendered; placed by <see cref="Render"/> before the series pass.</summary>
    private CartesianParts.CartesianDirectLabelsPart? _directLabels;

    /// <inheritdoc />
    /// <remarks>Draws the series' end label, when it asked for one, inside the series' own group.</remarks>
    internal override void DrawInsideSeriesGroup(int seriesIndex) =>
        _directLabels!.DrawLabelOf(SeriesAxisPair.Primary, seriesIndex);

    /// <summary>The band the mirrored Y tick labels took past the right spine in this render; 0 when none were drawn.
    /// The end labels start past it.</summary>
    private double _mirroredYTickBand;

    /// <summary>Whether a secondary Y axis is drawn: it has series. A drawn secondary axis owns the right edge.</summary>
    private bool DrawsSecondaryY => Axes.SecondaryYAxis is not null && Axes.SecondarySeries.Count > 0;

    /// <summary>Whether a secondary X axis is drawn: it has series. A drawn secondary axis owns the top edge.</summary>
    private bool DrawsSecondaryX => Axes.SecondaryXAxis is not null && Axes.XSecondarySeries.Count > 0;

    /// <summary>Whether the primary X ticks are mirrored on the top edge: asked for, and no drawn secondary X axis owns
    /// that edge (matplotlib's <c>twiny</c> moves the host's ticks to the bottom the same way).</summary>
    private bool MirrorsXTicks => Axes.XAxis.MajorTicks.Mirror && !DrawsSecondaryX;

    /// <summary>Whether the primary Y ticks are mirrored on the right edge: asked for, and no drawn secondary Y axis owns
    /// that edge (matplotlib's <c>twinx</c> moves the host's ticks to the left the same way).</summary>
    private bool MirrorsYTicks => Axes.YAxis.MajorTicks.Mirror && !DrawsSecondaryY;

    /// <summary>Whether every series fills the plot without Cartesian axes (radar, pie, treemap, table): then no grid,
    /// ticks or frame are drawn.</summary>
    private bool IsAxisLess => Axes.Series.Count > 0 && Axes.Series.All(s =>
        s is RadarSeries or PieSeries or HierarchicalSeries or TableSeries or TreeGridSeries);

    /// <inheritdoc />
    /// <remarks>Measured with what <see cref="Render"/> draws them with: the same predicates, tick values (the Y ticks
    /// for this renderer's plot height), formatter, label font and band rule
    /// (<see cref="AxisTickExtensions.TickLabelBand"/>). A tick label's height depends on its font only, so the
    /// mirrored top band is measured on one label. Categorical X labels are drawn without a mirror, but their band is
    /// still charged: a reservation that is too wide never clips. A drawn secondary X axis owns the top edge, so the top
    /// band is then its band (<see cref="CartesianParts.CartesianSecondaryXAxisPart.MeasureBand"/>), which it draws
    /// whatever the series.</remarks>
    internal override Layout.EdgeBands MeasureEdgeBands()
    {
        double secondaryTop = DrawsSecondaryX
            ? CartesianParts.CartesianSecondaryXAxisPart.MeasureBand(Axes, Ctx, Theme, ComputeSecondaryXDataRanges(0, 1))
            : 0;
        if (IsAxisLess) return new Layout.EdgeBands(secondaryTop, 0);
        var baseFont = TickFont();
        var xMajor = Axes.XAxis.MajorTicks;
        double top = MirrorsXTicks && xMajor.Visible
            ? xMajor.TickLabelBand(Ctx.MeasureText("0", xMajor.LabelFont(baseFont)).Height)
            : secondaryTop;
        var yMajor = Axes.YAxis.MajorTicks;
        double right = 0;
        if (MirrorsYTicks && yMajor.Visible)
        {
            var yTicks = PrimaryYTicks(ComputeDataRanges());
            var uniform = BuildUniformTickFormatter(yTicks);
            var formatter = PrimaryYFormatter;
            var font = yMajor.LabelFont(baseFont);
            double widest = 0;
            foreach (var tick in yTicks)
                widest = Math.Max(widest, Ctx.MeasureText(formatter?.Format(tick) ?? uniform(tick), font).Width);
            right = yMajor.TickLabelBand(widest);
        }
        return new Layout.EdgeBands(top, right);
    }

    /// <summary>The tick locator a scale brings when the axis has none: <see cref="AutoDateLocator"/> for a date X
    /// axis, <see cref="SymlogLocator"/> for symlog, <see cref="LogLocator"/> for log; <see langword="null"/>
    /// otherwise.</summary>
    private static ITickLocator? ScaleLocator(Axis axis, bool isX) => axis.Scale switch
    {
        AxisScale.Date when isX => new AutoDateLocator(),
        AxisScale.SymLog        => new SymlogLocator(axis.SymLogLinThresh),
        AxisScale.Log           => new LogLocator(),
        _                       => null,
    };

    /// <summary>The formatter that comes with a scale's locator: <see cref="AutoDateFormatter"/> with the date
    /// locator, <see cref="LogTickFormatter"/> with the log locator; <see langword="null"/> otherwise.</summary>
    private static ITickFormatter? ScaleFormatter(ITickLocator? locator) => locator switch
    {
        AutoDateLocator date => new AutoDateFormatter(date),
        LogLocator           => new LogTickFormatter(),
        _                    => null,
    };

    /// <summary>Installs the locator and formatter the axis' scale brings when the axis has no locator; the axis'
    /// own formatter is kept.</summary>
    private static void InstallScaleTicks(Axis axis, bool isX)
    {
        if (axis.TickLocator is not null) return;
        axis.TickLocator = ScaleLocator(axis, isX);
        axis.TickFormatter ??= ScaleFormatter(axis.TickLocator);
    }

    /// <summary>The primary Y tick values the axes draws for <paramref name="range"/>: placed by the axis' locator or
    /// the one its scale brings, otherwise by the default placement for this renderer's plot height; ticks inside a Y
    /// break are left out. Read before anything is installed (layout) and after (drawing) alike.</summary>
    private double[] PrimaryYTicks(DataRange range)
    {
        var axis = Axes.YAxis;
        var ticks = (axis.TickLocator ?? ScaleLocator(axis, isX: false)) is { } locator
            ? locator.LocateInterval(range.YMin, range.YMax)
            : ComputeTickValues(range.YMin, range.YMax, axis, PlotArea.Height);
        return Axes.YBreaks.Count > 0
            ? ticks.Where(t => !AxisBreakMapper.IsInBreak(Axes.YBreaks, t)).ToArray()
            : ticks;
    }

    /// <summary>The formatter of the primary Y tick labels: the axis' own, or the one its scale brings while the scale
    /// has not installed it yet.</summary>
    private ITickFormatter? PrimaryYFormatter => Axes.YAxis.TickFormatter
        ?? (Axes.YAxis.TickLocator is null ? ScaleFormatter(ScaleLocator(Axes.YAxis, isX: false)) : null);

    /// <inheritdoc />
    public sealed override void Render()
    {
        var axesBg = Theme.AxesBackground;
        Ctx.DrawRectangle(PlotArea, new ShapeStyle(axesBg, null, 0));

        // Compute data ranges, and the transform the primary series are drawn with
        var range = ComputeDataRanges();
        var transform = ViewTransform(range);
        DrawnTransform = transform;

        // A scale with its own tick placement installs it when the axis has no locator: AutoDateLocator +
        // AutoDateFormatter for a date X axis (handles legacy SetXDateFormat() calls that only set scale + formatter),
        // SymlogLocator for symlog (matplotlib's set_yscale("symlog") installs SymmetricalLogLocator), LogLocator +
        // LogTickFormatter for log (without that arm a log axis wore LINEAR ticks 0, 25, 50 … over a logarithmic
        // transform, measured 2026-08-30).
        InstallScaleTicks(Axes.XAxis, isX: true);
        InstallScaleTicks(Axes.YAxis, isX: false);

        // Compute tick values once for grid + ticks (respects TickLocator / Spacing / plot size)
        var xTicks = ComputeTickValues(range.XMin, range.XMax, Axes.XAxis, PlotArea.Width);
        var yTicks = PrimaryYTicks(range);

        // Filter out ticks that fall strictly inside any break region — they would render
        // in empty space (or on top of the break marker).
        if (Axes.XBreaks.Count > 0)
            xTicks = xTicks.Where(t => !AxisBreakMapper.IsInBreak(Axes.XBreaks, t)).ToArray();

        // Skip standard grid/ticks/frame for axis-less series (radar, pie, treemap, table).
        // These fill the plot area and don't use a Cartesian coordinate system.
        bool radarOnly = IsAxisLess;
        if (!radarOnly)
        {
            // Grid — axes-level setting overrides theme when explicitly set (Visible=true);
            // otherwise fall back to Theme.DefaultGrid so the theme controls the default.
            // An axes that asked for something gets the theme's grid WITH that change applied; one that asked
            // nothing gets the theme's grid whole. The older reading — the axes grid when it happened to be
            // visible, the theme's otherwise — dropped a change that did not mention visibility, and could not
            // express "no grid" at all under a theme that draws one.
            var effectiveGrid = Axes.GridOverride is { } change
                ? change(Theme.DefaultGrid)
                : (Axes.Grid.Visible ? Axes.Grid : Theme.DefaultGrid);
            if (effectiveGrid.Visible)
                RenderGrid(xTicks, yTicks, range.YMin, transform, effectiveGrid);

            // Tick marks and labels
            RenderTicks(xTicks, yTicks, range.YMin, transform);
        }

        // Render span regions (Phase B.5 — extracted to CartesianSpansPart)
        new CartesianParts.CartesianSpansPart(Axes, PlotArea, Ctx, Theme, transform, range).Render();

        // Render reference lines (Phase B.6 — extracted to CartesianReferenceLinesPart)
        new CartesianParts.CartesianReferenceLinesPart(Axes, PlotArea, Ctx, Theme, transform, range).Render();

        // Render financial drawing tools (trendlines, levels, Fibonacci retracements)
        new CartesianParts.CartesianToolsPart(Axes, PlotArea, Ctx, Theme, transform).Render();

        // Compute stacked baselines if needed
        if (Axes.BarMode == BarMode.Stacked)
        {
            var stackableList = Axes.Series.OfType<IStackable>().ToList();
            if (stackableList.Count > 1)
            {
                int catCount = stackableList[0].Values.Length;
                var cumulative = new double[catCount];
                foreach (var s in stackableList)
                {
                    s.StackBaseline = (double[])cumulative.Clone();
                    for (int c = 0; c < Math.Min(catCount, s.Values.Length); c++)
                        cumulative[c] += s.Values[c];
                }
            }
        }

        // The secondary Y transform is built ONCE: the secondary part draws its lines with it and the end-label part
        // places a secondary line's label with the same value.
        DataRange secYRange = default;
        DataTransform? secYTransform = null;
        if (DrawsSecondaryY)
        {
            secYRange = ComputeSecondaryDataRanges(range.XMin, range.XMax);
            secYTransform = new DataTransform(secYRange.XMin, secYRange.XMax, secYRange.YMin, secYRange.YMax, PlotArea);
        }

        // End-of-line labels: placed once, across every series, before any is drawn. Each label is drawn inside its own
        // series' group — by the primary pass (DrawInsideSeriesGroup) or the secondary Y part — so whatever hides the
        // series hides its label.
        _directLabels = new CartesianParts.CartesianDirectLabelsPart(Axes, PlotArea, Ctx, Theme, transform,
            secYTransform, new MinMaxRange(range.XMin, range.XMax), _mirroredYTickBand);

        // Series
        RenderSeries(transform);

        // Axis break markers (drawn on top of series, overlaying the break region)
        if (Axes.XBreaks.Count > 0) RenderBreakMarkers(Axes.XBreaks, transform, isXAxis: true);
        if (Axes.YBreaks.Count > 0) RenderBreakMarkers(Axes.YBreaks, transform, isXAxis: false);

        // Secondary Y-axis series (Phase B.7 — extracted to CartesianSecondaryYAxisPart)
        if (secYTransform is not null)
        {
            new CartesianParts.CartesianSecondaryYAxisPart(
                Axes, PlotArea, Ctx, Theme, transform, secYRange, secYTransform, _directLabels).Render();
        }

        // Secondary X-axis series (Phase B.8 — extracted to CartesianSecondaryXAxisPart). It owns the top edge, so the
        // band it draws there is what the axes title rises by.
        if (DrawsSecondaryX)
        {
            var secXRange = ComputeSecondaryXDataRanges(range.YMin, range.YMax);
            var secondaryX = new CartesianParts.CartesianSecondaryXAxisPart(
                Axes, PlotArea, Ctx, Theme, transform, secXRange);
            secondaryX.Render();
            MeasuredTopTickBand = secondaryX.Band;
        }

        // Annotations (Phase B.4 — extracted to CartesianAnnotationsPart via Composite pattern)
        new CartesianParts.CartesianAnnotationsPart(Axes, PlotArea, Ctx, Theme, transform).Render();

        // Signal markers (Phase B.9 — extracted to CartesianSignalsPart)
        new CartesianParts.CartesianSignalsPart(Axes, PlotArea, Ctx, Theme, transform).Render();

        // Axes spines — rendered after series so they appear on top of fills/areas
        if (!radarOnly)
            RenderSpines(transform);

        // Legend
        RenderLegend();

        // ColorBar
        RenderColorBar();

        // Axes title
        RenderTitle();

        // Axis labels
        RenderAxisLabels();
    }

    /// <inheritdoc />
    /// <remarks>The range is <see cref="ComputeDataRanges"/>' and the transform is built by <see cref="ViewTransform"/>,
    /// the two calls <see cref="Render"/> makes before it draws anything.</remarks>
    internal override DataTransform ComputeViewTransform() => ViewTransform(ComputeDataRanges());

    /// <summary>The transform that maps <paramref name="range"/> onto the plot area: axis breaks compress it, and on a
    /// log or symlog axis the pixel mapping works in the scaled space (<see cref="AxisScaleExtensions.Forward"/>) so
    /// that ticks at 100, 1000, 10000 land evenly spaced.</summary>
    private DataTransform ViewTransform(DataRange range)
    {
        // Apply axis breaks: compress the visible range by removing break gaps
        var (cXMin, cXMax) = Axes.XBreaks.Count > 0
            ? AxisBreakMapper.CompressedRange(Axes.XBreaks, range.XMin, range.XMax)
            : new MinMaxRange(range.XMin, range.XMax);
        var (cYMin, cYMax) = Axes.YBreaks.Count > 0
            ? AxisBreakMapper.CompressedRange(Axes.YBreaks, range.YMin, range.YMax)
            : new MinMaxRange(range.YMin, range.YMax);

        var (txMin, txMax) = ScaleRange(cXMin, cXMax, Axes.XAxis.Scale, Axes.XAxis.SymLogLinThresh);
        var (tyMin, tyMax) = ScaleRange(cYMin, cYMax, Axes.YAxis.Scale, Axes.YAxis.SymLogLinThresh);

        return (Axes.XBreaks.Count > 0 || Axes.YBreaks.Count > 0
                || Axes.XAxis.Scale is AxisScale.Log or AxisScale.SymLog
                || Axes.YAxis.Scale is AxisScale.Log or AxisScale.SymLog)
            ? new DataTransform(txMin, txMax, tyMin, tyMax, PlotArea,
                Axes.XBreaks, Axes.YBreaks,
                range.XMin, range.XMax, range.YMin, range.YMax,
                Axes.XAxis.Scale, Axes.YAxis.Scale, Axes.XAxis.SymLogLinThresh, Axes.YAxis.SymLogLinThresh)
            : new DataTransform(cXMin, cXMax, cYMin, cYMax, PlotArea);
    }

    /// <summary>Maps a (raw) data range through the axis scale's Forward function so the
    /// resulting scaled range can be used by <see cref="DataTransform"/> for pixel scaling.
    /// Linear scale is identity; SymLog applies <see cref="SymlogTransform.Forward"/>; Log
    /// applies log10. Returns the scaled (min, max) pair.</summary>
    private static MinMaxRange ScaleRange(double min, double max, AxisScale scale, double linthresh) =>
        new(scale.Forward(min, linthresh), scale.Forward(max, linthresh));

    /// <summary>Renders major and/or minor grid lines behind the plot area at each tick position.</summary>
    private void RenderGrid(double[] xTicks, double[] yTicks, double yMin, DataTransform transform, GridStyle grid)
    {
        var gridColor = grid.Color;
        var gridStyle = grid.LineStyle;
        double gridWidth = grid.LineWidth;
        bool drawX = grid.Axis is GridAxis.X or GridAxis.Both;
        bool drawY = grid.Axis is GridAxis.Y or GridAxis.Both;
        double xMin = Axes.XAxis.Min ?? 0;

        if (grid.Which is GridWhich.Major or GridWhich.Both)
        {
            if (drawX) RenderGridLines(Orientation.Horizontal, xTicks, yMin,   transform, gridColor, gridWidth, gridStyle, minor: false);
            if (drawY) RenderGridLines(Orientation.Vertical,   yTicks, xMin,   transform, gridColor, gridWidth, gridStyle, minor: false);
        }

        if (grid.Which is GridWhich.Minor or GridWhich.Both)
        {
            double minorWidth = gridWidth * 0.5;
            var minorColor = gridColor.WithAlpha((byte)(gridColor.A / 2));
            if (drawX) RenderGridLines(Orientation.Horizontal, xTicks, yMin, transform, minorColor, minorWidth, gridStyle, minor: true);
            if (drawY) RenderGridLines(Orientation.Vertical,   yTicks, xMin, transform, minorColor, minorWidth, gridStyle, minor: true);
        }
    }

    /// <summary>Renders tick marks and tick labels along the X and Y axes, including minor ticks when enabled.</summary>
    private void RenderTicks(double[] xTicks, double[] yTicks, double yMin, DataTransform transform)
    {
        // --- X axis ticks ---
        var xMajor = Axes.XAxis.MajorTicks;
        var xMinor = Axes.XAxis.MinorTicks;
        var yMajor = Axes.YAxis.MajorTicks;
        var baseFont = TickFont();

        // Don't treat a BarSeries with numeric XCoordinate as categorical — it opts into a
        // continuous X axis and shares it with companion line series (e.g. MACD histogram
        // alongside MACD/signal lines).
        var categoryLabeled = Axes.Series
            .OfType<ICategoryLabeled>()
            .Where(s => s is not BarSeries b || b.XCoordinate is null)
            .FirstOrDefault(s => s.CategoryLabels is not null);
        // When a custom TickLocator is set it takes priority — use the standard tick path so the
        // locator controls spacing (e.g. MultipleLocator(5,0.5) for bar charts with many bars).
        if (categoryLabeled?.CategoryLabels is not null && Axes.XAxis.TickLocator is null)
            RenderCategoryLabels(categoryLabeled.CategoryLabels, yMin, transform);
        else if (!xMajor.Visible)
        {
            // HideAllAxes / explicit TickConfig.Visible=false → draw no tick marks or labels.
            MeasuredXTickMaxHeight = 0;
        }
        else
        {
            var xFormatter = Axes.XAxis.TickFormatter;
            var xUniformFormat = BuildUniformTickFormatter(xTicks);
            double xAxisY = PlotArea.Y + PlotArea.Height;
            double xSpineHalf = Axes.Spines.Bottom.LineWidth / 2.0;
            // Phase L.8 — resolve effective X-label rotation. Manual setting wins; when
            // no manual rotation and adjacent tick labels would overlap, default to 30°
            // (matplotlib Figure.autofmt_xdate parity). We use label text + font width +
            // spacing to detect the collision and only rotate when it matters.
            double xRotation = ResolveXLabelRotation(xTicks, xMajor.LabelRotation, xFormatter, xUniformFormat,
                xMajor.LabelFont(baseFont), transform, yMin);
            var xDraw = new TickDrawContext
            {
                Side          = TickSide.Bottom,
                AxisEdge      = xAxisY,
                SpineHalf     = xSpineHalf,
                BaseFont      = baseFont,
                Formatter     = xFormatter,
                UniformFormat = xUniformFormat,
                Foreground    = Theme.ForegroundText,
                LabelRotation = xRotation,
                Alignment     = xRotation == 0 ? TextAlignment.Center : TextAlignment.Right,
            };
            MeasuredXTickMaxHeight = Ctx.RenderAxisTicks(xTicks, yMin, transform, xMajor, xDraw);

            // Mirrored X ticks on the top spine: the same ticks, labels above the edge. A rotated label is anchored
            // at its start so it rises away from the axis, the point reflection of the bottom label. The band they
            // take is what the axes title rises by.
            if (MirrorsXTicks)
            {
                MeasuredTopTickBand = xMajor.TickLabelBand(Ctx.RenderAxisTicks(xTicks, yMin, transform, xMajor, xDraw with
                {
                    Side      = TickSide.Top,
                    AxisEdge  = PlotArea.Y,
                    SpineHalf = Axes.Spines.Top.LineWidth / 2.0,
                    Alignment = xRotation == 0 ? TextAlignment.Center : TextAlignment.Left,
                }));
            }

            // Minor X ticks (no labels, shorter mark)
            RenderMinorTicks(xTicks, xMinor, isHorizontal: true, transform, xAxisY, xSpineHalf, yMin);
        }

        // --- Y axis ticks ---
        double yAxisX = PlotArea.X;
        double ySpineHalf = Axes.Spines.Left.LineWidth / 2.0;
        var yDraw = new TickDrawContext
        {
            Side          = TickSide.Left,
            AxisEdge      = yAxisX,
            SpineHalf     = ySpineHalf,
            BaseFont      = baseFont,
            Formatter     = PrimaryYFormatter,
            UniformFormat = BuildUniformTickFormatter(yTicks),
            Foreground    = Theme.ForegroundText,
            LabelRotation = yMajor.LabelRotation,
            Alignment     = TextAlignment.Right,
        };
        MeasuredYTickMaxWidth = Ctx.RenderAxisTicks(yTicks, Axes.XAxis.Min ?? 0, transform, yMajor, yDraw);

        // Mirrored Y ticks on the right spine: the same ticks, labels past the right edge. The end labels start past
        // the band they take.
        if (MirrorsYTicks)
        {
            _mirroredYTickBand = yMajor.TickLabelBand(Ctx.RenderAxisTicks(yTicks, Axes.XAxis.Min ?? 0, transform, yMajor, yDraw with
            {
                Side      = TickSide.Right,
                AxisEdge  = PlotArea.X + PlotArea.Width,
                SpineHalf = Axes.Spines.Right.LineWidth / 2.0,
                Alignment = TextAlignment.Left,
            }));
        }

        // Minor Y ticks (no labels, shorter mark)
        var yMinor = Axes.YAxis.MinorTicks;
        RenderMinorTicks(yTicks, yMinor, isHorizontal: false, transform, yAxisX, ySpineHalf, Axes.XAxis.Min ?? 0);
    }

    /// <summary>Draws minor tick marks for one axis, skipping positions that coincide with a major tick.
    /// Shared by the X path (<paramref name="isHorizontal"/> = true) and Y path (false), eliminating
    /// the two near-identical 16-line loops that previously lived in <see cref="RenderTicks"/>.</summary>
    private void RenderMinorTicks(
        double[] majorTicks,
        TickConfig minorConfig,
        bool isHorizontal,
        DataTransform transform,
        double axisEdge,
        double spineHalf,
        double fixedCoord)
    {
        if (!minorConfig.Visible || majorTicks.Length < 2) return;
        var color = minorConfig.Color ?? Theme.ForegroundText;
        double minorLength = minorConfig.Length;
        double majorStep = majorTicks[1] - majorTicks[0];
        // The caller's own spacing wins when it set one; otherwise matplotlib's five-way subdivision, which is
        // what every axis that has not asked for anything still gets. A non-positive value is a computed
        // spacing that came out wrong, never an instruction — it falls back rather than dividing by zero.
        double minorStep = minorConfig.Spacing is { } spacing && spacing > 0 ? spacing : majorStep / 5;
        double rangeStart = majorTicks[0] - majorStep;
        double rangeEnd   = majorTicks[^1] + majorStep;
        double areaMin = isHorizontal ? PlotArea.X : PlotArea.Y;
        double areaMax = isHorizontal ? PlotArea.X + PlotArea.Width : PlotArea.Y + PlotArea.Height;
        for (double mt = rangeStart; mt <= rangeEnd + minorStep * 0.01; mt += minorStep)
        {
            mt = Math.Round(mt, 10);
            if (Array.Exists(majorTicks, t => Math.Abs(t - mt) < minorStep * 0.01)) continue;
            var pt = isHorizontal ? transform.DataToPixel(mt, fixedCoord) : transform.DataToPixel(fixedCoord, mt);
            double pos = isHorizontal ? pt.X : pt.Y;
            if (pos < areaMin || pos > areaMax) continue;
            Ctx.DrawTickMark(pos, axisEdge, isHorizontal ? TickSide.Bottom : TickSide.Left,
                minorLength, color, minorConfig.Width, minorConfig.Direction, spineHalf);
        }
    }

    /// <summary>
    /// Phase L.8 — resolves the effective X-tick-label rotation. A non-zero user-set
    /// <paramref name="manualRotation"/> always wins. Otherwise, if adjacent label
    /// rectangles would overlap given the current tick positions and label widths,
    /// returns 30° (matches matplotlib's <c>Figure.autofmt_xdate</c> auto-rotate
    /// behaviour). Returns 0 when horizontal labels fit comfortably.
    /// </summary>
    private double ResolveXLabelRotation(
        double[] xTicks, double manualRotation,
        ITickFormatter? formatter, Func<double, string> uniformFormat,
        Font font, DataTransform transform, double yMin)
    {
        if (manualRotation != 0) return manualRotation;
        if (xTicks.Length < 2)   return 0;

        // Measure every label once; find the max width, then check whether the
        // tick-spacing in pixels is at least max-width + 4 px padding. If any pair
        // collides we return 30°.
        const double GapPx = 4;
        double maxLabelWidth = 0;
        var pixelXs = new double[xTicks.Length];
        for (int i = 0; i < xTicks.Length; i++)
        {
            var labelText = formatter?.Format(xTicks[i]) ?? uniformFormat(xTicks[i]);
            var w = Ctx.MeasureText(labelText, font).Width;
            if (w > maxLabelWidth) maxLabelWidth = w;
            pixelXs[i] = transform.DataToPixel(xTicks[i], yMin).X;
        }

        for (int i = 1; i < pixelXs.Length; i++)
        {
            double spacing = Math.Abs(pixelXs[i] - pixelXs[i - 1]);
            if (spacing < maxLabelWidth + GapPx) return 30;
        }
        return 0;
    }

    /// <summary>Renders category labels centered below each category position on the X-axis.</summary>
    private void RenderCategoryLabels(string[] labels, double yMin, DataTransform transform)
    {
        var tickFont = TickFont();
        // Draw an x-axis tick MARK at each category centre (matplotlib draws tick marks on the
        // bottom spine even for categorical bar charts — they anchor each label to the spine).
        var xMajor = Axes.XAxis.MajorTicks;
        var xTickColor  = xMajor.Color ?? Theme.ForegroundText;
        var xTickLength = xMajor.Length;
        var xTickWidth  = xMajor.Width;
        double xAxisY = PlotArea.Y + PlotArea.Height;
        double xSpineHalf = Axes.Spines.Bottom.LineWidth / 2.0;

        for (int i = 0; i < labels.Length; i++)
        {
            // Bar slot [i, i+1] has its center at i+0.5 — place the label there.
            var pt = transform.DataToPixel(i + 0.5, yMin);
            Ctx.DrawTickMark(pt.X, xAxisY, TickSide.Bottom, xTickLength, xTickColor, xTickWidth, xMajor.Direction, xSpineHalf);
            Ctx.DrawText(labels[i],
                new Point(pt.X, PlotArea.Y + PlotArea.Height + 15),
                tickFont, TextAlignment.Center);
        }
    }

    /// <summary>
    /// Computes the combined X and Y data ranges across all series on the axes, including
    /// shared axes' ranges when <c>sharex</c>/<c>sharey</c> is configured. The heavy lifting
    /// lives in <see cref="Range1D"/> (pipeline methods) and <see cref="AxesRangeExtensions"/>
    /// (aggregation helpers). Exposed as <c>internal</c> so regression tests can assert the
    /// padding / sticky-edge / nice-bound interaction without rendering to SVG.
    /// </summary>
    internal DataRange ComputeDataRanges()
    {
        // === 1. Aggregate — seed from axis-level limits, fold in own + shared series; a set limit is exact ===
        var xRange = Axes.AggregateXRangeWithSharedAxes(Range1D.FromAxis(Axes.XAxis)).WithExplicitLimits(Axes.XAxis);
        var yRange = Axes.AggregateYRangeWithSharedAxes(Range1D.FromAxis(Axes.YAxis)).WithExplicitLimits(Axes.YAxis);

        // === 2. Normalize — handle empty / lopsided / zero-width inputs before any math ===
        xRange = xRange.Normalized().PositiveForLog(Axes.XAxis);
        yRange = yRange.Normalized().PositiveForLog(Axes.YAxis);

        // === 3. Snapshot contributions once, then drive padding + sticky + nice-bound ===
        // Calling ComputeDataRange per series is expensive (Histogram rebuilds bins each call),
        // so the legacy code's "iterate three times" pattern is replaced by one snapshot.
        var contribs = Axes.SnapshotContributions();

        // The unpadded snapshot is captured BEFORE padding so sticky clamp can distinguish
        // "padding pushed past the sticky" (snap back) from "another series has data past the
        // sticky" (preserve, an overlay's sticky must not clip a wider underlying series).
        var xUnpadded = xRange;
        var yUnpadded = yRange;

        xRange = xRange.Padded(Axes.XAxis.Margin ?? Theme.AxisXMargin, Axes.XAxis);
        yRange = yRange.Padded(Axes.YAxis.Margin ?? Theme.AxisYMargin, Axes.YAxis);

        foreach (var c in contribs)
        {
            xRange = xRange.ClampSticky(c.StickyXMin, c.StickyXMax, xUnpadded, Axes.XAxis);
            yRange = yRange.ClampSticky(c.StickyYMin, c.StickyYMax, yUnpadded, Axes.YAxis);
        }

        var locator = new MatPlotLibNet.Rendering.TickLocators.AutoLocator();
        xRange = xRange.ExpandedToNiceBoundsIfAuto(Axes.XAxis, xUnpadded, contribs.HasAnyStickyX(), locator);
        yRange = yRange.ExpandedToNiceBoundsIfAuto(Axes.YAxis, yUnpadded, contribs.HasAnyStickyY(), locator);

        return new DataRange(xRange.Lo, xRange.Hi, yRange.Lo, yRange.Hi);
    }

    /// <summary>Computes the Y data range for series plotted against the secondary Y-axis.</summary>
    internal DataRange ComputeSecondaryDataRanges(double xMin, double xMax)
    {
        double yMin = Axes.SecondaryYAxis?.Min ?? double.MaxValue;
        double yMax = Axes.SecondaryYAxis?.Max ?? double.MinValue;

        var context = new AxesContextAdapter(Axes);
        foreach (var series in Axes.SecondarySeries)
        {
            // ISeries.ComputeDataRange, not an `is IHasDataRange` test: every series carries the member, but
            // not every one carries that separate marker interface — a streaming series does not, so the test
            // skipped it, the range stayed at its 0..1 sentinel and the trace was drawn off the plot area
            // while the primary path (SnapshotContributions) had ranged it correctly all along.
            var c = series.ComputeDataRange(context);
            if (c.YMin.HasValue && c.YMin.Value < yMin) yMin = c.YMin.Value;
            if (c.YMax.HasValue && c.YMax.Value > yMax) yMax = c.YMax.Value;
        }

        // A set limit is exact, the same rule the primary axes follow (Range1D.WithExplicitLimits).
        var exact = new Range1D(yMin, yMax).WithExplicitLimits(Axes.SecondaryYAxis!);
        yMin = exact.Lo;
        yMax = exact.Hi;

        if (yMin == double.MaxValue) { yMin = 0; yMax = 1; }
        if (Math.Abs(yMax - yMin) < 1e-10) { yMin -= 0.5; yMax += 0.5; }

        double yPadding = (yMax - yMin) * 0.05;
        if (!Axes.SecondaryYAxis!.Min.HasValue) yMin -= yPadding;
        if (!Axes.SecondaryYAxis!.Max.HasValue) yMax += yPadding;

        return new DataRange(xMin, xMax, yMin, yMax);
    }

    /// <summary>Computes the X data range for series plotted against the secondary X-axis.</summary>
    internal DataRange ComputeSecondaryXDataRanges(double yMin, double yMax)
    {
        double xMin = Axes.SecondaryXAxis?.Min ?? double.MaxValue;
        double xMax = Axes.SecondaryXAxis?.Max ?? double.MinValue;

        var context = new AxesContextAdapter(Axes);
        foreach (var series in Axes.XSecondarySeries)
        {
            if (series is IHasDataRange hasRange)
            {
                var c = hasRange.ComputeDataRange(context);
                if (c.XMin.HasValue && c.XMin.Value < xMin) xMin = c.XMin.Value;
                if (c.XMax.HasValue && c.XMax.Value > xMax) xMax = c.XMax.Value;
            }
        }

        // A set limit is exact, the same rule the primary axes follow (Range1D.WithExplicitLimits).
        var exact = new Range1D(xMin, xMax).WithExplicitLimits(Axes.SecondaryXAxis!);
        xMin = exact.Lo;
        xMax = exact.Hi;

        if (xMin == double.MaxValue) { xMin = 0; xMax = 1; }
        if (Math.Abs(xMax - xMin) < 1e-10) { xMin -= 0.5; xMax += 0.5; }

        double xPadding = (xMax - xMin) * 0.05;
        if (!Axes.SecondaryXAxis!.Min.HasValue) xMin -= xPadding;
        if (!Axes.SecondaryXAxis!.Max.HasValue) xMax += xPadding;

        return new DataRange(xMin, xMax, yMin, yMax);
    }

    /// <summary>Renders the four axis spines (border lines) based on the <see cref="Axes.Spines"/> configuration.</summary>
    private void RenderSpines(DataTransform transform)
    {
        var spines = Axes.Spines;
        double left = PlotArea.X;
        double right = PlotArea.X + PlotArea.Width;
        double top = PlotArea.Y;
        double bottom = PlotArea.Y + PlotArea.Height;

        if (spines.Bottom.Visible)
        {
            double y = ResolveSpineY(spines.Bottom, bottom, top, bottom, transform);
            DrawSpineLine(new Point(left, y), new Point(right, y),
                spines.Bottom.Color ?? Theme.ForegroundText, spines.Bottom.LineWidth, spines.Bottom.LineStyle);
        }

        if (spines.Top.Visible)
        {
            double y = ResolveSpineY(spines.Top, top, top, bottom, transform);
            DrawSpineLine(new Point(left, y), new Point(right, y),
                spines.Top.Color ?? Theme.ForegroundText, spines.Top.LineWidth, spines.Top.LineStyle);
        }

        if (spines.Left.Visible)
        {
            double x = ResolveSpineX(spines.Left, left, left, right, transform);
            DrawSpineLine(new Point(x, top), new Point(x, bottom),
                spines.Left.Color ?? Theme.ForegroundText, spines.Left.LineWidth, spines.Left.LineStyle);
        }

        if (spines.Right.Visible)
        {
            double x = ResolveSpineX(spines.Right, right, left, right, transform);
            DrawSpineLine(new Point(x, top), new Point(x, bottom),
                spines.Right.Color ?? Theme.ForegroundText, spines.Right.LineWidth, spines.Right.LineStyle);
        }
    }

    private double ResolveSpineY(SpineConfig spine, double edgeY, double plotTop, double plotBottom, DataTransform transform) =>
        spine.Position switch
        {
            SpinePosition.Data => transform.DataToPixel(0, spine.PositionValue).Y,
            SpinePosition.Axes => plotTop + (1 - spine.PositionValue) * (plotBottom - plotTop),
            _ => edgeY
        };

    private double ResolveSpineX(SpineConfig spine, double edgeX, double plotLeft, double plotRight, DataTransform transform) =>
        spine.Position switch
        {
            SpinePosition.Data => transform.DataToPixel(spine.PositionValue, 0).X,
            SpinePosition.Axes => plotLeft + spine.PositionValue * (plotRight - plotLeft),
            _ => edgeX
        };

    private void DrawSpineLine(Point p1, Point p2, Color color, double lineWidth, LineStyle lineStyle = LineStyle.Solid)
    {
        Ctx.BeginGroup("spine");
        Ctx.DrawLine(p1, p2, new StrokeStyle(color, lineWidth, lineStyle));
        Ctx.EndGroup();
    }

    private void RenderBreakMarkers(IReadOnlyList<Models.AxisBreak> breaks, DataTransform transform, bool isXAxis)
    {
        const double MarkerHalfSize = 6.0;
        const double MarkerThickness = 1.5;
        var markerColor = Theme.ForegroundText;
        var bgColor = Theme.AxesBackground;

        foreach (var b in breaks)
        {
            if (b.Style == Models.BreakStyle.None) continue;

            if (isXAxis)
            {
                double compressedX = AxisBreakMapper.Remap(breaks, b.From, transform.DataXMin, transform.DataXMax);
                if (double.IsNaN(compressedX)) continue;
                var pt = transform.DataToPixel(compressedX, transform.DataYMin);
                DrawAxisBreakMark(b.Style, markerColor, bgColor, MarkerHalfSize, MarkerThickness,
                    pt.X, PlotArea.Y, PlotArea.Y + PlotArea.Height, horizontal: true);
            }
            else
            {
                double compressedY = AxisBreakMapper.Remap(breaks, b.From, transform.DataYMin, transform.DataYMax);
                if (double.IsNaN(compressedY)) continue;
                var pt = transform.DataToPixel(transform.DataXMin, compressedY);
                DrawAxisBreakMark(b.Style, markerColor, bgColor, MarkerHalfSize, MarkerThickness,
                    pt.Y, PlotArea.X, PlotArea.X + PlotArea.Width, horizontal: false);
            }
        }
    }

    private void DrawAxisBreakMark(
        Models.BreakStyle style, Color lineColor, Color bgColor,
        double halfSize, double thickness,
        double pos, double edgeA, double edgeB, bool horizontal)
    {
        const double GapHalf = 4.0;
        if (horizontal)
            Ctx.DrawRectangle(new Rect(pos - GapHalf, edgeA, GapHalf * 2, edgeB - edgeA), new ShapeStyle(bgColor, null, 0));
        else
            Ctx.DrawRectangle(new Rect(edgeA, pos - GapHalf, edgeB - edgeA, GapHalf * 2), new ShapeStyle(bgColor, null, 0));

        DrawBreakSegments(
            horizontal ? Orientation.Horizontal : Orientation.Vertical,
            crossStart: edgeA, crossEnd: edgeB,
            perpPos: pos, halfSize: halfSize,
            thickness: thickness, lineColor: lineColor,
            style: style);
    }

    // ── Internal helpers (testable via InternalsVisibleTo) ───────────────────

    /// <summary>Draws major or minor grid lines for one axis orientation.
    /// Horizontal → vertical lines at each X tick; Vertical → horizontal lines at each Y tick.
    /// <paramref name="fixedCoord"/> is the data-space value of the perpendicular axis used in the
    /// DataToPixel lookup (yMin for X ticks, xMin for Y ticks).</summary>
    internal void RenderGridLines(
        Orientation orientation,
        double[] ticks,
        double fixedCoord,
        DataTransform transform,
        Color color, double width, LineStyle style,
        bool minor)
    {
        bool isHorizontal = orientation == Orientation.Horizontal;
        double areaMin = isHorizontal ? PlotArea.X : PlotArea.Y;
        double areaMax = isHorizontal ? PlotArea.X + PlotArea.Width : PlotArea.Y + PlotArea.Height;

        if (!minor)
        {
            foreach (var tick in ticks)
            {
                var pt = isHorizontal
                    ? transform.DataToPixel(tick, fixedCoord)
                    : transform.DataToPixel(fixedCoord, tick);
                if (isHorizontal)
                    Ctx.DrawLine(new Point(pt.X, PlotArea.Y), new Point(pt.X, PlotArea.Y + PlotArea.Height),
                        new StrokeStyle(color, width, style));
                else
                    Ctx.DrawLine(new Point(PlotArea.X, pt.Y), new Point(PlotArea.X + PlotArea.Width, pt.Y),
                        new StrokeStyle(color, width, style));
            }
        }
        else if (ticks.Length >= 2)
        {
            double majorStep = ticks[1] - ticks[0];
            double minorStep = majorStep / 5;
            double rangeStart = ticks[0] - majorStep;
            double rangeEnd   = ticks[^1] + majorStep;
            for (double mt = rangeStart; mt <= rangeEnd + minorStep * 0.01; mt += minorStep)
            {
                mt = Math.Round(mt, 10);
                if (Array.Exists(ticks, t => Math.Abs(t - mt) < minorStep * 0.01)) continue;
                var pt = isHorizontal
                    ? transform.DataToPixel(mt, fixedCoord)
                    : transform.DataToPixel(fixedCoord, mt);
                double pos = isHorizontal ? pt.X : pt.Y;
                if (pos < areaMin || pos > areaMax) continue;
                if (isHorizontal)
                    Ctx.DrawLine(new Point(pt.X, PlotArea.Y), new Point(pt.X, PlotArea.Y + PlotArea.Height),
                        new StrokeStyle(color, width, style));
                else
                    Ctx.DrawLine(new Point(PlotArea.X, pt.Y), new Point(PlotArea.X + PlotArea.Width, pt.Y),
                        new StrokeStyle(color, width, style));
            }
        }
    }

    /// <summary>Draws the zigzag or straight-line segments of an axis break marker.
    /// The white-rectangle gap erasure is done by the caller (<see cref="DrawAxisBreakMark"/>).
    /// <paramref name="crossStart"/>/<paramref name="crossEnd"/> span the axis extent (e.g. PlotArea.Y to PlotArea.Y+Height for horizontal);
    /// <paramref name="perpPos"/> is the break position on the perpendicular axis.</summary>
    internal void DrawBreakSegments(
        Orientation orientation,
        double crossStart, double crossEnd,
        double perpPos, double halfSize,
        double thickness, Color lineColor,
        BreakStyle style)
    {
        double span = crossEnd - crossStart;
        bool horiz = orientation == Orientation.Horizontal;

        if (style == BreakStyle.Zigzag)
        {
            if (horiz)
            {
                Ctx.DrawLine(new Point(perpPos - halfSize, crossStart + span * 0.4),
                    new Point(perpPos, crossStart + span * 0.5), new StrokeStyle(lineColor, thickness, LineStyle.Solid));
                Ctx.DrawLine(new Point(perpPos, crossStart + span * 0.5),
                    new Point(perpPos + halfSize, crossStart + span * 0.6), new StrokeStyle(lineColor, thickness, LineStyle.Solid));
                Ctx.DrawLine(new Point(perpPos - halfSize, crossEnd - span * 0.6),
                    new Point(perpPos, crossEnd - span * 0.5), new StrokeStyle(lineColor, thickness, LineStyle.Solid));
                Ctx.DrawLine(new Point(perpPos, crossEnd - span * 0.5),
                    new Point(perpPos + halfSize, crossEnd - span * 0.4), new StrokeStyle(lineColor, thickness, LineStyle.Solid));
            }
            else
            {
                Ctx.DrawLine(new Point(crossStart + span * 0.4, perpPos - halfSize),
                    new Point(crossStart + span * 0.5, perpPos), new StrokeStyle(lineColor, thickness, LineStyle.Solid));
                Ctx.DrawLine(new Point(crossStart + span * 0.5, perpPos),
                    new Point(crossStart + span * 0.6, perpPos + halfSize), new StrokeStyle(lineColor, thickness, LineStyle.Solid));
            }
        }
        else if (style == BreakStyle.Straight)
        {
            if (horiz)
            {
                Ctx.DrawLine(new Point(perpPos - halfSize * 0.5, crossStart + span * 0.3),
                    new Point(perpPos + halfSize * 0.5, crossStart + span * 0.7),
                    new StrokeStyle(lineColor, thickness, LineStyle.Solid));
                Ctx.DrawLine(new Point(perpPos - halfSize * 0.5 + halfSize * 0.6, crossStart + span * 0.3),
                    new Point(perpPos + halfSize * 0.5 + halfSize * 0.6, crossStart + span * 0.7),
                    new StrokeStyle(lineColor, thickness, LineStyle.Solid));
            }
            else
            {
                Ctx.DrawLine(new Point(crossStart + span * 0.3, perpPos - halfSize * 0.5),
                    new Point(crossStart + span * 0.7, perpPos + halfSize * 0.5),
                    new StrokeStyle(lineColor, thickness, LineStyle.Solid));
                Ctx.DrawLine(new Point(crossStart + span * 0.3, perpPos - halfSize * 0.5 + halfSize * 0.6),
                    new Point(crossStart + span * 0.7, perpPos + halfSize * 0.5 + halfSize * 0.6),
                    new StrokeStyle(lineColor, thickness, LineStyle.Solid));
            }
        }
    }
}
