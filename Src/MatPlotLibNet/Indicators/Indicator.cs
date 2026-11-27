// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;
using System.Numerics.Tensors;

namespace MatPlotLibNet.Indicators;

/// <summary>Base class for technical indicators with shared styling properties.</summary>
/// <remarks>Concrete indicators inherit <see cref="Color"/>, <see cref="Label"/>, <see cref="LineWidth"/>,
/// <see cref="LineStyle"/>, and <see cref="Offset"/> and implement <see cref="Apply"/> to compute derived data
/// and inject series into the axes.</remarks>
public abstract class Indicator : IIndicator, IGivenStroked
{
    /// <summary>The colour of the indicator's main line, or of its band or fill. <see langword="null"/> (the default) leaves the choice to
    /// the indicator or the axes' colour cycle; where the indicator chooses one (a band's <c>#1f77b4</c>, a drawdown's red) it is drawn
    /// under the library's own themes, and the next colour of the cycle under the matplotlib themes.</summary>
    public Color? Color { get; set; }

    /// <summary>The legend text of the indicator's main series. Each indicator sets a default that names its periods, such as <c>ADX(14)</c>.</summary>
    public string? Label { get; set; }

    /// <inheritdoc cref="Models.Series.LineSeries.LineWidth"/>
    public double LineWidth { get => this.ReportedLineWidth; set => Stroke = Stroke with { Width = value }; }

    /// <summary>The dash pattern of the indicator's lines. Solid by default. An indicator given no dash pattern leaves its lines
    /// to the theme's cycler, as a line given none does; one that is given, solid included, is drawn as given.</summary>
    public LineStyle LineStyle { get => Stroke.StyleOrResting; set => Stroke = Stroke with { Style = value }; }

    /// <summary>The width and the dash pattern the indicator's lines were given, each <see langword="null"/> while it was not.</summary>
    internal GivenStroke Stroke { get; private set; }

    GivenStroke IGivenStroked.Stroke => Stroke;

    /// <summary>A shift added to every X position the indicator plots, in X-axis units (bars, for an indicator plotted against bar index). Default 0, which plots each value at its own bar.</summary>
    public double Offset { get; set; }

    /// <inheritdoc />
    public abstract void Apply(Axes axes);

    /// <summary>Applies the <see cref="Offset"/> to an array of X coordinates.</summary>
    protected double[] ApplyOffset(double[] x)
    {
        if (Offset == 0) return x;
        var result = new double[x.Length];
        TensorPrimitives.Add(x, Offset, result);
        return result;
    }

    /// <summary>Generates offset X coordinates for a signal of the given length
    /// starting at the specified warmup offset.</summary>
    protected double[] MakeX(int length, int warmup)
    {
        var x = new double[length];
        for (int i = 0; i < length; i++)
            x[i] = warmup + (double)i;
        return ApplyOffset(x);
    }

    /// <summary>Plots a single signal line with the indicator's styling applied.</summary>
    /// <returns>The created <see cref="LineSeries"/> for further customization.</returns>
    protected LineSeries PlotSignal(Axes axes, double[] values, int warmup,
        string? label = null, Color? color = null) =>
        PlotSignalAt(axes, MakeX(values.Length, warmup), values, label, color);

    /// <summary>Plots a single signal line at the given positions, with the indicator's styling applied. For a signal whose values do
    /// not stand at consecutive bars (a stride leaves some out); <paramref name="x"/> is used as given, without the
    /// <see cref="Offset"/>.</summary>
    /// <param name="axes">The axes the line is added to.</param>
    /// <param name="x">The position of each value.</param>
    /// <param name="values">The values, parallel to <paramref name="x"/>.</param>
    /// <param name="label">The label of the line; the indicator's own when null.</param>
    /// <param name="color">The color of the line; the indicator's own when null.</param>
    /// <returns>The created <see cref="LineSeries"/> for further customization.</returns>
    protected LineSeries PlotSignalAt(Axes axes, double[] x, double[] values,
        string? label = null, Color? color = null)
    {
        var series = axes.Plot(x, values);
        series.Label = label ?? Label;
        if (color.HasValue) series.Color = color.Value;
        else if (Color.HasValue) series.Color = Color.Value;
        StrokeAsGiven(series);
        return series;
    }

