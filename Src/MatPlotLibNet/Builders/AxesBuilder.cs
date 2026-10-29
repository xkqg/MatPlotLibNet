// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Models.Tools;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using FibonacciRetracementTool = MatPlotLibNet.Models.Tools.FibonacciRetracement;

namespace MatPlotLibNet;

/// <summary>Fluent builder for configuring an <see cref="Models.Axes"/> instance within a subplot.</summary>
/// <example>
/// Dual-axis chart: candlestick + Bollinger bands on the left, the traded volume against its own Y axis on the right:
/// <code>
/// string svg = Plt.Create()
///     .AddSubPlot(1, 1, 1, ax =>
///     {
///         ax.Candlestick(open, high, low, close)
///           .BollingerBands(20, 2.0)
///           .AxHLine(support, r => r.Color = Color.Red)
///           .SetYLabel("Price")
///           .WithAddRightYAxis(volumeAxis =>
///               volumeAxis.SetYLabel("Volume").Plot(days, volume));
///     })
///     .ToSvg();
/// </code>
/// </example>
public sealed class AxesBuilder
{
    private readonly Axes _axes = new();
    // True when the panel uses bar-slot X coordinates (slot [i,i+1], centre at i+0.5).
    // Set by UseBarSlotX() or inferred from a categorical series on the same axes.
    private bool _isBarSlotContext;

    /// <summary>Sets the axes title.</summary>
    public AxesBuilder WithTitle(string title) { _axes.Title = title; return this; }

    /// <summary>Sets the axes title with an optional text style override.</summary>
    /// <param name="title">The title text.</param>
    /// <param name="configure">Function that receives the current <see cref="TextStyle"/> and returns a modified copy; <see langword="null"/> leaves the style unchanged.</param>
    public AxesBuilder WithTitle(string title, Func<TextStyle, TextStyle>? configure)
    {
        _axes.Title = title;
        if (configure is not null) _axes.TitleStyle = configure(_axes.TitleStyle ?? new TextStyle());
        return this;
    }

    /// <summary>Sets the X-axis label.</summary>
    public AxesBuilder SetXLabel(string label) { _axes.XAxis.Label = label; return this; }

    /// <summary>Sets the X-axis label with an optional text style override.</summary>
    /// <param name="label">The axis label text.</param>
    /// <param name="configure">Function that receives the current <see cref="TextStyle"/> and returns a modified copy; <see langword="null"/> leaves the style unchanged.</param>
    public AxesBuilder SetXLabel(string label, Func<TextStyle, TextStyle>? configure)
    {
        _axes.XAxis.Label = label;
        if (configure is not null) _axes.XAxis.LabelStyle = configure(_axes.XAxis.LabelStyle ?? new TextStyle());
        return this;
    }

    /// <summary>The Y verbs of this builder act on the primary Y axis, which is slot 0 of the left side of the
    /// subplot's Y axes: they are <see cref="YAxisSlotBuilder"/>'s verbs over that slot, not a second copy.</summary>
    private YAxisSlotBuilder PrimaryYAxis => new(_axes, _axes.YAxes.Left[0]);

    /// <summary>Sets the Y-axis label.</summary>
    public AxesBuilder SetYLabel(string label) { PrimaryYAxis.SetYLabel(label); return this; }

    /// <summary>Sets the Y-axis label with an optional text style override.</summary>
    /// <param name="label">The axis label text.</param>
    /// <param name="configure">Function that receives the current <see cref="TextStyle"/> and returns a modified copy; <see langword="null"/> leaves the style unchanged.</param>
    public AxesBuilder SetYLabel(string label, Func<TextStyle, TextStyle>? configure)
    {
        PrimaryYAxis.SetYLabel(label, configure);
        return this;
    }

    /// <summary>Colours the left Y axis in one call: its spine, its tick marks, its tick labels and its title.
    /// Pairs the axis with a series drawn in the same colour; keep <see cref="SetYLabel(string)"/> naming the series and
    /// its unit, so the colour is never the only thing that says which series the axis belongs to.</summary>
    /// <param name="color">The colour all four take.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <remarks>Everything else the axis carries (tick length, label rotation, font size, spine width) is kept.
    /// Every further Y axis is coloured with <see cref="YAxisSlotBuilder.SetYAxisColor"/>: slot 0 of the right side
    /// colours the right spine, an axis stacked past slot 0 colours its ticks, tick labels and title.</remarks>
    public AxesBuilder SetYAxisColor(Color color) { PrimaryYAxis.SetYAxisColor(color); return this; }

    /// <summary>Colours the bottom X axis in one call: its spine, its tick marks, its tick labels and its title.</summary>
    /// <param name="color">The colour all four take.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <remarks>Everything else the axis carries (tick length, label rotation, font size, spine width) is kept.
    /// The top axis is coloured with <see cref="SecondaryXAxisBuilder.SetXAxisColor"/>.</remarks>
    public AxesBuilder SetXAxisColor(Color color)
    {
        _axes.ApplyAxisColor(_axes.XAxis, SpineEdge.Bottom, color);
        return this;
    }

    /// <summary>Sets the X-axis data range limits.</summary>
    public AxesBuilder SetXLim(double min, double max) { _axes.XAxis.Min = min; _axes.XAxis.Max = max; return this; }

    /// <summary>Sets the Y-axis data range limits.</summary>
    public AxesBuilder SetYLim(double min, double max) { PrimaryYAxis.SetYLim(min, max); return this; }

    /// <summary>Sets the Z-axis label (3D charts only).</summary>
    public AxesBuilder SetZLabel(string label) { _axes.ZAxis.Label = label; return this; }

    /// <summary>Sets the Z-axis data range limits (3D charts only).</summary>
    public AxesBuilder SetZLim(double min, double max) { _axes.ZAxis.Min = min; _axes.ZAxis.Max = max; return this; }

    /// <summary>Sets the auto-scale margin on each side of the X axis (default 0.05). Use 0 for bar/candlestick charts that already include half-bar-width padding.</summary>
    public AxesBuilder SetXMargin(double margin) { _axes.XAxis.Margin = margin; return this; }

    /// <summary>Marks this panel as using bar-slot X coordinates (slot [i, i+1], centre at i+0.5).
    /// All subsequently added indicators will automatically apply a +0.5 offset so their points
    /// align with bar centres rather than the left edge of each bar slot.</summary>
    /// <remarks>Must be called <em>before</em> adding any indicator series; the bar-slot X mapping
    /// is fixed at subplot construction time and cannot be applied retroactively.</remarks>
    public AxesBuilder UseBarSlotX() { _isBarSlotContext = true; return this; }

    /// <summary>Sets the auto-scale margin on each side of the Y axis (default 0.05).</summary>
    public AxesBuilder SetYMargin(double margin) { _axes.YAxis.Margin = margin; return this; }

    /// <summary>Removes all data padding — data touches the axis spines directly.
    /// Equivalent to <c>SetXMargin(0).SetYMargin(0)</c>.</summary>
    public AxesBuilder WithTightMargins() { _axes.XAxis.Margin = 0; _axes.YAxis.Margin = 0; return this; }

    /// <summary>Rotates X-axis tick labels by <paramref name="degrees"/>. Matches matplotlib's
    /// <c>ax.tick_params(axis='x', labelrotation=...)</c>. Pass 0 to restore horizontal labels
    /// — at 0 the renderer auto-rotates to 30° if adjacent labels would overlap
    /// (<c>Figure.autofmt_xdate</c> parity).</summary>
    public AxesBuilder WithXTickLabelRotation(double degrees)
    {
        _axes.XAxis.MajorTicks = _axes.XAxis.MajorTicks with { LabelRotation = degrees };
        return this;
    }

    /// <summary>Rotates Y-axis tick labels by <paramref name="degrees"/>. Matches matplotlib's
    /// <c>ax.tick_params(axis='y', labelrotation=...)</c>.</summary>
    public AxesBuilder WithYTickLabelRotation(double degrees)
    {
        _axes.YAxis.MajorTicks = _axes.YAxis.MajorTicks with { LabelRotation = degrees };
        return this;
    }

    /// <summary>Mirrors Y-axis ticks and labels on both left and right spines.
    /// Useful for wide figures where reading the Y scale from the right side is more convenient.</summary>
    /// <remarks>The layout makes room for the right-hand labels, and end-of-line labels start past them. A secondary Y
    /// axis with series takes the right spine for its own ticks, so the mirrored ticks are not drawn there.</remarks>
    public AxesBuilder WithYTicksMirrored()
    {
        _axes.YAxis.MajorTicks = _axes.YAxis.MajorTicks with { Mirror = true };
        return this;
    }

    /// <summary>Mirrors X-axis ticks and labels on both top and bottom spines.</summary>
    /// <remarks>The axes title moves up above the top labels and the layout makes room for both. A secondary X axis
    /// with series takes the top spine for its own ticks, so the mirrored ticks are not drawn there.</remarks>
    public AxesBuilder WithXTicksMirrored()
    {
        _axes.XAxis.MajorTicks = _axes.XAxis.MajorTicks with { Mirror = true };
        return this;
    }

    /// <summary>Sets the X-axis scale type (e.g., linear or logarithmic).</summary>
    public AxesBuilder SetXScale(AxisScale scale) { _axes.XAxis.Scale = scale; return this; }

    /// <summary>Sets the Y-axis scale type (e.g., linear or logarithmic).</summary>
    public AxesBuilder SetYScale(AxisScale scale) { _axes.YAxis.Scale = scale; return this; }

    /// <summary>Sets the Y-axis to symmetric logarithmic scale with the specified linear threshold.
    /// Linear within [-linthresh, linthresh], logarithmic outside.</summary>
    /// <param name="linthresh">Linear threshold. Default 1.0.</param>
    public AxesBuilder WithSymlogYScale(double linthresh = 1.0)
    {
        _axes.YAxis.Scale = AxisScale.SymLog;
        _axes.YAxis.SymLogLinThresh = linthresh;
        _axes.YAxis.TickLocator = new MatPlotLibNet.Rendering.TickLocators.SymlogLocator(linthresh);
        return this;
    }

    /// <summary>Sets the X-axis to symmetric logarithmic scale with the specified linear threshold.</summary>
    public AxesBuilder WithSymlogXScale(double linthresh = 1.0)
    {
        _axes.XAxis.Scale = AxisScale.SymLog;
        _axes.XAxis.SymLogLinThresh = linthresh;
        _axes.XAxis.TickLocator = new MatPlotLibNet.Rendering.TickLocators.SymlogLocator(linthresh);
        return this;
    }

    /// <summary>Adds a buy or sell signal marker at the specified data coordinates.</summary>
    public AxesBuilder AddSignal(double x, double y, SignalDirection direction, Action<SignalMarker>? configure = null)
    {
        var marker = _axes.AddSignal(x, y, direction);
        configure?.Invoke(marker);
        return this;
    }

    /// <summary>Applies a technical indicator to this axes, adding computed series or decorations.</summary>
    /// <remarks>Overlay indicators (SMA, EMA, Bollinger Bands) add series to the same axes.
    /// Panel indicators (RSI, MACD) should be placed in a separate subplot by the caller. In a bar-slot context
    /// (a categorical series already on the axes), nudges the indicator's X positions onto the category slot's
    /// centre -- the same nudge every dedicated indicator verb (Sma, Rsi, ...) applies via
    /// <see cref="ApplyIndicator{T}"/>, so the same indicator lands in the same place through either entry
    /// point.</remarks>
    public AxesBuilder AddIndicator(IIndicator indicator)
    {
        if (indicator is Indicators.Indicator concrete && IsBarSlotContext())
            concrete.Offset = CategorySlot.CentreOffset;
        indicator.Apply(_axes);
        return this;
    }

    /// <summary>Configures the spine (border line) display for this axes.</summary>
    public AxesBuilder WithSpines(Func<SpinesConfig, SpinesConfig> configure) { _axes.Spines = configure(_axes.Spines); return this; }

    /// <summary>Configures the 3D axes pane colors (floor, left wall, right wall).
    /// Set <c>Visible = false</c> to hide all panes for a transparent 3D background.</summary>
    public AxesBuilder WithPane3D(Func<Pane3DConfig, Pane3DConfig> configure) { _axes.Pane3D = configure(_axes.Pane3D); return this; }

    /// <summary>Hides the top spine.</summary>
    public AxesBuilder HideTopSpine() { _axes.Spines = _axes.Spines with { Top = _axes.Spines.Top with { Visible = false } }; return this; }

    /// <summary>Hides the right spine.</summary>
    public AxesBuilder HideRightSpine() { _axes.Spines = _axes.Spines with { Right = _axes.Spines.Right with { Visible = false } }; return this; }