    /// <summary>Plots a band indicator: FillBetween for the upper/lower range + a midline with the indicator's styling. The two are one
    /// colour group, so they wear one colour and hold one colour slot: the <see cref="Color"/> the indicator was given; given none,
    /// <see cref="Colors.Tab10Blue"/> under the library's own themes and the next colour of the cycle under the matplotlib themes.
    /// </summary>
    /// <param name="axes">The axes the band is drawn on.</param>
    /// <param name="result">The upper, middle and lower values of the indicator.</param>
    /// <param name="warmup">The number of leading values that are left out because the indicator has no value there yet.</param>
    /// <param name="alpha">The opacity of the fill between the upper and the lower line.</param>
    protected void PlotBands(Axes axes, BandsResult result, int warmup, double alpha = IndicatorLook.BandOpacity)
    {
        var x = MakeX(result.Middle.Length, warmup);
        var fill = axes.AddSeries(AreaSeries.Channel(x, result.Upper, result.Lower, alpha));
        WearColor(fill, Color, IndicatorLook.Library.Lines.First);
        var mid = axes.Plot(x, result.Middle);
        mid.Label = Label;
        WearColor(mid, Color, IndicatorLook.Library.Lines.First);
        axes.InColorGroup(fill, mid);
        StrokeAsGiven(mid);
    }

    /// <summary>Gives <paramref name="line"/> the width and dash pattern the indicator was given, and leaves what it was not given
    /// to the line's slot and theme.</summary>
    /// <param name="line">One of the indicator's lines.</param>
    private protected void StrokeAsGiven(LineSeries line) => line.Stroke = Stroke.Over(line.Stroke);

    /// <summary>The decision every indicator makes about the colour of a part of its picture, stated once: gives the series the colour
    /// its caller gave it, else the colour the indicator chose for it, kept apart as the series' <see cref="ChartSeries.VerbColor"/>: the
    /// library's own themes draw the choice, and the matplotlib themes draw the colour of the series' slot, as matplotlib draws an artist
    /// that names no colour (<see cref="Rendering.SeriesSlot.UnnamedColorOf"/>). The indicator does not know the theme when it draws, so
    /// it never picks between the two itself.</summary>
    /// <param name="series">The part of the indicator's picture.</param>
    /// <param name="given">The colour the caller gave the part; <see langword="null"/> when none.</param>
    /// <param name="chosen">The colour the indicator chose for it.</param>
    internal static void WearColor<TSeries>(TSeries series, Color? given, Color chosen) where TSeries : ChartSeries, IHasColor
    {
        if (given is { } named) series.Color = named;
        else series.VerbColor = chosen;
    }
}

/// <summary>Generic typed indicator that returns a typed result from <see cref="Compute"/>.</summary>
/// <typeparam name="TResult">The type of the computed result. Must implement <see cref="IIndicatorResult"/>.</typeparam>
/// <remarks>Enables composition: use one indicator's <see cref="Compute"/> output as input to another.</remarks>
public abstract class Indicator<TResult> : Indicator where TResult : IIndicatorResult
{
    private readonly IndicatorSpec _spec;

    /// <summary>An indicator written outside the library: it names itself, and places its lines by the warm-up it passes.</summary>
    protected Indicator() : this(IndicatorSpec.None) { }

    /// <summary>An indicator of the library: its label, the labels of its other lines and its first value are what its batch class's
    /// <c>Spec</c> factory said of its parameters, the one place both of its forms read them from.</summary>
    /// <param name="spec">What the <c>Spec</c> factory said of the parameters.</param>
    private protected Indicator(IndicatorSpec spec)
    {
        _spec = spec;
        Label = spec.Label;
    }

    /// <summary>How many bars come before the indicator has a value on every line (<see cref="IndicatorSpec.FirstValueIndex"/>).</summary>
    private protected int FirstValueIndex => _spec.FirstValueIndex;

    /// <summary>How many values the indicator has over its bars: one for each bar from <see cref="FirstValueIndex"/> on, and none when
    /// it has no more bars than that. An indicator writes as many as this says, so with no value it gives nothing: no indicator tests
    /// the count itself.</summary>
    private protected int ValueCount => Math.Max(0, BarsIn - FirstValueIndex);

    /// <summary>How many values an indicator that writes one at every bar, its warm-up included (a moving average seeded from its
    /// first bar), gives: one per bar once it has a value, none while <see cref="ValueCount"/> says it has none.</summary>
    private protected int ValuedBars => ValueCount == 0 ? 0 : BarsIn;

    /// <summary>How many pairs of consecutive bars the indicator is computed over: one fewer than its bars, none when it has no bar.</summary>
    private protected int PairsIn => Math.Max(0, BarsIn - 1);

    /// <summary>The label of one of the indicator's lines, in the order of its outputs (<see cref="IndicatorSpec.Lines"/>).</summary>
    /// <param name="line">The line, from 0 (the indicator's own label).</param>
    /// <returns>Its label.</returns>
    private protected string LineLabel(int line) => _spec.Lines[line];