    /// <summary>Hides every spine, tick and tick label on both the X and Y axis — turns the plot
    /// area into a bare canvas. Useful for non-coordinate charts like Sankey, Treemap, or Sunburst
    /// where the default cartesian axes decoration has no meaning and only adds visual clutter.</summary>
    public AxesBuilder HideAllAxes()
    {
        _axes.Spines = _axes.Spines with
        {
            Top = _axes.Spines.Top with { Visible = false },
            Bottom = _axes.Spines.Bottom with { Visible = false },
            Left = _axes.Spines.Left with { Visible = false },
            Right = _axes.Spines.Right with { Visible = false },
        };
        _axes.XAxis.MajorTicks = _axes.XAxis.MajorTicks with { Visible = false };
        _axes.XAxis.MinorTicks = _axes.XAxis.MinorTicks with { Visible = false };
        _axes.YAxis.MajorTicks = _axes.YAxis.MajorTicks with { Visible = false };
        _axes.YAxis.MinorTicks = _axes.YAxis.MinorTicks with { Visible = false };
        return this;
    }

    /// <summary>Shares the X axis with the axes identified by the given key.</summary>
    public AxesBuilder ShareX(string key) { _shareXKey = key; return this; }
    private string? _shareXKey;

    /// <summary>Shares the Y axis with the axes identified by the given key.</summary>
    public AxesBuilder ShareY(string key) { _shareYKey = key; return this; }
    private string? _shareYKey;

    /// <summary>Gets the pending share-X key, if any. Used by FigureBuilder to resolve sharing at build time.</summary>
    internal string? ShareXKey => _shareXKey;

    /// <summary>Gets the pending share-Y key, if any. Used by FigureBuilder to resolve sharing at build time.</summary>
    internal string? ShareYKey => _shareYKey;

    /// <summary>Enables or disables native SVG tooltips for data elements.</summary>
    public AxesBuilder WithTooltips(bool enabled = true) { _axes.EnableTooltips = enabled; return this; }

    /// <summary>Configures the first Y axis on the right of the plot and adds series to it, creating that axis when there
    /// is none. Called again it configures the same axis; it never stacks another.</summary>
    /// <remarks>The container verb <see cref="WithAddRightYAxis"/> replaces it: it stacks one more right-hand axis every
    /// call, so a chart with three right axes needs no count and no "second" or "third" in a name. This verb stays
    /// published so existing code compiles and draws what it always drew.</remarks>
    [Obsolete("Use WithAddRightYAxis, which stacks one more right-hand Y axis per call; WithSecondaryYAxis keeps configuring the first right-hand axis.")]
    public AxesBuilder WithSecondaryYAxis(Action<SecondaryAxisBuilder> configure)
    {
        configure(new SecondaryAxisBuilder(new YAxisSlotBuilder(_axes, _axes.YAxes.Right.FirstOrAdd())));
        return this;
    }

    /// <summary>Configures a secondary X-axis (top edge) and adds series to it.</summary>
    public AxesBuilder WithSecondaryXAxis(Action<SecondaryXAxisBuilder> configure)
    {
        _axes.TwinY();
        var builder = new SecondaryXAxisBuilder(_axes);
        configure(builder);
        return this;
    }

    /// <summary>Stacks one more Y axis outward on <paramref name="side"/>, past every axis already there, and
    /// configures it. Every call adds one; there is no cap. On a right side with no axis yet, the new axis is the
    /// first right axis.</summary>
    /// <param name="side">Which side to stack the new axis on.</param>
    /// <param name="configure">Callback that configures the new axis and adds series to it.</param>
    /// <remarks>The container verbs <see cref="WithAddRightYAxis"/> and <see cref="WithAddLeftYAxis"/> replace it, one
    /// per side. This verb stays published so existing code compiles and draws what it always drew.</remarks>
    [Obsolete("Use WithAddRightYAxis or WithAddLeftYAxis, which stack one more Y axis on their side per call; WithOutwardYAxis does the same for the side it is given.")]
    public AxesBuilder WithOutwardYAxis(AxisSide side, Action<OutwardYAxisBuilder> configure)
    {
        configure(new OutwardYAxisBuilder(new YAxisSlotBuilder(_axes, _axes.YAxes.StackOn(side).Add())));
        return this;
    }

    /// <summary>Stacks one more Y axis on the right of the plot, past every right axis already there, and configures it.
    /// Call it as often as you want axes: the first call adds the axis nearest the plot, each further call stacks one more
    /// past it, with no cap and nothing to count. This is matplotlib's repeated <c>ax.twinx()</c> with its right spine
    /// moved outward by <c>ax.spines['right'].set_position(('outward', N))</c>, in one verb.</summary>
    /// <param name="configure">Callback that receives the builder of the new axis: label, limits, colour and the series
    /// plotted against it.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is null.</exception>
    /// <remarks>The layout makes room for every axis, and each one keeps its own tick labels and title. The first right
    /// axis colours the right spine when it is given a colour; an axis stacked past it draws no spine of its own.</remarks>
    /// <example>
    /// Three units against one time axis:
    /// <code>
    /// Plt.Create().AddSubPlot(1, 1, 1, ax => ax
    ///     .Plot(days, price).SetYLabel("Price")
    ///     .WithAddRightYAxis(rate => rate.SetYLabel("Rate (%)").Plot(days, rates))
    ///     .WithAddRightYAxis(load => load.SetYLabel("Load").SetYLim(0, 1).Plot(days, loads)));
    /// </code>
    /// </example>
    public AxesBuilder WithAddRightYAxis(Action<YAxisSlotBuilder> configure) => AddYAxis(_axes.YAxes.Right, configure);

    /// <summary>Stacks one more Y axis on the left of the plot, past the primary Y axis and every left axis already
    /// added, and configures it. The primary Y axis stays the left axis nearest the plot; the first call adds the axis next
    /// to it, each further call stacks one more past that, with no cap and nothing to count. This is matplotlib's repeated
    /// <c>ax.twinx()</c> with its Y axis moved to the left (<c>twin.yaxis.tick_left()</c>) and its left spine moved outward
    /// by <c>twin.spines['left'].set_position(('outward', N))</c>, in one verb.</summary>
    /// <param name="configure">Callback that receives the builder of the new axis: label, limits, colour and the series
    /// plotted against it.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is null.</exception>
    /// <remarks>The layout makes room for every axis, and each one keeps its own tick labels and title. An axis stacked
    /// past the primary draws no spine of its own, so colouring it colours its ticks, tick labels and title only.</remarks>
    /// <example>
    /// A queue length next to the primary price axis:
    /// <code>
    /// Plt.Create().AddSubPlot(1, 1, 1, ax => ax
    ///     .Plot(days, price).SetYLabel("Price")
    ///     .WithAddLeftYAxis(queue => queue.SetYLabel("Queue").Plot(days, lengths)));
    /// </code>
    /// </example>
    public AxesBuilder WithAddLeftYAxis(Action<YAxisSlotBuilder> configure) => AddYAxis(_axes.YAxes.Left, configure);

    /// <summary>Sets the blank gap, in pixels, between the ink of every Y axis title of the subplot and the ink of the
    /// widest tick label beside it: the same for the primary Y axis and every axis added with
    /// <see cref="WithAddLeftYAxis"/> or <see cref="WithAddRightYAxis"/>, on both sides, and between two stacked axes. The
    /// default is 4, matplotlib's <c>axes.labelpad</c>. A title's own pad (the <c>Pad</c> of the text style given to
    /// <c>SetYLabel</c>) moves that one title further out on top of it.</summary>
    /// <param name="pixels">The gap in pixels: a finite number, 0 or more.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="pixels"/> is negative, NaN or infinite.</exception>
    /// <example>
    /// Titles a little further from their tick labels on both sides:
    /// <code>
    /// Plt.Create().AddSubPlot(1, 1, 1, ax => ax
    ///     .Plot(days, price).SetYLabel("Price")
    ///     .WithAddRightYAxis(rate => rate.SetYLabel("Rate (%)").Plot(days, rates))
    ///     .WithYAxisTitleGap(8));
    /// </code>
    /// </example>
    public AxesBuilder WithYAxisTitleGap(double pixels)
    {
        _axes.YAxes.TitleGap = YAxisContainer.ValidTitleGap(pixels, nameof(pixels));
        return this;
    }

    /// <summary>Stacks one more axis on <paramref name="stack"/> and hands it to <paramref name="configure"/>: the one
    /// body both container verbs share.</summary>
    /// <param name="stack">The side's stack of the subplot's Y axes.</param>
    /// <param name="configure">The caller's callback; checked before anything is added.</param>
    private AxesBuilder AddYAxis(YAxisStack stack, Action<YAxisSlotBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(new YAxisSlotBuilder(_axes, stack.Add()));
        return this;
    }

    /// <summary>Adds a text annotation at the specified data coordinates.</summary>
    public AxesBuilder Annotate(string text, double x, double y, Action<Annotation>? configure = null)
    {
        var annotation = _axes.Annotate(text, x, y);
        configure?.Invoke(annotation);
        return this;
    }

    /// <summary>Adds a text annotation with an arrow pointing to the given target coordinates.</summary>
    public AxesBuilder Annotate(string text, double x, double y, double arrowX, double arrowY,
        Action<Annotation>? configure = null)
    {
        var annotation = _axes.Annotate(text, x, y);
        annotation.ArrowTargetX = arrowX;
        annotation.ArrowTargetY = arrowY;
        configure?.Invoke(annotation);
        return this;
    }

    /// <summary>Adds a trendline between two data-coordinate points.</summary>
    public AxesBuilder AddTrendline(double x1, double y1, double x2, double y2,
        Action<Trendline>? configure = null)
    {
        var line = _axes.AddTrendline(x1, y1, x2, y2);
        configure?.Invoke(line);
        return this;
    }

    /// <summary>Adds a horizontal price level (support/resistance) at the given value.
    /// <para>The automatic range of the Y axis holds the level, so a level beyond the data is drawn inside the plot; a
    /// level beyond a limit you set is not drawn, nor its label.</para></summary>
    public AxesBuilder AddLevel(double value, Action<HorizontalLevel>? configure = null)
    {
        var level = _axes.AddLevel(value);
        configure?.Invoke(level);
        return this;
    }

    /// <summary>Adds a Fibonacci retracement overlay between a price high and a price low.
    /// <para>The automatic range of the Y axis holds every level, from the high to the low; a level beyond a limit you
    /// set is not drawn, nor its label.</para></summary>
    public AxesBuilder AddFibonacci(double priceHigh, double priceLow,
        Action<FibonacciRetracementTool>? configure = null)
    {
        var fib = _axes.AddFibonacci(priceHigh, priceLow);
        configure?.Invoke(fib);
        return this;
    }

    /// <summary>Adds a horizontal reference line at the specified Y value.
    /// <para>The automatic range of the Y axis holds the line, as matplotlib's <c>axhline</c> updates the data limits,
    /// so a line beyond the data is drawn inside the plot; a line beyond a limit you set is not drawn, nor its
    /// label.</para></summary>
    public AxesBuilder AxHLine(double y, Action<ReferenceLine>? configure = null)
    {
        var line = _axes.AxHLine(y);
        configure?.Invoke(line);
        return this;
    }

    /// <summary>Adds a vertical reference line at the specified X value.
    /// <para>The automatic range of the X axis holds the line, as matplotlib's <c>axvline</c> updates the data limits,
    /// so a line beyond the data is drawn inside the plot; a line beyond a limit you set is not drawn, nor its
    /// label.</para></summary>
    public AxesBuilder AxVLine(double x, Action<ReferenceLine>? configure = null)
    {
        var line = _axes.AxVLine(x);
        configure?.Invoke(line);
        return this;
    }

    /// <summary>Adds a horizontal shaded span between the specified Y values.</summary>
    public AxesBuilder AxHSpan(double yMin, double yMax, Action<SpanRegion>? configure = null)
    {
        var span = _axes.AxHSpan(yMin, yMax);
        configure?.Invoke(span);
        return this;
    }

    /// <summary>Adds a vertical shaded span between the specified X values.</summary>
    public AxesBuilder AxVSpan(double xMin, double xMax, Action<SpanRegion>? configure = null)
    {
        var span = _axes.AxVSpan(xMin, xMax);
        configure?.Invoke(span);
        return this;
    }

    /// <summary>Adds a threshold marker: a dashed <see cref="ReferenceLine"/> at <paramref name="value"/>
    /// plus a shaded <see cref="SpanRegion"/> covering the breach zone on the <paramref name="breach"/>
    /// side. An optional text <paramref name="label"/> is added as an annotation at the threshold value.</summary>
    /// <param name="value">The threshold value in data coordinates.</param>
    /// <param name="orientation">Horizontal (constant Y) or Vertical (constant X).</param>
    /// <param name="breach">Which side of the threshold is the breach zone.</param>
    /// <param name="color">Optional color for the line and span. Null uses the renderer cycle color.</param>
    /// <param name="label">Optional label text. Null adds no annotation.</param>
    public AxesBuilder Threshold(double value, Orientation orientation, ThresholdBreach breach,
        Color? color = null, string? label = null)
    {
        _axes.AddThreshold(value, orientation, breach, color, label);
        return this;
    }