    /// <summary>A step over the indicator's bars, written to its lines: the step takes every bar in order, and what it says from
    /// <see cref="FirstValueIndex"/> on is written, one entry per bar. The one fold every indicator whose recursion is a step runs; the
    /// bases over prices and bars build the step over their inputs (<c>FoldPrices</c>, <c>FoldHlc</c> and the others).</summary>
    /// <typeparam name="TFold">The step, reading its input of each bar.</typeparam>
    /// <typeparam name="TOut">What the step says at a bar.</typeparam>
    /// <typeparam name="TLines">The lines the outputs are written to.</typeparam>
    /// <param name="fold">The step over the indicator's inputs.</param>
    /// <param name="lines">The lines, made for <see cref="ValueCount"/> entries.</param>
    /// <returns>The lines, written; nothing is written when the indicator has too few bars for a value.</returns>
    private protected TLines FoldBars<TFold, TOut, TLines>(TFold fold, TLines lines)
        where TFold : struct, IBarFold<TOut>
        where TLines : struct, IOutputLines<TOut>
    {
        // BarsIn is virtual: read once, not once a bar.
        int first = FirstValueIndex, bars = BarsIn;
        for (int bar = 0; bar < bars; bar++)
        {
            TOut output = fold.Next(bar);
            if (bar >= first) lines.Set(bar - first, output);
        }

        return lines;
    }

    /// <summary>Pure computation — returns the typed result without side effects on any axes.</summary>
    public abstract TResult Compute();

    /// <summary>How many bars the indicator is computed over. A value worked out at a bar stands at that bar, so the last value of a
    /// line stands at the last bar and a line of <c>n</c> values starts <c>n</c> bars before the end: the base works out where every
    /// value stands, and no indicator says how many bars come before its first value. The bases over prices, bars and rows say it
    /// from their input; an indicator written outside the library places its lines by the warm-up it passes itself.</summary>
    /// <exception cref="NotSupportedException">The indicator does not say how many bars it is computed over.</exception>
    private protected virtual int BarsIn => throw new NotSupportedException($"{GetType().Name} does not say how many bars it is computed over.");

    /// <summary>The positions of <paramref name="count"/> values whose last stands at the last bar, shifted by the
    /// <see cref="Indicator.Offset"/>.</summary>
    /// <param name="count">How many values.</param>
    /// <param name="displacement">How many bars ahead of the bar it is worked out at a value is drawn: an Ichimoku draws its spans
    /// ahead and its lagging line behind. 0 for every other line.</param>
    /// <returns>One position per value.</returns>
    private protected double[] Positions(int count, int displacement = 0) => MakeX(count, BarsIn - count + displacement);

    /// <summary>Plots a line of the indicator's values, each at the bar it is worked out at, in the indicator's style.</summary>
    /// <param name="axes">The axes the line is added to.</param>
    /// <param name="values">The values; the last stands at the last bar.</param>
    /// <param name="label">The label of the line; the indicator's own when null.</param>
    /// <param name="color">The colour of the line; the indicator's own when null.</param>
    /// <returns>The created <see cref="LineSeries"/>.</returns>
    private protected LineSeries PlotLine(Axes axes, double[] values, string? label = null, Color? color = null) =>
        PlotSignalAt(axes, Positions(values.Length), values, label, color);

    /// <summary>Plots a line of the indicator's values, each at the bar it is worked out at, in the colour its caller gave it, else in
    /// the colour the indicator chose for it (<see cref="Models.Series.ChartSeries.VerbColor"/>): drawn under the library's own themes,
    /// and the next colour of the cycle under the matplotlib themes, as matplotlib draws a line that names no colour (a MACD's signal,
    /// a stochastic's %D). The indicator's <see cref="Indicator.Color"/> does not reach it unless the indicator passes it as
    /// <paramref name="given"/> (an Ichimoku's Tenkan line); its width and dash pattern always do.</summary>
    /// <param name="axes">The axes the line is added to.</param>
    /// <param name="values">The values; the last stands at the last bar.</param>
    /// <param name="label">The label of the line.</param>
    /// <param name="given">The colour the caller gave the line; <see langword="null"/> when none.</param>
    /// <param name="chosen">The colour the indicator chose for it.</param>
    /// <param name="displacement">How many bars ahead of the bar it is worked out at a value is drawn; see <see cref="Positions"/>.</param>
    /// <returns>The created <see cref="LineSeries"/>.</returns>
    private protected LineSeries PlotLine(Axes axes, double[] values, string label, Color? given, Color chosen, int displacement = 0)
    {
        var series = axes.Plot(Positions(values.Length, displacement), values);
        series.Label = label;
        WearColor(series, given, chosen);
        StrokeAsGiven(series);
        return series;
    }

    /// <summary>Plots the indicator's band and its midline (<see cref="Indicator.PlotBands"/>), each value at the bar it is worked
    /// out at.</summary>
    /// <param name="axes">The axes the band is drawn on.</param>
    /// <param name="result">The upper, middle and lower values; the last of each stands at the last bar.</param>
    /// <param name="alpha">The opacity of the fill between the upper and the lower line.</param>
    private protected void PlotBand(Axes axes, BandsResult result, double alpha) =>
        PlotBands(axes, result, BarsIn - result.Middle.Length, alpha);
}