    /// <summary>Sets the X-axis to date scale with intelligent tick placement and automatic format selection.</summary>
    /// <remarks>
    /// Installs an <see cref="Rendering.TickLocators.AutoDateLocator"/> and a paired
    /// <see cref="Rendering.TickFormatters.AutoDateFormatter"/> that together choose granularity
    /// (years, months, weeks, days, hours, minutes, or seconds) from the visible range.
    /// </remarks>
    public AxesBuilder SetXDateAxis()
    {
        var locator = new Rendering.TickLocators.AutoDateLocator();
        _axes.XAxis.Scale = AxisScale.Date;
        _axes.XAxis.TickLocator = locator;
        _axes.XAxis.TickFormatter = new Rendering.TickFormatters.AutoDateFormatter(locator);
        return this;
    }

    /// <summary>Adds a line series with DateTime X values; automatically activates the date X-axis.</summary>
    /// <param name="dates">X values as <see cref="DateTime"/> instances (converted to OLE Automation dates).</param>
    /// <param name="y">Y values corresponding to each date.</param>
    /// <param name="configure">Optional delegate to further configure the series.</param>
    public AxesBuilder Plot(DateTime[] dates, double[] y, Action<Models.Series.LineSeries>? configure = null)
        => SetXDateAxis().Plot(dates.Select(d => d.ToOADate()).ToArray(), y, configure);

    /// <summary>Adds a scatter series with DateTime X values; automatically activates the date X-axis.</summary>
    /// <param name="dates">X values as <see cref="DateTime"/> instances (converted to OLE Automation dates).</param>
    /// <param name="y">Y values corresponding to each date.</param>
    /// <param name="configure">Optional delegate to further configure the series.</param>
    public AxesBuilder Scatter(DateTime[] dates, double[] y, Action<Models.Series.ScatterSeries>? configure = null)
        => SetXDateAxis().Scatter(dates.Select(d => d.ToOADate()).ToArray(), y, configure);

    /// <summary>Sets the X-axis to date scale with the specified format.</summary>
    public AxesBuilder SetXDateFormat(string format = "yyyy-MM-dd")
    {
        _axes.XAxis.Scale = AxisScale.Date;
        _axes.XAxis.TickFormatter = new Rendering.TickFormatters.DateTickFormatter { DateFormat = format };
        return this;
    }

    /// <summary>Sets the Y-axis to date scale with the specified format.</summary>
    public AxesBuilder SetYDateFormat(string format = "yyyy-MM-dd")
    {
        _axes.YAxis.Scale = AxisScale.Date;
        _axes.YAxis.TickFormatter = new Rendering.TickFormatters.DateTickFormatter { DateFormat = format };
        return this;
    }

    /// <summary>Sets a custom tick formatter on the X-axis.</summary>
    public AxesBuilder SetXTickFormatter(Rendering.TickFormatters.ITickFormatter formatter)
    {
        _axes.XAxis.TickFormatter = formatter;
        return this;
    }

    /// <summary>Sets a custom tick formatter on the Y-axis.</summary>
    public AxesBuilder SetYTickFormatter(Rendering.TickFormatters.ITickFormatter formatter)
    {
        _axes.YAxis.TickFormatter = formatter;
        return this;
    }

    /// <summary>Sets a custom tick locator on the X-axis, overriding the default nice-number algorithm.</summary>
    /// <param name="locator">The <see cref="Rendering.TickLocators.ITickLocator"/> implementation to use.</param>
    /// <remarks>Also overrides any <see cref="Models.TickConfig.Spacing"/> set on the axis. Tick formatting via the axis <c>TickFormatter</c> is still applied after the locator produces its positions.</remarks>
    public AxesBuilder SetXTickLocator(Rendering.TickLocators.ITickLocator locator)
    {
        _axes.XAxis.TickLocator = locator;
        return this;
    }

    /// <summary>Sets a custom tick locator on the Y-axis, overriding the default nice-number algorithm.</summary>
    /// <param name="locator">The <see cref="Rendering.TickLocators.ITickLocator"/> implementation to use.</param>
    /// <remarks>Also overrides any <see cref="Models.TickConfig.Spacing"/> set on the axis. Tick formatting via the axis <c>TickFormatter</c> is still applied after the locator produces its positions.</remarks>
    public AxesBuilder SetYTickLocator(Rendering.TickLocators.ITickLocator locator)
    {
        _axes.YAxis.TickLocator = locator;
        return this;
    }

    /// <summary>Enables or disables minor tick marks on both axes. Minor ticks subdivide each major interval into 5 sub-intervals.</summary>
    /// <param name="visible">When <c>true</c> (default), minor ticks are shown; when <c>false</c>, they are hidden.</param>
    public AxesBuilder WithMinorTicks(bool visible = true)
    {
        _axes.XAxis.MinorTicks = _axes.XAxis.MinorTicks with { Visible = visible };
        _axes.YAxis.MinorTicks = _axes.YAxis.MinorTicks with { Visible = visible };
        return this;
    }

    /// <summary>
    /// Enables bar value labels on the last <see cref="BarSeries"/> added to this axes.
    /// </summary>
    /// <param name="format">Optional .NET format string (e.g. "F1"). When null, a value is written with up to six
    /// significant digits and no trailing zeros, as matplotlib's <c>bar_label</c> writes it by default (<c>'%g'</c>):
    /// 12500 reads <c>12500</c>, 0.5 reads <c>0.5</c>, and only a value below 1e-4 or from 1e6 takes an exponent.</param>
    /// <remarks>Only the last <see cref="BarSeries"/> in the series list is affected. Call this method once per <see cref="BarSeries"/> immediately after adding it.</remarks>
    public AxesBuilder WithBarLabels(string? format = null)
    {
        if (_axes.Series.LastOrDefault(s => s is BarSeries) is BarSeries bar)
        {
            bar.ShowLabels = true;
            bar.LabelFormat = format;
        }
        return this;
    }

    /// <summary>
    /// Enables LTTB downsampling on the last XY series (Line, Area, Step, Signal, SignalXY) added to this axes.
    /// Viewport culling is applied first; if the result still exceeds <paramref name="maxPoints"/>, LTTB reduces it.
    /// A scatter series is culled to the viewport but never reduced further, because each marker may carry its own
    /// size and colour; a scatter that carries any of those per-point arrays is drawn whole.
    /// </summary>
    /// <param name="maxPoints">Maximum number of points to display. Default is 2000.</param>
    /// <remarks>Only the last XY series is affected; call after each series you want to downsample. The two-stage pipeline (viewport cull → LTTB) is opt-in — series without <c>MaxDisplayPoints</c> set are rendered at full resolution.</remarks>
    public AxesBuilder WithDownsampling(int maxPoints = 2000)
    {
        if (_axes.Series.LastOrDefault(s => s is Models.Series.XYSeries) is Models.Series.XYSeries xy)
            xy.MaxDisplayPoints = maxPoints;
        return this;
    }

    /// <summary>Sets the bar mode (grouped or stacked) for multiple bar series.</summary>
    public AxesBuilder SetBarMode(BarMode mode) { _axes.BarMode = mode; return this; }

    /// <summary>Sets the colormap on the last heatmap/contour/surface series by name (resolved
    /// from <see cref="Styling.ColorMaps.ColorMapRegistry"/>).
    /// <para>Throws <see cref="ArgumentException"/> if <paramref name="name"/> is not a
    /// registered colormap name — surfacing typos or renamed colormaps instead of silently
    /// falling back to the renderer default. If you want a
    /// defensive lookup that returns the builder unchanged on unknown names, call
    /// <see cref="Styling.ColorMaps.ColorMapRegistry.Get"/> yourself and invoke the
    /// <see cref="WithColorMap(Styling.ColorMaps.IColorMap)"/> overload conditionally.</para></summary>
    /// <exception cref="ArgumentException">When <paramref name="name"/> is not registered.</exception>
    public AxesBuilder WithColorMap(string name)
    {
        var map = Styling.ColorMaps.ColorMapRegistry.Get(name)
            ?? throw new ArgumentException(
                $"Unknown colormap '{name}'. Registered colormaps: " +
                string.Join(", ", Styling.ColorMaps.ColorMapRegistry.Names),
                nameof(name));
        return WithColorMap(map);
    }

    /// <summary>Sets the colormap on the last colormappable series (heatmap, image, histogram2d, contour, surface, scatter, hierarchical).</summary>
    public AxesBuilder WithColorMap(Styling.ColorMaps.IColorMap colorMap)
    {
        if (_axes.Series.LastOrDefault() is Models.Series.IColormappable c) c.ColorMap = colorMap;
        return this;
    }

    /// <summary>Sets the normalizer on the last normalizable series (heatmap, image, histogram2d).</summary>
    public AxesBuilder WithNormalizer(Styling.ColorMaps.INormalizer normalizer)
    {
        if (_axes.Series.LastOrDefault() is Models.Series.INormalizable n) n.Normalizer = normalizer;
        return this;
    }

    /// <summary>Draws the name of the series added last at the end of its line, in the line's colour, instead of
    /// in the legend. Applies to a line or a streaming line (<see cref="Models.Series.IHasLabelPlacement"/>) that
    /// carries a label; when the last series cannot carry a placement, nothing changes.</summary>
    /// <remarks>The same as setting <c>LabelPlacement = SeriesLabelPlacement.LineEnd</c> on the series. Polar and 3D axes
    /// draw no end labels, so there the series keeps its legend entry.</remarks>
    /// <returns>This builder, for chaining.</returns>
    public AxesBuilder WithDirectLabel()
    {
        if (_axes.Series.LastOrDefault() is Models.Series.IHasLabelPlacement p) p.LabelPlacement = Models.Series.SeriesLabelPlacement.LineEnd;
        return this;
    }

    /// <summary>Enables a color bar alongside the plot area. Auto-detects colormap and range from heatmap/contour series.</summary>
    public AxesBuilder WithColorBar(Func<ColorBar, ColorBar>? configure = null)
    {
        var cb = _axes.ColorBar ?? new ColorBar { Visible = true };
        if (configure is not null) cb = configure(cb);
        _axes.ColorBar = cb;
        return this;
    }

    /// <summary>Configures the legend display for this axes. Preserves every other
    /// <see cref="Legend"/> property already set earlier in the chain (e.g. via
    /// <see cref="WithLegendValues"/> or the <see cref="WithLegend(Func{Legend, Legend})"/>
    /// transform overload) — only <see cref="Legend.Visible"/> and <see cref="Legend.Position"/>
    /// are updated. A legend that names no position stands at <see cref="LegendPosition.Best"/>, where it covers the
    /// fewest marks (the upper right under the classic theme).</summary>
    public AxesBuilder WithLegend(LegendPosition position = LegendPosition.Best, bool visible = true)
    {
        _axes.Legend = _axes.Legend with { Visible = visible, Position = position };
        return this;
    }

    /// <summary>Configures the legend using a transform function, preserving all other properties.</summary>
    public AxesBuilder WithLegend(Func<Legend, Legend> configure)
    {
        _axes.Legend = configure(_axes.Legend);
        return this;
    }

    /// <summary>Enables or disables the legend-values suffix that appends each labelled series'
    /// last data value to its legend entry — e.g. "Signal" becomes "Signal = 2.70"
    /// (InvariantCulture, F2 format). Only <see cref="Models.Series.XYSeries"/> instances
    /// yield a value; other series types display the label text only. Default is
    /// <see langword="false"/>.</summary>
    public AxesBuilder WithLegendValues(bool enabled = true)
    {
        _axes.Legend = _axes.Legend with { LegendValues = enabled };
        return this;
    }

    /// <summary>Toggles grid line visibility on the axes.</summary>
    public AxesBuilder ShowGrid(bool visible = true) { _axes.Grid = _axes.Grid with { Visible = visible }; return this; }

    /// <summary>Adds a discontinuous region to the X-axis.</summary>
    /// <param name="from">Data-space start of the hidden region (inclusive).</param>
    /// <param name="to">Data-space end of the hidden region (inclusive).</param>
    /// <param name="style">Visual marker style at the break boundaries (default <see cref="BreakStyle.Zigzag"/>).</param>
    public AxesBuilder WithXBreak(double from, double to, BreakStyle style = BreakStyle.Zigzag)
    {
        _axes.AddXBreak(from, to, style);
        return this;
    }

    /// <summary>Adds a discontinuous region to the Y-axis.</summary>
    /// <param name="from">Data-space start of the hidden region (inclusive).</param>
    /// <param name="to">Data-space end of the hidden region (inclusive).</param>
    /// <param name="style">Visual marker style at the break boundaries (default <see cref="BreakStyle.Zigzag"/>).</param>
    public AxesBuilder WithYBreak(double from, double to, BreakStyle style = BreakStyle.Zigzag)
    {
        _axes.AddYBreak(from, to, style);
        return this;
    }

    /// <summary>Configures the grid style using a transform function, preserving all other properties.</summary>
    public AxesBuilder WithGrid(Func<GridStyle, GridStyle> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _axes.Grid = configure(_axes.Grid);
        // The CHANGE is kept as well as its result, because what a caller means by this on a themed chart is
        // "the theme's grid, but ...". Keeping only the result cost them the theme (the grid arrives with every
        // unmentioned property back at its bare default), and reading only the result's Visible could not tell
        // "no grid, please" from "nothing said" — so asking for no grid under a gridded theme drew one anyway.
        _axes.GridOverride = configure;
        return this;
    }

    /// <summary>Adds a line series to the axes.</summary>
    public AxesBuilder Plot(double[] x, double[] y, Action<LineSeries>? configure = null)
        => AddSeries(ax => ax.Plot(x, y), configure);

    /// <summary>Adds a scatter series to the axes.</summary>
    public AxesBuilder Scatter(double[] x, double[] y, Action<ScatterSeries>? configure = null)
        => AddSeries(ax => ax.Scatter(x, y), configure);

    /// <summary>Adds a scatter whose markers are coloured by how crowded their neighbourhood is.
    /// <para>A scatter of ten thousand points is a black blob: the markers overlap, and where they overlap most
    /// is exactly what the reader wants to know and cannot see. This counts how many points share each point's
    /// cell of a grid laid over the cloud, puts that count on the series as its colour value, and gives it a
    /// perceptually uniform colour map to read it with.</para>
    /// <para>Counted, not estimated: a two-dimensional kernel estimate walks every other point for every point,
    /// which is unaffordable at exactly the sample sizes that make this chart worth drawing.
    /// <paramref name="configure"/> runs afterwards, so a caller with a better number than crowding - an age, a
    /// score, an error - can assign their own and keep everything else.</para></summary>
    /// <param name="x">The X coordinates.</param>
    /// <param name="y">The Y coordinates, one per X.</param>
    /// <param name="bins">Cells to a side of the counting grid; 0 chooses one from the point count, aiming at
    /// roughly ten points per cell.</param>
    /// <param name="configure">Optional configuration, applied after the density is assigned.</param>
    /// <returns>This builder for chaining.</returns>
    /// <exception cref="ArgumentException">The two axes carry different numbers of points.</exception>
    public AxesBuilder DensityScatter(double[] x, double[] y, Action<ScatterSeries>? configure = null, int bins = 0)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);

        double[] density = Numerics.PointDensity.PerPoint(
            x, y, bins > 0 ? bins : Numerics.PointDensity.BinsFor(x.Length));

        return AddSeries(ax => ax.Scatter(x, y), series =>
        {
            series.C = density;
            series.ColorMap = Styling.ColorMaps.ColorMaps.Viridis;
            configure?.Invoke(series);
        });
    }

    /// <summary>Adds a bar series to the axes.</summary>
    public AxesBuilder Bar(string[] categories, double[] values, Action<BarSeries>? configure = null)
        => AddSeries(ax => ax.Bar(categories, values), configure);

    /// <summary>Adds several bars per category in one call: one series per group, each labelled with the
    /// group's name, drawn side by side inside every category.
    /// <para>The groups are an ordered list, not a map. The order is the picture — it decides which bar sits
    /// where in each category and which colour it takes from the theme's cycle — and a map has no order to give.
    /// <paramref name="configure"/> runs on every one of them, so set the orientation, the alpha or the hatch
    /// once rather than per group.</para></summary>
    /// <param name="categories">The categories along the axis, shared by every group.</param>
    /// <param name="groups">The groups, in the order they should be drawn. At least one, and each must carry
    /// exactly one value per category.</param>
    /// <param name="configure">Optional configuration applied to every series.</param>
    /// <returns>This builder for chaining.</returns>
    /// <exception cref="ArgumentException">No groups, or a group whose value count does not match the
    /// categories — named, so the caller knows which one.</exception>
    public AxesBuilder GroupedBar(string[] categories, IReadOnlyList<BarGroup> groups,
        Action<BarSeries>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(categories);
        ArgumentNullException.ThrowIfNull(groups);
        if (groups.Count == 0)
        {
            throw new ArgumentException("A grouped bar chart needs at least one group.", nameof(groups));
        }

        foreach (var group in groups)
        {
            group.ThrowIfMisaligned(categories.Length, nameof(groups));
        }

        foreach (var group in groups)
        {
            Bar(categories, group.Values, series =>
            {
                series.Label = group.Label;
                configure?.Invoke(series);
            });
        }

        return this;
    }

    /// <summary>Adds a histogram series to the axes.</summary>
    public AxesBuilder Hist(double[] data, int bins = 10, Action<HistogramSeries>? configure = null)
        => AddSeries(ax => ax.Hist(data, bins), configure);

    /// <summary>Adds an ECDF series to the axes.</summary>
    public AxesBuilder Ecdf(double[] data, Action<EcdfSeries>? configure = null)
        => AddSeries(ax => ax.Ecdf(data), configure);

    /// <summary>Adds a stacked area (stackplot) series to the axes.</summary>
    public AxesBuilder StackPlot(double[] x, double[][] ySets, Action<StackedAreaSeries>? configure = null)
        => AddSeries(ax => ax.StackPlot(x, ySets), configure);

    /// <summary>Adds a pie series to the axes.</summary>
    public AxesBuilder Pie(double[] sizes, string[]? labels = null, Action<PieSeries>? configure = null)
        => AddSeries(ax => ax.Pie(sizes, labels), configure);

    /// <summary>Adds a heatmap series to the axes.</summary>
    public AxesBuilder Heatmap(double[,] data, Action<HeatmapSeries>? configure = null)
        => AddSeries(ax => ax.Heatmap(data), configure);

    /// <summary>Adds an image series to the axes.</summary>
    public AxesBuilder Image(double[,] data, Action<ImageSeries>? configure = null)
        => AddSeries(ax => ax.Image(data), configure);

    /// <summary>Adds a 2D histogram (density) series to the axes.</summary>
    public AxesBuilder Histogram2D(double[] x, double[] y, int bins = 20, Action<Histogram2DSeries>? configure = null)
        => AddSeries(ax => ax.Histogram2D(x, y, bins), configure);

    /// <summary>Adds a box plot series to the axes.</summary>
    public AxesBuilder BoxPlot(double[][] datasets, Action<BoxSeries>? configure = null)
        => AddSeries(ax => ax.BoxPlot(datasets), configure);

    /// <summary>Adds a violin series to the axes.</summary>
    public AxesBuilder Violin(double[][] datasets, Action<ViolinSeries>? configure = null)
        => AddSeries(ax => ax.Violin(datasets), configure);

    /// <summary>Adds a hexagonal binning series to the axes.</summary>
    public AxesBuilder Hexbin(double[] x, double[] y, Action<HexbinSeries>? configure = null)
        => AddSeries(ax => ax.Hexbin(x, y), configure);

    /// <summary>Adds a polynomial regression series to the axes.</summary>
    public AxesBuilder Regression(double[] x, double[] y, Action<RegressionSeries>? configure = null)
        => AddSeries(ax => ax.Regression(x, y), configure);

    /// <summary>Adds a kernel density estimation (KDE) series to the axes.</summary>
    public AxesBuilder Kde(double[] data, Action<KdeSeries>? configure = null)
        => AddSeries(ax => ax.Kde(data), configure);

    /// <summary>Adds a contour series to the axes.</summary>
    public AxesBuilder Contour(double[] x, double[] y, double[,] z, Action<ContourSeries>? configure = null)
        => AddSeries(ax => ax.Contour(x, y, z), configure);

    /// <summary>Adds a filled contour series to the axes.</summary>
    public AxesBuilder Contourf(double[] x, double[] y, double[,] z, Action<ContourfSeries>? configure = null)
        => AddSeries(ax => ax.Contourf(x, y, z), configure);

    /// <summary>Adds a stem series to the axes.</summary>
    public AxesBuilder Stem(double[] x, double[] y, Action<StemSeries>? configure = null)
        => AddSeries(ax => ax.Stem(x, y), configure);

    /// <summary>Adds a gauge (speedometer) chart to the axes.</summary>
    public AxesBuilder Gauge(double value, Action<GaugeSeries>? configure = null)
        => AddSeries(ax => ax.Gauge(value), configure);

    /// <summary>Adds a progress bar to the axes.</summary>
    public AxesBuilder ProgressBar(double value, Action<ProgressBarSeries>? configure = null)
        => AddSeries(ax => ax.ProgressBar(value), configure);

    /// <summary>Adds a single-value stat tile (a big headline number + label) to the axes.</summary>
    public AxesBuilder StatTile(double value, Action<StatTileSeries>? configure = null)
        => AddSeries(ax => ax.StatTile(value), configure);

    /// <summary>Adds a bullet graph to the axes — Few's replacement for the radial dial: a measure, an
    /// optional target tick, and optional qualitative bands in one thin strip.</summary>
    /// <param name="value">The measure.</param>
    /// <param name="configure">Optional configuration of the created series.</param>
    /// <returns>This builder for chaining.</returns>
    public AxesBuilder Bullet(double value, Action<BulletGraphSeries>? configure = null)
        => AddSeries(ax => ax.Bullet(value), configure);

    /// <summary>Adds a single-row discrete-state timeline of coloured segments along the X axis.</summary>
    /// <param name="segments">Ordered state segments; each defines a coloured rectangle spanning
    /// <c>[Start, End]</c> in data units with a centred label.</param>
    /// <param name="configure">Optional post-construction configurator.</param>
    public AxesBuilder StateTimeline(IReadOnlyList<StateSegment> segments, Action<StateTimelineSeries>? configure = null)
        => AddSeries(ax => ax.StateTimeline(segments), configure);

    /// <summary>Adds a sparkline to the axes.</summary>
    public AxesBuilder Sparkline(double[] values, Action<SparklineSeries>? configure = null)
        => AddSeries(ax => ax.Sparkline(values), configure);

    /// <summary>Adds a donut chart series to the axes.</summary>
    public AxesBuilder Donut(double[] sizes, string[]? labels = null, Action<DonutSeries>? configure = null)
        => AddSeries(ax => ax.Donut(sizes, labels), configure);

    /// <summary>Adds a bubble chart series to the axes.</summary>
    public AxesBuilder Bubble(double[] x, double[] y, double[] sizes, Action<BubbleSeries>? configure = null)
        => AddSeries(ax => ax.Bubble(x, y, sizes), configure);

    /// <summary>Adds a traditional OHLC bar chart series to the axes.</summary>
    public AxesBuilder OhlcBar(double[] open, double[] high, double[] low, double[] close, string[]? dateLabels = null, Action<OhlcBarSeries>? configure = null)
        => AddSeries(ax => ax.OhlcBar(open, high, low, close, dateLabels), configure);

    /// <summary>Adds a waterfall chart series to the axes: each value is a change on the running total, and a step marked
    /// in <see cref="WaterfallSeries.TotalIndices"/> (set in <paramref name="configure"/>) is a total, a level that stands
    /// on zero in <see cref="WaterfallSeries.TotalColor"/>.</summary>
    public AxesBuilder Waterfall(string[] categories, double[] values, Action<WaterfallSeries>? configure = null)
        => AddSeries(ax => ax.Waterfall(categories, values), configure);

    /// <summary>Adds a funnel chart series to the axes.</summary>
    public AxesBuilder Funnel(string[] labels, double[] values, Action<FunnelSeries>? configure = null)
        => AddSeries(ax => ax.Funnel(labels, values), configure);

    /// <summary>Adds a Gantt chart series to the axes.</summary>
    public AxesBuilder Gantt(string[] tasks, double[] starts, double[] ends, Action<GanttSeries>? configure = null)
        => AddSeries(ax => ax.Gantt(tasks, starts, ends), configure);

    /// <summary>Adds a treemap series to the axes.</summary>
    public AxesBuilder Treemap(TreeNode root, Action<TreemapSeries>? configure = null)
        => AddSeries(ax => ax.Treemap(root), configure);

    /// <summary>Adds a sunburst series to the axes.</summary>
    public AxesBuilder Sunburst(TreeNode root, Action<SunburstSeries>? configure = null)
        => AddSeries(ax => ax.Sunburst(root), configure);

    /// <summary>Adds a dendrogram series to the axes.</summary>
    /// <param name="root">The root <see cref="TreeNode"/>. Internal nodes carry their merge
    /// distance in <see cref="TreeNode.Value"/>.</param>
    /// <param name="configure">Optional callback to configure orientation, cut-height, and
    /// clustering colours on the new <see cref="DendrogramSeries"/>.</param>
    /// <returns>This <see cref="AxesBuilder"/> for chaining.</returns>
    public AxesBuilder Dendrogram(TreeNode root, Action<DendrogramSeries>? configure = null)
        => AddSeries(ax => ax.Dendrogram(root), configure);

    /// <summary>
    /// Adds a nested pie series — inner filled disc showing top-level categories, outer ring
    /// showing each category's sub-breakdown. Convenience wrapper around <see cref="Sunburst"/>
    /// with <see cref="SunburstSeries.InnerRadius"/> pinned at 0. Expects a 2-level
    /// <see cref="TreeNode"/> (root → categories → sub-items); deeper trees still render but
    /// add more concentric rings.
    /// </summary>
    public AxesBuilder NestedPie(TreeNode root, Action<SunburstSeries>? configure = null)
        => Sunburst(root, s =>
        {
            s.InnerRadius = 0;
            configure?.Invoke(s);
        });

    /// <summary>Adds a clustermap series to the axes (heatmap with optional row/column dendrograms).</summary>
    /// <param name="data">The 2D data matrix to render as the heatmap.</param>
    /// <param name="configure">Optional configuration callback for the series.</param>
    public AxesBuilder Clustermap(double[,] data, Action<ClustermapSeries>? configure = null)
        => AddSeries(ax => ax.Clustermap(data), configure);

    /// <summary>Adds a pair-grid series to the axes (N×N matrix of histograms + scatters).</summary>
    /// <param name="variables">The N input variables (one <c>double[]</c> per variable, all equal length).</param>
    /// <param name="configure">Optional configuration callback for the series.</param>
    /// <returns>This <see cref="AxesBuilder"/> for chaining.</returns>
    public AxesBuilder PairGrid(double[][] variables, Action<PairGridSeries>? configure = null)
        => AddSeries(ax => ax.PairGrid(variables), configure);

    /// <summary>Adds a network-graph series to the axes (nodes + edges in 2D).</summary>
    /// <param name="nodes">The graph's nodes.</param>
    /// <param name="edges">The graph's edges.</param>
    /// <param name="configure">Optional configuration callback for the series.</param>
    /// <returns>This <see cref="AxesBuilder"/> for chaining.</returns>
    public AxesBuilder NetworkGraph(
        IReadOnlyList<GraphNode> nodes,
        IReadOnlyList<GraphEdge> edges,
        Action<NetworkGraphSeries>? configure = null)
        => AddSeries(ax => ax.NetworkGraph(nodes, edges), configure);

    /// <summary>Adds a Relative Rotation Graph series to the axes.</summary>
    /// <param name="assetCloses">Per-asset close price series.</param>
    /// <param name="benchmarkCloses">Benchmark close price series (e.g. BTC).</param>
    /// <param name="assetLabels">Display labels — one per asset.</param>
    /// <param name="configure">Optional configuration callback.</param>
    /// <returns>This <see cref="AxesBuilder"/> for chaining.</returns>
    public AxesBuilder RelativeRotation(
        double[][] assetCloses,
        double[] benchmarkCloses,
        string[] assetLabels,
        Action<RelativeRotationSeries>? configure = null)
        => AddSeries(ax => ax.RelativeRotation(assetCloses, benchmarkCloses, assetLabels), configure);

    /// <summary>Adds a Sankey diagram series to the axes.</summary>
    public AxesBuilder Sankey(SankeyNode[] nodes, SankeyLink[] links, Action<SankeySeries>? configure = null)
        => AddSeries(ax => ax.Sankey(nodes, links), configure);

    /// <summary>Adds a polar line series to the axes.</summary>
    public AxesBuilder PolarPlot(double[] r, double[] theta, Action<PolarLineSeries>? configure = null)
        => AddSeries(ax => ax.PolarPlot(r, theta), configure);

    /// <summary>Adds a polar scatter series to the axes.</summary>
    public AxesBuilder PolarScatter(double[] r, double[] theta, Action<PolarScatterSeries>? configure = null)
        => AddSeries(ax => ax.PolarScatter(r, theta), configure);

    /// <summary>Adds a polar bar series to the axes.</summary>
    public AxesBuilder PolarBar(double[] r, double[] theta, Action<PolarBarSeries>? configure = null)
        => AddSeries(ax => ax.PolarBar(r, theta), configure);

    /// <summary>Adds a polar heatmap series to the axes.</summary>
    public AxesBuilder PolarHeatmap(double[,] data, int thetaBins, int rBins, Action<PolarHeatmapSeries>? configure = null)
        => AddSeries(ax => ax.PolarHeatmap(data, thetaBins, rBins), configure);

    /// <summary>Adds a 3D surface series to the axes.</summary>
    public AxesBuilder Surface(double[] x, double[] y, double[,] z, Action<SurfaceSeries>? configure = null)
        => AddSeries(ax => ax.Surface(x, y, z), configure);

    /// <summary>Adds a 3D wireframe series to the axes.</summary>
    public AxesBuilder Wireframe(double[] x, double[] y, double[,] z, Action<WireframeSeries>? configure = null)
        => AddSeries(ax => ax.Wireframe(x, y, z), configure);

    /// <summary>Adds a 3D scatter series to the axes.</summary>
    public AxesBuilder Scatter3D(double[] x, double[] y, double[] z, Action<Scatter3DSeries>? configure = null)
        => AddSeries(ax => ax.Scatter3D(x, y, z), configure);

    /// <summary>Adds a 3D stem series to the axes.</summary>
    public AxesBuilder Stem3D(double[] x, double[] y, double[] z, Action<Stem3DSeries>? configure = null)
        => AddSeries(ax => ax.Stem3D(x, y, z), configure);

    /// <summary>Adds a 3D bar series to the axes.</summary>
    public AxesBuilder Bar3D(double[] x, double[] y, double[] z, Action<Bar3DSeries>? configure = null)
        => AddSeries(ax => ax.Bar3D(x, y, z), configure);

    /// <summary>Adds a planar 3D bar series — flat translucent rectangles placed on Y planes.
    /// Reproduces matplotlib's <c>ax.bar(xs, heights, zs=y, zdir='y')</c> pattern
    /// (also known as "2D bars in different planes" or a "skyscraper plot").</summary>
    public AxesBuilder PlanarBar3D(double[] x, double[] y, double[] z, Action<PlanarBar3DSeries>? configure = null)
        => AddSeries(ax => ax.PlanarBar3D(x, y, z), configure);

    /// <summary>Adds a 3D line (polyline) series to the axes.</summary>
    public AxesBuilder Plot3D(double[] x, double[] y, double[] z, Action<Line3DSeries>? configure = null)
        => AddSeries(ax => ax.Plot3D(x, y, z), configure);

    /// <summary>Adds a triangulated surface series to the axes.</summary>
    public AxesBuilder Trisurf(double[] x, double[] y, double[] z, Action<Trisurf3DSeries>? configure = null)
        => AddSeries(ax => ax.Trisurf(x, y, z), configure);

    /// <summary>Adds a 3D contour series to the axes.</summary>
    public AxesBuilder Contour3D(double[] x, double[] y, double[,] z, Action<Contour3DSeries>? configure = null)
        => AddSeries(ax => ax.Contour3D(x, y, z), configure);

    /// <summary>Adds a 3D quiver (vector field) series to the axes.</summary>
    public AxesBuilder Quiver3D(double[] x, double[] y, double[] z, double[] u, double[] v, double[] w, Action<Quiver3DSeries>? configure = null)
        => AddSeries(ax => ax.Quiver3D(x, y, z, u, v, w), configure);

    /// <summary>Adds a voxel series to the axes.</summary>
    public AxesBuilder Voxels(bool[,,] filled, Action<VoxelSeries>? configure = null)
        => AddSeries(ax => ax.Voxels(filled), configure);

    /// <summary>Adds a 3D text annotation series to the axes.</summary>
    public AxesBuilder Text3D(double x, double y, double z, string text, Action<Text3DSeries>? configure = null)
        => AddSeries(ax => ax.Text3D(x, y, z, text), configure);

    /// <summary>Sets the 3D projection angles for ThreeD coordinate system axes.</summary>
    public AxesBuilder WithProjection(double elevation = 30, double azimuth = -60)
    {
        _axes.Elevation = elevation;
        _axes.Azimuth = azimuth;
        return this;
    }

    /// <summary>Sets camera elevation, azimuth, and optional perspective distance for 3D axes.</summary>
    /// <param name="elevation">Camera elevation above the XY plane in degrees.</param>
    /// <param name="azimuth">Camera azimuth rotation around the Z axis in degrees.</param>
    /// <param name="distance">Perspective camera distance. Null = orthographic. Minimum clamped to 2.0.</param>
    public AxesBuilder WithCamera(double elevation = 30, double azimuth = -60, double? distance = null)
    {
        _axes.Elevation = elevation;
        _axes.Azimuth = azimuth;
        _axes.CameraDistance = distance;
        return this;
    }

    /// <summary>Attaches a directional light source for per-face shading on 3D surfaces and bars.</summary>
    /// <param name="dx">X component of the light direction.</param>
    /// <param name="dy">Y component of the light direction.</param>
    /// <param name="dz">Z component of the light direction.</param>
    /// <param name="ambient">Ambient light intensity [0, 1]. Default 0.3.</param>
    /// <param name="diffuse">Diffuse light intensity [0, 1]. Default 0.7.</param>
    public AxesBuilder WithLighting(double dx, double dy, double dz,
        double ambient = 0.3, double diffuse = 0.7)
    {
        _axes.LightSource = new Rendering.Lighting.DirectionalLight(dx, dy, dz, ambient, diffuse);
        return this;
    }

    /// <summary>Adds a radar (spider) chart series to the axes.</summary>
    public AxesBuilder Radar(string[] categories, double[] values, Action<RadarSeries>? configure = null)
        => AddSeries(ax => ax.Radar(categories, values), configure);

    /// <summary>Adds a quiver (vector field) series to the axes.</summary>
    public AxesBuilder Quiver(double[] x, double[] y, double[] u, double[] v, Action<QuiverSeries>? configure = null)
        => AddSeries(ax => ax.Quiver(x, y, u, v), configure);

    /// <summary>Adds a streamplot (vector field streamlines) series to the axes.</summary>
    public AxesBuilder Streamplot(double[] x, double[] y, double[,] u, double[,] v, Action<StreamplotSeries>? configure = null)
        => AddSeries(ax => ax.Streamplot(x, y, u, v), configure);

    /// <summary>Adds a candlestick (OHLC) series to the axes.</summary>
    public AxesBuilder Candlestick(double[] open, double[] high, double[] low, double[] close, string[]? dateLabels = null, Action<CandlestickSeries>? configure = null)
        => AddSeries(ax => ax.Candlestick(open, high, low, close, dateLabels), configure);

    /// <summary>Adds an error bar series to the axes.</summary>
    public AxesBuilder ErrorBar(double[] x, double[] y, double[] yErrorLow, double[] yErrorHigh, Action<ErrorBarSeries>? configure = null)
        => AddSeries(ax => ax.ErrorBar(x, y, yErrorLow, yErrorHigh), configure);

    /// <summary>Adds a step-function series to the axes.</summary>
    public AxesBuilder Step(double[] x, double[] y, Action<StepSeries>? configure = null)
        => AddSeries(ax => ax.Step(x, y), configure);

    /// <summary>Adds a filled area (fill-between) series to the axes.</summary>
    public AxesBuilder FillBetween(double[] x, double[] y, double[]? y2 = null, Action<AreaSeries>? configure = null)
        => AddSeries(ax => ax.FillBetween(x, y, y2), configure);

    // --- v0.8.0 builder methods ---

    /// <summary>Adds a rug plot series to the axes.</summary>
    public AxesBuilder Rugplot(double[] data, Action<RugplotSeries>? configure = null)
        => AddSeries(ax => ax.Rugplot(data), configure);

    /// <summary>Adds a strip plot series to the axes.</summary>
    public AxesBuilder Stripplot(double[][] datasets, Action<StripplotSeries>? configure = null)
        => AddSeries(ax => ax.Stripplot(datasets), configure);

    /// <summary>Adds an event plot series to the axes.</summary>
    public AxesBuilder Eventplot(double[][] positions, Action<EventplotSeries>? configure = null)
        => AddSeries(ax => ax.Eventplot(positions), configure);

    /// <summary>Adds a broken-bar series to the axes (matplotlib <c>broken_barh</c>).</summary>
    /// <param name="ranges">One <see cref="BarRange"/> array per row; each inner entry defines a
    /// horizontal segment by <c>(Start, Width)</c> in data units.</param>
    /// <param name="configure">Optional post-construction configurator (colour, labels, bar height).</param>
    public AxesBuilder BrokenBarH(BarRange[][] ranges, Action<BrokenBarSeries>? configure = null)
        => AddSeries(ax => ax.BrokenBarH(ranges), configure);

    /// <summary>Adds a count plot series to the axes.</summary>
    public AxesBuilder Countplot(string[] values, Action<CountSeries>? configure = null)
        => AddSeries(ax => ax.Countplot(values), configure);

    /// <summary>Adds a pseudocolor mesh series to the axes.</summary>
    public AxesBuilder Pcolormesh(double[] x, double[] y, double[,] c, Action<PcolormeshSeries>? configure = null)
        => AddSeries(ax => ax.Pcolormesh(x, y, c), configure);

    /// <summary>Adds a residual plot series to the axes.</summary>
    public AxesBuilder Residplot(double[] x, double[] y, Action<ResidualSeries>? configure = null)
        => AddSeries(ax => ax.Residplot(x, y), configure);

    /// <summary>Adds a point plot series to the axes.</summary>
    public AxesBuilder Pointplot(double[][] datasets, Action<PointplotSeries>? configure = null)
        => AddSeries(ax => ax.Pointplot(datasets), configure);

    /// <summary>Adds a swarm plot series to the axes.</summary>
    public AxesBuilder Swarmplot(double[][] datasets, Action<SwarmplotSeries>? configure = null)
        => AddSeries(ax => ax.Swarmplot(datasets), configure);

    /// <summary>Adds a spectrogram series to the axes.</summary>
    public AxesBuilder Spectrogram(double[] signal, int sampleRate = 1, Action<SpectrogramSeries>? configure = null)
        => AddSeries(ax => ax.Spectrogram(signal, sampleRate), configure);

    /// <summary>Adds a table series to the axes.</summary>
    public AxesBuilder Table(string[][] cellData, Action<TableSeries>? configure = null)
        => AddSeries(ax => ax.Table(cellData), configure);

    /// <summary>Adds a tree grid — indented rows with right-aligned value columns (htop's process tree, the
    /// ARIA <c>treegrid</c>): what a treemap stops being able to do once the leaves are many and small.</summary>
    public AxesBuilder TreeGrid(IReadOnlyList<TreeGridRow> rows, Action<TreeGridSeries>? configure = null)
        => AddSeries(ax => ax.TreeGrid(rows), configure);

    /// <summary>Adds a contour series on an unstructured triangular mesh to the axes.</summary>
    public AxesBuilder Tricontour(double[] x, double[] y, double[] z, Action<TricontourSeries>? configure = null)
        => AddSeries(ax => ax.Tricontour(x, y, z), configure);

    /// <summary>Adds a pseudocolor series on a triangular mesh to the axes.</summary>
    public AxesBuilder Tripcolor(double[] x, double[] y, double[] z, Action<TripcolorSeries>? configure = null)
        => AddSeries(ax => ax.Tripcolor(x, y, z), configure);

    /// <summary>Adds a quiver key series to the axes.</summary>
    public AxesBuilder QuiverKey(double x, double y, double u, string label, Action<QuiverKeySeries>? configure = null)
        => AddSeries(ax => ax.QuiverKey(x, y, u, label), configure);

    /// <summary>Adds a wind barb series to the axes.</summary>
    public AxesBuilder Barbs(double[] x, double[] y, double[] speed, double[] direction, Action<BarbsSeries>? configure = null)
        => AddSeries(ax => ax.Barbs(x, y, speed, direction), configure);

    /// <summary>Adds a <see cref="SignalSeries"/> with uniform sample rate to the axes.</summary>
    public AxesBuilder Signal(double[] y, double sampleRate = 1.0, double xStart = 0.0, Action<SignalSeries>? configure = null)
        => AddSeries(ax => ax.Signal(y, sampleRate, xStart), configure);

    /// <summary>Adds a <see cref="SignalXYSeries"/> with monotonically ascending X values to the axes.</summary>
    public AxesBuilder SignalXY(double[] x, double[] y, Action<SignalXYSeries>? configure = null)
        => AddSeries(ax => ax.SignalXY(x, y), configure);

    // --- Streaming series (v1.4.0) ---

    /// <summary>Adds a streaming line series backed by a ring buffer.</summary>
    /// <param name="capacity">Maximum data points retained. Default 10,000.</param>
    /// <param name="configure">Optional configuration callback.</param>
    /// <returns>The created <see cref="StreamingLineSeries"/> for appending data.</returns>
    public StreamingLineSeries StreamingPlot(int capacity = 10_000, Action<StreamingLineSeries>? configure = null)
        => AddStreamingSeries(new StreamingLineSeries(capacity), configure);

    /// <summary>Adds a streaming scatter series backed by a ring buffer.</summary>
    /// <param name="capacity">Maximum data points retained. Default 10,000.</param>
    /// <param name="configure">Optional configuration callback.</param>
    /// <returns>The created <see cref="StreamingScatterSeries"/> for appending data.</returns>
    public StreamingScatterSeries StreamingScatter(int capacity = 10_000, Action<StreamingScatterSeries>? configure = null)
        => AddStreamingSeries(new StreamingScatterSeries(capacity), configure);

    /// <summary>Adds a streaming signal series (uniform sample rate, Y-only storage).</summary>
    /// <param name="capacity">Maximum samples retained. Default 100,000.</param>
    /// <param name="sampleRate">Samples per X-unit. Default 1.0.</param>
    /// <param name="configure">Optional configuration callback.</param>
    /// <returns>The created <see cref="StreamingSignalSeries"/> for appending samples.</returns>
    public StreamingSignalSeries StreamingSignal(int capacity = 100_000, double sampleRate = 1.0, Action<StreamingSignalSeries>? configure = null)
        => AddStreamingSeries(new StreamingSignalSeries(capacity, sampleRate), configure);

    /// <summary>Adds a streaming candlestick series backed by four parallel ring buffers.</summary>
    /// <param name="capacity">Maximum bars retained. Default 5,000.</param>
    /// <param name="configure">Optional configuration callback.</param>
    /// <returns>The created <see cref="StreamingCandlestickSeries"/> for appending OHLC bars.</returns>
    public StreamingCandlestickSeries StreamingCandlestick(int capacity = 5_000, Action<StreamingCandlestickSeries>? configure = null)
        => AddStreamingSeries(new StreamingCandlestickSeries(capacity), configure);

    private T AddStreamingSeries<T>(T series, Action<T>? configure) where T : ChartSeries
    {
        configure?.Invoke(series);
        _axes.AddSeries(series);
        return series;
    }

    private AxesBuilder AddSeries<T>(Func<Axes, T> factory, Action<T>? configure) where T : ISeries
    {
        var series = factory(_axes);
        configure?.Invoke(series);
        return this;
    }

    /// <summary>Adds a pre-constructed series to this axes. Used by extension packages
    /// (e.g. <c>MatPlotLibNet.Geo</c>) that ship their own series types.</summary>
    /// <typeparam name="T">A type implementing <see cref="ISeries"/>.</typeparam>
    /// <param name="series">The series instance to add.</param>
    /// <param name="configure">Optional configuration callback applied after the series is added.</param>
    /// <returns>This <see cref="AxesBuilder"/> for fluent chaining.</returns>
    public AxesBuilder AddSeries<T>(T series, Action<T>? configure = null) where T : ISeries
    {
        _axes.AddSeries(series);
        configure?.Invoke(series);
        return this;
    }

    /// <summary>A fresh <see cref="Models.Series.ChartSeries.ColorGroup"/> value for a verb that draws several
    /// series as one visually grouped unit (<see cref="Axes.NextColorGroup"/>).</summary>
    /// <returns>One more than the highest group already held by a series of this axes, or 0 when none holds one.</returns>
    internal int NextColorGroup() => _axes.NextColorGroup();

    // --- Intuitive indicator shortcuts (auto-resolve price data from axes) ---

    /// <summary>Adds a Simple Moving Average overlay. Auto-extracts price data from the last series on the axes.</summary>
    public AxesBuilder Sma(int period, Action<Indicators.Sma>? configure = null)
    {
        var indicator = new Indicators.Sma(GetPriceData(), period);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds an Exponential Moving Average overlay.</summary>
    public AxesBuilder Ema(int period, Action<Indicators.Ema>? configure = null)
    {
        var indicator = new Indicators.Ema(GetPriceData(), period);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds Bollinger Bands overlay.</summary>
    public AxesBuilder BollingerBands(int period = 20, double stdDev = 2.0, Action<Indicators.BollingerBands>? configure = null)
    {
        var indicator = new Indicators.BollingerBands(GetPriceData(), period, stdDev);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds an RSI panel indicator.</summary>
    public AxesBuilder Rsi(double[] prices, int period = 14, Action<Indicators.Rsi>? configure = null)
    {
        var indicator = new Indicators.Rsi(prices, period);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds a Williams %R panel indicator.</summary>
    public AxesBuilder WilliamsR(Indicators.OhlcvBars bars, int period = 14, Action<Indicators.WilliamsR>? configure = null)
    {
        var indicator = new Indicators.WilliamsR(bars, period);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds an On-Balance Volume panel indicator.</summary>
    public AxesBuilder Obv(double[] close, double[] volume, Action<Indicators.Obv>? configure = null)
    {
        var indicator = new Indicators.Obv(close, volume);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds a Commodity Channel Index panel indicator.</summary>
    public AxesBuilder Cci(Indicators.OhlcvBars bars, int period = 20, Action<Indicators.Cci>? configure = null)
    {
        var indicator = new Indicators.Cci(bars, period);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds a Parabolic SAR overlay to the price axes.</summary>
    public AxesBuilder ParabolicSar(double[] high, double[] low, double step = 0.02, double max = 0.2, Action<Indicators.ParabolicSar>? configure = null)
    {
        var indicator = new Indicators.ParabolicSar(high, low, step, max);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds a Garman-Klass volatility panel indicator.</summary>
    public AxesBuilder GarmanKlass(Indicators.OhlcvBars bars, int period = 20, Action<Indicators.GarmanKlass>? configure = null)
    {
        var indicator = new Indicators.GarmanKlass(bars, period);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds a Yang-Zhang volatility panel indicator.</summary>
    public AxesBuilder YangZhang(Indicators.OhlcvBars bars, int period = 20, Action<Indicators.YangZhang>? configure = null)
    {
        var indicator = new Indicators.YangZhang(bars, period);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds a Kaufman Efficiency Ratio panel indicator. Output bounded to [0, 1].</summary>
    public AxesBuilder KaufmanEfficiencyRatio(double[] prices, int period = 10,
        Action<Indicators.KaufmanEfficiencyRatio>? configure = null)
    {
        var indicator = new Indicators.KaufmanEfficiencyRatio(prices, period);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds a CUSUM filter panel indicator (regime-break detector).</summary>
    public AxesBuilder Cusum(double[] prices, double threshold, double drift = 0.0,
        Action<Indicators.Cusum>? configure = null)
    {
        var indicator = new Indicators.Cusum(prices, threshold, drift);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds a fractional-differentiation panel indicator (Lopez de Prado §5.5).</summary>
    public AxesBuilder Ffd(double[] prices, double d = 0.4, double tolerance = 1e-3,
        Action<Indicators.FractionalDifferentiation>? configure = null)
    {
        var indicator = new Indicators.FractionalDifferentiation(prices, d, tolerance);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds an Amihud illiquidity panel indicator.</summary>
    public AxesBuilder AmihudIlliquidity(Indicators.OhlcvBars bars, int period = 20,
        Action<Indicators.AmihudIlliquidity>? configure = null)
    {
        var indicator = new Indicators.AmihudIlliquidity(bars, period);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds a Corwin-Schultz bid-ask-spread panel indicator.</summary>
    public AxesBuilder CorwinSchultz(Indicators.OhlcvBars bars, int period = 20,
        Action<Indicators.CorwinSchultz>? configure = null)
    {
        var indicator = new Indicators.CorwinSchultz(bars, period);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds a VPIN (Volume-Synchronized Probability of Informed Trading) panel indicator.
    /// Output bounded to [0, 1].</summary>
    public AxesBuilder Vpin(Indicators.OhlcvBars bars, int bucketPeriod = 50, int sigmaPeriod = 50,
        Action<Indicators.Vpin>? configure = null)
    {
        var indicator = new Indicators.Vpin(bars, bucketPeriod, sigmaPeriod);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds a Roll bid-ask-spread panel indicator.</summary>
    public AxesBuilder RollSpread(double[] prices, int period = 20,
        Action<Indicators.RollSpread>? configure = null)
    {
        var indicator = new Indicators.RollSpread(prices, period);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds a Laguerre RSI panel indicator (Ehlers 2004). Output bounded to [0, 1].</summary>
    public AxesBuilder LaguerreRsi(double[] prices, double alpha = 0.2,
        Action<Indicators.LaguerreRsi>? configure = null)
    {
        var indicator = new Indicators.LaguerreRsi(prices, alpha);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds a MAMA/FAMA adaptive-moving-average overlay (Ehlers 2001). Two lines.</summary>
    public AxesBuilder MamaFama(double[] prices, double fastLimit = 0.5, double slowLimit = 0.05,
        Action<Indicators.MamaFama>? configure = null)
    {
        var indicator = new Indicators.MamaFama(prices, fastLimit, slowLimit);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds a Squeeze Momentum (LazyBear) panel indicator.</summary>
    public AxesBuilder SqueezeMomentum(Indicators.OhlcvBars bars,
        int period = 20, double bbMult = 2.0, double kcMult = 1.5,
        Action<Indicators.SqueezeMomentum>? configure = null)
    {
        var indicator = new Indicators.SqueezeMomentum(bars, period, bbMult, kcMult);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds a BOCPD (Bayesian Online Changepoint Detection) panel indicator.
    /// Output is per-bar changepoint probability in [0, 1].</summary>
    public AxesBuilder Bocpd(double[] prices, double hazard = 0.01,
        double priorVariance = 1.0, int maxRunLength = 500,
        Action<Indicators.Bocpd>? configure = null)
    {
        var indicator = new Indicators.Bocpd(prices, hazard, priorVariance, maxRunLength);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds a Turbulence Index panel indicator (Kritzman &amp; Li 2010).
    /// Rolling Mahalanobis distance — crisis detector.</summary>
    public AxesBuilder TurbulenceIndex(double[][] features, int window = 252,
        double regularization = 1e-6,
        Action<Indicators.TurbulenceIndex>? configure = null)
    {
        var indicator = new Indicators.TurbulenceIndex(features, window, regularization);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds a Dispersion Index panel indicator — population stddev across signals per bar.</summary>
    public AxesBuilder DispersionIndex(double[][] signals,
        Action<Indicators.DispersionIndex>? configure = null)
    {
        var indicator = new Indicators.DispersionIndex(signals);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds a Permutation Entropy panel indicator (Bandt &amp; Pompe 2002). Output in [0, 1].</summary>
    public AxesBuilder PermutationEntropy(double[] prices, int order = 4, int window = 100,
        Action<Indicators.PermutationEntropy>? configure = null)
    {
        var indicator = new Indicators.PermutationEntropy(prices, order, window);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds a Haar-DWT energy-ratio panel indicator. Output in [0, 1] — fraction of
    /// band energy at the target detail level.</summary>
    public AxesBuilder WaveletEnergyRatio(double[] prices, int window = 64, int level = 0,
        Action<Indicators.WaveletEnergyRatio>? configure = null)
    {
        var indicator = new Indicators.WaveletEnergyRatio(prices, window, level);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds a Haar-DWT entropy panel indicator. Output in [0, 1] — Shannon entropy
    /// of the energy distribution over wavelet bands.</summary>
    public AxesBuilder WaveletEntropy(double[] prices, int window = 64,
        Action<Indicators.WaveletEntropy>? configure = null)
    {
        var indicator = new Indicators.WaveletEntropy(prices, window);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds an Ehlers Cyber Cycle panel indicator (Ehlers 2002).</summary>
    public AxesBuilder CyberCycle(double[] prices, double alpha = 0.07,
        Action<Indicators.CyberCycle>? configure = null)
    {
        var indicator = new Indicators.CyberCycle(prices, alpha);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds an Ehlers Roofing Filter panel indicator (band-pass, Ehlers 2014).</summary>
    public AxesBuilder RoofingFilter(double[] prices, int hpPeriod = 48, int lpPeriod = 10,
        Action<Indicators.RoofingFilter>? configure = null)
    {
        var indicator = new Indicators.RoofingFilter(prices, hpPeriod, lpPeriod);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds an Ehlers Sinewave indicator (Ehlers 2002) — two lines + cycle/trend flag.</summary>
    public AxesBuilder EhlersSineWave(double[] prices,
        Action<Indicators.EhlersSineWave>? configure = null)
    {
        var indicator = new Indicators.EhlersSineWave(prices);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds an Ehlers Adaptive Stochastic panel indicator. Output in [0, 100].</summary>
    public AxesBuilder AdaptiveStochastic(Indicators.OhlcvBars bars,
        int smoothingPeriod = 3,
        Action<Indicators.AdaptiveStochastic>? configure = null)
    {
        var indicator = new Indicators.AdaptiveStochastic(bars, smoothingPeriod);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds an Aroon Oscillator panel indicator (Chande 1995). Output in [-100, 100].</summary>
    public AxesBuilder AroonOscillator(Indicators.OhlcvBars bars, int period = 25,
        Action<Indicators.AroonOscillator>? configure = null)
    {
        var indicator = new Indicators.AroonOscillator(bars, period);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds an Elder Force Index panel indicator (Elder 1993). Signed momentum.</summary>
    public AxesBuilder ForceIndex(Indicators.OhlcvBars bars, int period = 13,
        Action<Indicators.ForceIndex>? configure = null)
    {
        var indicator = new Indicators.ForceIndex(bars, period);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds a Relative Vigor Index panel indicator (Ehlers 2002). Two lines (RVI + Signal).</summary>
    public AxesBuilder RelativeVigorIndex(Indicators.OhlcvBars bars,
        int period = 10,
        Action<Indicators.RelativeVigorIndex>? configure = null)
    {
        var indicator = new Indicators.RelativeVigorIndex(bars, period);
        return ApplyIndicator(indicator, configure);
    }

    // ── v1.9.0 Tier 3a — Volume / Money Flow ──

    /// <summary>Adds an Ease of Movement panel indicator (Arms 1975). Signed — positive =
    /// price rises easily on light volume (accumulation), negative = easy decline.</summary>
    public AxesBuilder EaseOfMovement(Indicators.OhlcvBars bars,
        int period = 14, double scale = 1_000_000,
        Action<Indicators.EaseOfMovement>? configure = null)
    {
        var indicator = new Indicators.EaseOfMovement(bars, period, scale);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds a Klinger Volume Oscillator panel indicator (Klinger 1977). Two lines —
    /// KVO (fast EMA − slow EMA of volume force) + Signal (EMA of KVO). Crossovers are the
    /// standard buy / sell trigger.</summary>
    public AxesBuilder KlingerVolumeOscillator(Indicators.OhlcvBars bars,
        int fastPeriod = 34, int slowPeriod = 55, int signalPeriod = 13,
        Action<Indicators.KlingerVolumeOscillator>? configure = null)
    {
        var indicator = new Indicators.KlingerVolumeOscillator(bars, fastPeriod, slowPeriod, signalPeriod);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds a Twiggs Money Flow panel indicator (Twiggs 2002). Output bounded in
    /// <c>[-1, 1]</c>; positive = accumulation, negative = distribution.</summary>
    public AxesBuilder TwiggsMoneyFlow(Indicators.OhlcvBars bars,
        int period = 21,
        Action<Indicators.TwiggsMoneyFlow>? configure = null)
    {
        var indicator = new Indicators.TwiggsMoneyFlow(bars, period);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds a VWAP Z-Score panel indicator — standardised deviation from the rolling
    /// volume-weighted average price. Mean-reversion signal; extremes beyond ±2σ mark
    /// dislocations from fair value.</summary>
    public AxesBuilder VwapZScore(Indicators.OhlcvBars bars, int window = 20,
        Action<Indicators.VwapZScore>? configure = null)
    {
        var indicator = new Indicators.VwapZScore(bars, window);
        return ApplyIndicator(indicator, configure);
    }

    // ── v1.9.0 Tier 3b — Trend & Transform ──

    /// <summary>Adds an Ehlers Center-of-Gravity Oscillator panel (Ehlers 2002) — linearly
    /// weighted price average centred on zero. Recent prices weigh more than older ones.</summary>
    public AxesBuilder CgOscillator(double[] prices, int period = 10,
        Action<Indicators.CgOscillator>? configure = null)
    {
        var indicator = new Indicators.CgOscillator(prices, period);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds an Ehlers Inverse Fisher Transform panel (Ehlers 2004) — <c>tanh(scale·x)</c>
    /// squash that sharpens any bounded oscillator. Output in <c>[-1, +1]</c>. Apply to RSI,
    /// stochastic, CCI, etc. to produce cleaner crossover signals.</summary>
    public AxesBuilder InverseFisherTransform(double[] input, double scale = 1.0,
        Action<Indicators.InverseFisherTransform>? configure = null)
    {
        var indicator = new Indicators.InverseFisherTransform(input, scale);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds a Supertrend overlay (Seban 2008) — ATR-based trailing stop with direction
    /// flips. Renders as a single line below price in uptrends, above price in downtrends.</summary>
    public AxesBuilder Supertrend(Indicators.OhlcvBars bars,
        int period = 10, double multiplier = 3.0,
        Action<Indicators.Supertrend>? configure = null)
    {
        var indicator = new Indicators.Supertrend(bars, period, multiplier);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds a Yang-Zhang volatility ratio panel — <c>short-window YZ / long-window YZ</c>.
    /// Ratio &gt; 1 = vol expansion (possible breakout); &lt; 1 = consolidation. Reuses
    /// <see cref="Indicators.YangZhang"/> for both numerator and denominator.</summary>
    public AxesBuilder YangZhangVolRatio(Indicators.OhlcvBars bars,
        int shortWindow = 20, int longWindow = 60,
        Action<Indicators.YangZhangVolRatio>? configure = null)
    {
        var indicator = new Indicators.YangZhangVolRatio(bars, shortWindow, longWindow);
        return ApplyIndicator(indicator, configure);
    }

    // ── v1.9.0 Tier 3c — Advanced (remaining Ehlers + cross-asset) ──

    /// <summary>Adds an Ehlers Decycler overlay (Ehlers 2015) — price minus the one-pole
    /// high-pass filter output. Removes the dominant cycle band, leaving the trend.</summary>
    public AxesBuilder Decycler(double[] prices, int hpPeriod = 60,
        Action<Indicators.Decycler>? configure = null)
    {
        var indicator = new Indicators.Decycler(prices, hpPeriod);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds an Ehlers Instantaneous Trendline overlay (Ehlers 2001) — adaptive
    /// linearly-weighted MA whose window length equals the Hilbert-derived dominant cycle.</summary>
    public AxesBuilder EhlersITrend(double[] prices,
        Action<Indicators.EhlersITrend>? configure = null)
    {
        var indicator = new Indicators.EhlersITrend(prices);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds an Ehlers SuperSmoother panel (Ehlers 2013) — two-pole Butterworth
    /// low-pass. Applicable to any numerical series (price, indicator output, residuals).</summary>
    public AxesBuilder EhlersSuperSmoother(double[] input, int period = 10,
        Action<Indicators.EhlersSuperSmoother>? configure = null)
    {
        var indicator = new Indicators.EhlersSuperSmoother(input, period);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Adds a Transfer Entropy computation (Schreiber 2000). Scalar output in nats —
    /// measures directional information flow from <paramref name="source"/> to
    /// <paramref name="target"/>. Unlike line-emitting indicators, <c>Apply</c> is a no-op;
    /// callers typically consume <c>Compute()</c> for display as an annotation or ML feature.</summary>
    public AxesBuilder TransferEntropy(double[] source, double[] target,
        int bins = 8, int lag = 1,
        Action<Indicators.TransferEntropy>? configure = null)
    {
        var indicator = new Indicators.TransferEntropy(source, target, bins, lag);
        return ApplyIndicator(indicator, configure);
    }

    /// <summary>Applies any <see cref="Indicators.IIndicator"/> to the current axes.</summary>
    /// <remarks>Generic entry point for indicators that don't have a dedicated shortcut
    /// (e.g. <c>Macd</c>, <c>Stochastic</c>, <c>Atr</c>, <c>Ichimoku</c>). The indicator instance
    /// is constructed by the caller. Same method as <see cref="AddIndicator"/>, under the name this overload
    /// shipped with first; kept so neither call site breaks.</remarks>
    public AxesBuilder Indicator(Indicators.IIndicator indicator) => AddIndicator(indicator);

    /// <summary>Adds a buy signal marker at the given position.</summary>
    public AxesBuilder BuyAt(double x, double y, Action<SignalMarker>? configure = null)
        => AddSignal(x, y, SignalDirection.Buy, configure);

    /// <summary>Adds a sell signal marker at the given position.</summary>
    public AxesBuilder SellAt(double x, double y, Action<SignalMarker>? configure = null)
        => AddSignal(x, y, SignalDirection.Sell, configure);

    /// <summary>The shared tail every indicator verb (Sma, Rsi, KlingerVolumeOscillator, ...) ends with: nudge a
    /// bar-slot-context indicator onto the category slot's centre, let the caller configure it, apply it to the
    /// axes, and return this builder for chaining.</summary>
    private AxesBuilder ApplyIndicator<T>(T indicator, Action<T>? configure) where T : Indicators.Indicator
    {
        if (IsBarSlotContext()) indicator.Offset = CategorySlot.CentreOffset;
        configure?.Invoke(indicator);
        indicator.Apply(_axes);
        return this;
    }

    /// <summary>Extracts Y data from the last series on the axes for indicator computation.</summary>
    // Returns true when this panel uses bar-slot X coordinates — either because it contains
    // a categorical series (candlestick/bar), or because UseBarSlotX() was called explicitly
    // (e.g. for a separate oscillator panel aligned to the same bar-slot X axis).
    private bool IsBarSlotContext() => _isBarSlotContext || HasCategoricalSeries();

    private bool HasCategoricalSeries() =>
        _axes.Series.Any(s => s is ICategoryLabeled);

    private double[] GetPriceData()
    {
        // Prefer the most recently added price series, searched across the WHOLE list, not just the last
        // one added: this lets users *chain* indicators (.Plot(x, close).Sma(20).Sma(5) computes SMA(5) over
        // the SMA(20) output, not the raw close again), and it already resolves `.Candlestick(o,h,l,c).Sma(20)`
        // to Close too — CandlestickSeries and OhlcBarSeries are themselves IPriceSeries (via OhlcSeries), so
        // a separate OHLC-only fallback search here would never find anything this one hadn't already.
        var last = _axes.Series.LastOrDefault(s => s is IPriceSeries);
        return last is IPriceSeries price
            ? price.PriceData
            : throw new InvalidOperationException("No price data found. Add a series with Y data before calling indicator shortcuts.");
    }

    /// <summary>Builds and returns a new <see cref="Axes"/> at the specified grid position.</summary>
    /// <param name="rows">Total number of rows in the grid.</param>
    /// <param name="cols">Total number of columns in the grid.</param>
    /// <param name="index">Zero-based linear index of this subplot within the grid.</param>
    internal Axes Build(int rows, int cols, int index)
    {
        _axes.GridRows = rows;
        _axes.GridCols = cols;
        _axes.GridIndex = index;
        return _axes;
    }

    /// <summary>Builds and returns a new <see cref="Axes"/> at the specified grid position.</summary>
    /// <param name="position">The row/column span that this subplot occupies.</param>
    internal Axes Build(GridPosition position)
    {
        _axes.GridPosition = position;
        return _axes;
    }

    /// <summary>Adds an inset axes at the specified bounds within this axes and configures it via
    /// the supplied builder. This is the canonical form; the loose-doubles
    /// <see cref="AddInset(double,double,double,double,System.Action{AxesBuilder})"/> overload forwards here.</summary>
    /// <param name="bounds">The fractional position and size of the inset within this axes.</param>
    /// <param name="configure">Configures the inset's series and axes.</param>
    public AxesBuilder AddInset(InsetBounds bounds, Action<AxesBuilder> configure)
    {
        var insetBuilder = new AxesBuilder();
        configure(insetBuilder);
        var inset = _axes.AddInset(bounds);
        inset.CopyAxesState(insetBuilder._axes, Theme.Default);
        return this;
    }

    /// <summary>Adds an inset axes at the specified fractional position within this axes.
    /// Convenience overload that forwards to <see cref="AddInset(InsetBounds,System.Action{AxesBuilder})"/>.</summary>
    public AxesBuilder AddInset(double x, double y, double width, double height, Action<AxesBuilder> configure)
        => AddInset(new InsetBounds(x, y, width, height), configure);
}

/// <summary>Fluent builder for configuring the first right-hand Y axis and adding series that scale against it.</summary>
/// <remarks>Obsolete: use <see cref="YAxisSlotBuilder"/>, which <see cref="AxesBuilder.WithAddRightYAxis"/> and
/// <see cref="AxesBuilder.WithAddLeftYAxis"/> hand to their callback. This builder is handed out by the obsolete
/// <c>WithSecondaryYAxis</c> only. The axis renders ticks and labels on the right side
/// of the plot and uses an independent Y-axis data range. It is <see cref="YAxisSlotBuilder"/> over that axis with the
/// return types this builder was published with: every verb forwards to the one builder, so the two cannot differ.</remarks>
[Obsolete("Use YAxisSlotBuilder, the builder WithAddRightYAxis and WithAddLeftYAxis hand to their callback; it has every verb this builder has.")]
public sealed class SecondaryAxisBuilder
{
    private readonly YAxisSlotBuilder _slot;

    /// <summary>Initializes a new <see cref="SecondaryAxisBuilder"/> over the first right-hand axis.</summary>
    /// <param name="slot">The builder of that axis.</param>
    internal SecondaryAxisBuilder(YAxisSlotBuilder slot) => _slot = slot;

    /// <summary>Sets the label displayed alongside the right-side Y-axis.</summary>
    public SecondaryAxisBuilder SetYLabel(string label) { _slot.SetYLabel(label); return this; }

    /// <summary>Colours the right-hand Y axis in one call: its spine, its tick marks, its tick labels and its title,
    /// so the axis reads as belonging to the series drawn in that colour. Keep <see cref="SetYLabel"/> naming the
    /// series and its unit, so the colour is never the only thing that says which series the axis belongs to.</summary>
    /// <param name="color">The colour all four take.</param>
    /// <returns>This builder, for chaining.</returns>
    public SecondaryAxisBuilder SetYAxisColor(Color color) { _slot.SetYAxisColor(color); return this; }

    /// <summary>Sets explicit min/max limits for the right-hand Y-axis data range.</summary>
    public SecondaryAxisBuilder SetYLim(double min, double max) { _slot.SetYLim(min, max); return this; }

    /// <summary>Adds a line series plotted against the right-hand Y-axis.</summary>
    public SecondaryAxisBuilder Plot(double[] x, double[] y, Action<LineSeries>? configure = null)
    {
        _slot.Plot(x, y, configure);
        return this;
    }

    /// <summary>Adds a scatter series plotted against the right-hand Y-axis.</summary>
    public SecondaryAxisBuilder Scatter(double[] x, double[] y, Action<ScatterSeries>? configure = null)
    {
        _slot.Scatter(x, y, configure);
        return this;
    }

    /// <summary>Adds a STREAMING line series against the right-hand Y-axis, so a live chart can carry two
    /// units at once — one appended on the left, one on the right.</summary>
    /// <param name="capacity">Maximum data points retained in the ring buffer. Default 10,000.</param>
    /// <param name="configure">Optional configuration callback.</param>
    /// <returns>The created series, so the caller can <c>AppendPoint</c> to it from any thread — unlike the
    /// other methods here it does NOT return the builder, because the series IS the handle live data needs
    /// (the same shape <see cref="AxesBuilder.StreamingPlot"/> already has on the primary axis).</returns>
    public StreamingLineSeries StreamingPlot(int capacity = 10_000, Action<StreamingLineSeries>? configure = null) =>
        _slot.StreamingPlot(capacity, configure);

    /// <summary>Draws the name of the series added last to this axis at the end of its line, in the line's
    /// colour, instead of in the legend. Applies to a line or a streaming line
    /// (<see cref="IHasLabelPlacement"/>) that carries a label; when the last series cannot
    /// carry a placement, or there is none, nothing changes.</summary>
    /// <returns>This builder, for chaining.</returns>
    public SecondaryAxisBuilder WithDirectLabel() { _slot.WithDirectLabel(); return this; }
}

/// <summary>Fluent builder for configuring ONE outward-stacked Y axis, on either side.</summary>
/// <remarks>Obsolete: use <see cref="YAxisSlotBuilder"/>, which <see cref="AxesBuilder.WithAddRightYAxis"/> and
/// <see cref="AxesBuilder.WithAddLeftYAxis"/> hand to their callback. This builder is handed out by the obsolete
/// <c>WithOutwardYAxis</c> only; it is over the axis the call just stacked, so it reaches the right axis whatever
/// number already stand on that side. It is <see cref="YAxisSlotBuilder"/> over that axis with the return types this
/// builder was published with, and carries no <c>SetYAxisColor</c>: the one builder has it, and so does every axis.</remarks>
[Obsolete("Use YAxisSlotBuilder, the builder WithAddRightYAxis and WithAddLeftYAxis hand to their callback; it has every verb this builder has.")]
public sealed class OutwardYAxisBuilder
{
    private readonly YAxisSlotBuilder _slot;

    /// <summary>Initializes a new <see cref="OutwardYAxisBuilder"/> over the axis just stacked.</summary>
    /// <param name="slot">The builder of that axis.</param>
    internal OutwardYAxisBuilder(YAxisSlotBuilder slot) => _slot = slot;

    /// <summary>Sets the label displayed alongside this axis.</summary>
    public OutwardYAxisBuilder SetYLabel(string label) { _slot.SetYLabel(label); return this; }

    /// <summary>Sets explicit min/max limits for this axis' data range.</summary>
    public OutwardYAxisBuilder SetYLim(double min, double max) { _slot.SetYLim(min, max); return this; }

    /// <summary>Adds a line series plotted against this axis.</summary>
    public OutwardYAxisBuilder Plot(double[] x, double[] y, Action<LineSeries>? configure = null)
    {
        _slot.Plot(x, y, configure);
        return this;
    }

    /// <summary>Adds a scatter series plotted against this axis.</summary>
    public OutwardYAxisBuilder Scatter(double[] x, double[] y, Action<ScatterSeries>? configure = null)
    {
        _slot.Scatter(x, y, configure);
        return this;
    }

    /// <summary>Adds a STREAMING line series against this axis, so a live chart can carry another unit at once.</summary>
    /// <param name="capacity">Maximum data points retained in the ring buffer. Default 10,000.</param>
    /// <param name="configure">Optional configuration callback.</param>
    /// <returns>The created series, so the caller can <c>AppendPoint</c> to it from any thread — unlike the
    /// other methods here it does NOT return the builder, matching <see cref="YAxisSlotBuilder.StreamingPlot"/>.</returns>
    public StreamingLineSeries StreamingPlot(int capacity = 10_000, Action<StreamingLineSeries>? configure = null) =>
        _slot.StreamingPlot(capacity, configure);

    /// <summary>Draws the name of the series added last at the end of its line instead of in the legend, on an axis on
    /// either side: the end labels stand in one column right of the plot, at the height this axis puts the end of the
    /// line at.</summary>
    /// <returns>This builder, for chaining.</returns>
    public OutwardYAxisBuilder WithDirectLabel() { _slot.WithDirectLabel(); return this; }
}

/// <summary>Fluent builder for configuring a secondary X-axis (top edge) and adding series that scale against it.</summary>
/// <remarks>Obtained via <see cref="AxesBuilder.WithSecondaryXAxis"/>. The secondary axis renders ticks and
/// labels on the top of the plot and uses an independent X-axis data range.</remarks>
public sealed class SecondaryXAxisBuilder
{
    private readonly Axes _axes;

    /// <summary>Initializes a new <see cref="SecondaryXAxisBuilder"/> attached to the given axes.</summary>
    internal SecondaryXAxisBuilder(Axes axes) => _axes = axes;

    /// <summary>Sets the label displayed above the top-edge X-axis.</summary>
    public SecondaryXAxisBuilder SetXLabel(string label) { _axes.SecondaryXAxis!.Label = label; return this; }

    /// <summary>Colours the top X axis in one call: its spine, its tick marks, its tick labels and its title.</summary>
    /// <param name="color">The colour all four take.</param>
    /// <returns>This builder, for chaining.</returns>
    public SecondaryXAxisBuilder SetXAxisColor(Color color)
    {
        _axes.ApplyAxisColor(_axes.SecondaryXAxis!, SpineEdge.Top, color);
        return this;
    }

    /// <summary>Sets explicit min/max limits for the secondary X-axis data range.</summary>
    public SecondaryXAxisBuilder SetXLim(double min, double max) { _axes.SecondaryXAxis!.Min = min; _axes.SecondaryXAxis!.Max = max; return this; }

    /// <summary>Adds a line series plotted against the secondary X-axis.</summary>
    public SecondaryXAxisBuilder PlotXSecondary(double[] x, double[] y, Action<LineSeries>? configure = null)
    {
        var series = _axes.PlotXSecondary(x, y);
        configure?.Invoke(series);
        return this;
    }

    /// <summary>Adds a scatter series plotted against the secondary X-axis.</summary>
    public SecondaryXAxisBuilder ScatterXSecondary(double[] x, double[] y, Action<ScatterSeries>? configure = null)
    {
        var series = _axes.ScatterXSecondary(x, y);
        configure?.Invoke(series);
        return this;
    }
}
