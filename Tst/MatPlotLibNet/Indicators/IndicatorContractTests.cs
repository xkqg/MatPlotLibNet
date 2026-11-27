// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Indicators;
using MatPlotLibNet.Indicators.Streaming;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Tests.Indicators.Streaming;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>What every indicator of the package keeps, held once over <see cref="IndicatorCases"/>, one case an indicator: what it
/// draws and under which labels, where it draws it (a value worked out at a bar stands at that bar, the last at the last bar given, an
/// offset moves everything by itself), the range it pins its pane to (with or without a value yet) and draws inside, the bar of its first
/// value and the fewest bars it needs, what it says over bars that never move, the name it takes from its defaults, and the arguments it
/// refuses by name, its streaming form refusing the same ones by the same names. A case lists every rule it breaks.</summary>
public class IndicatorContractTests
{
    public static TheoryData<string> Names() => IndicatorCases.Names();

    public static TheoryData<string> Refusing() => new(IndicatorCases.All.Where(c => c.Refuses.Length > 0).Select(c => c.Name));

    public static TheoryData<string> StreamedNames() => new(StreamingRows.All.Select(r => r.Name));

    /// <summary>The streaming forms that draw something other than what their batch indicator pins its pane for: a streaming drawdown
    /// draws the drawdown itself (the batch draws it below zero), a streaming profit and loss cannot know its lowest bar ahead.</summary>
    private static readonly IReadOnlySet<string> StreamedUnpinned = new HashSet<string>(StringComparer.Ordinal) { "DrawDown", "ProfitLoss" };

    /// <summary>Every calculator of the package is one case, and the streaming map classifies the same calculators: one that is added
    /// is held by the contract, and by the map, from the day it is added.</summary>
    [Fact]
    public void EveryCalculatorOfThePackage_IsOneCase_AndOneLineOfTheStreamingMap()
    {
        var calculated = IndicatorCountContractTests.Calculators().Select(t => t.Name).Order(StringComparer.Ordinal).ToList();
        var cases = IndicatorCases.All.Select(c => c.Name).Order(StringComparer.Ordinal).ToList();
        var mapped = StreamingMap.All.Select(c => c.Name).Order(StringComparer.Ordinal).ToList();

        Assert.Equal(calculated, cases);
        Assert.Equal(calculated, mapped);
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void WhatAnIndicatorDraws_AndWhereItDrawsIt(string name)
    {
        var indicatorCase = IndicatorCases.Of(name);
        var broken = new List<string>();

        var axes = Applied(indicatorCase.Make(IndicatorCases.Bars), 0);
        var drawn = Drawn(axes);
        if (!drawn.SequenceEqual(indicatorCase.Draws))
            broken.Add($"it draws [{string.Join(", ", drawn)}], the case says [{string.Join(", ", indicatorCase.Draws)}]");
        if (Pinned(axes) != indicatorCase.Pane) broken.Add($"it pins its pane to {Pinned(axes)}, the case says {indicatorCase.Pane}");

        if (indicatorCase.AtBars) broken.AddRange(Misplaced(axes, indicatorCase.Shift ?? (_ => 0)));
        var outside = Heights(axes).Where(v => v < (indicatorCase.Pane.Min ?? double.NegativeInfinity) || v > (indicatorCase.Pane.Max ?? double.PositiveInfinity)).ToList();
        if (outside.Count > 0 && !indicatorCase.LeavesItsPane) broken.Add($"it draws {outside[0]}, outside its pane {indicatorCase.Pane}");

        var moved = Positions(Applied(indicatorCase.Make(IndicatorCases.Bars), 0.5)).ToList();
        var unmoved = Positions(axes).ToList();
        if (moved.Count != unmoved.Count || moved.Zip(unmoved).Any(p => !p.First.SequenceEqual(p.Second.Select(x => x + 0.5))))
            broken.Add("an offset of 0.5 does not move everything it draws by 0.5");

        if (indicatorCase.Needs is int needs)
        {
            var early = Applied(indicatorCase.Make(OracleBars.Walk(needs - 1)), 0);
            if (Positions(early).Any(x => x.Length > 0)) broken.Add($"over {needs - 1} bars, before its first value, it draws points");
            if (Pinned(early) != indicatorCase.Pane) broken.Add($"over {needs - 1} bars it pins its pane to {Pinned(early)}, not {indicatorCase.Pane}");
        }

        Assert.True(broken.Count == 0, $"{name}: {string.Join("; ", broken)}");
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void WhatAnIndicatorComputes_AndWhatItIsCalledByDefault(string name)
    {
        var indicatorCase = IndicatorCases.Of(name);
        var broken = new List<string>();

        if (indicatorCase.Needs is int needs)
        {
            if (Lines(indicatorCase.Make(OracleBars.Walk(0))).Any(line => line.Length > 0)) broken.Add("over no bars it computes values");
            if (Lines(indicatorCase.Make(OracleBars.Walk(needs - 1)))[0].Length != 0) broken.Add($"over {needs - 1} bars it already has a value");
            if (Lines(indicatorCase.Make(OracleBars.Walk(needs)))[0].Length == 0) broken.Add($"over {needs} bars it has no value yet");
        }

        var lines = Lines(indicatorCase.Make(IndicatorCases.Bars));
        if (indicatorCase.FirstValue is int first && lines[0].Length != IndicatorCases.Bars.Count - first)
            broken.Add($"over {IndicatorCases.Bars.Count} bars its first value is at bar {IndicatorCases.Bars.Count - lines[0].Length}, not {first}");

        if (indicatorCase.OnFlatBars is double flat)
        {
            double[] price = [.. Enumerable.Repeat(100.0, IndicatorCases.Bars.Count)];
            double[] volume = [.. Enumerable.Repeat(1000.0, IndicatorCases.Bars.Count)];
            var said = Lines(indicatorCase.Make(new OracleBars(price, price, price, price, volume))).OfType<double[]>().SelectMany(Numbers).ToList();
            if (said.Count == 0 || said.Any(v => !(Math.Abs(v - flat) <= 1e-9 * Math.Max(1, Math.Abs(flat)))))
                broken.Add($"over bars that never move it says [{string.Join(", ", said.Distinct().Take(3))}], not {flat}");
        }

        string? byDefault = IndicatorCases.MadeByDefault(name)?.Label;
        if (byDefault != indicatorCase.ByDefault) broken.Add($"made with its defaults it is called '{byDefault}', the case says '{indicatorCase.ByDefault}'");

        Assert.True(broken.Count == 0, $"{name}: {string.Join("; ", broken)}");
    }

    [Theory]
    [MemberData(nameof(Refusing))]
    public void AnArgumentAnIndicatorDoesNotTake_IsRefusedByName_InBothForms(string name)
    {
        var broken = new List<string>();

        foreach (var refusal in IndicatorCases.Of(name).Refuses)
        {
            broken.AddRange(Refused("the indicator", refusal.What, refusal.Batch, refusal.Argument));
            if (refusal.Streaming is { } streaming) broken.AddRange(Refused("the streaming form", refusal.What, streaming, refusal.FormArgument));
        }

        Assert.True(broken.Count == 0, $"{name}: {string.Join("; ", broken)}");
    }

    /// <summary>A streaming form attached to a pane pins it as its batch indicator pins its own: a streaming RSI's pane runs from 0 to
    /// 100 as a drawn RSI's does.</summary>
    [Theory]
    [MemberData(nameof(StreamedNames))]
    public void AStreamingForm_PinsThePaneItIsAttachedTo_AsItsBatchIndicatorDoes(string name)
    {
        var candles = new StreamingCandlestickSeries(10);
        var axes = new Axes();

        candles.WithStreaming(axes, StreamingRows.Of(name).Make());

        var pinned = StreamedUnpinned.Contains(name) ? default : IndicatorCases.Of(name).Pane;
        Assert.Equal(pinned, Pinned(axes));
    }

    /// <summary>An indicator written outside the library places its line by the warm-up it passes the base's own tools, shifted by
    /// its offset as the library's are.</summary>
    [Fact]
    public void AnIndicatorWrittenOutsideTheLibrary_PlacesItsLineByTheWarmUpItPasses()
    {
        var axes = new Axes();

        new OwnWarmUp { Offset = 0.5 }.Apply(axes);

        Assert.Equal([2.5, 3.5, 4.5], ((LineSeries)axes.Series[0]).XData);
    }

    /// <summary>An indicator that does not say how many bars it is computed over cannot be placed by the base, and says so.</summary>
    [Fact]
    public void AnIndicatorThatDoesNotSayItsBars_IsNotPlacedByTheBase()
    {
        var thrown = Assert.Throws<NotSupportedException>(() => new NoBars().Apply(new Axes()));

        Assert.Contains(nameof(NoBars), thrown.Message, StringComparison.Ordinal);
    }

    private static Axes Applied(Indicator indicator, double offset)
    {
        var axes = new Axes();
        indicator.Offset = offset;
        indicator.Apply(axes);
        return axes;
    }

    /// <summary>What the axes hold, as a case states it: the kind and the label of each series, then each reference line at its value.</summary>
    private static List<string> Drawn(Axes axes) =>
    [
        .. axes.AllSeries.Select(s => s.GetType().Name[..^"Series".Length] + (s.Label is null ? "" : " " + s.Label)),
        .. axes.ReferenceLines.Select(r => "Reference " + r.Value.ToString("0.###", CultureInfo.InvariantCulture)),
    ];

    private static PaneRange Pinned(Axes axes) => new(axes.YAxis.Min, axes.YAxis.Max);

    /// <summary>The positions each series stands at along the bars: the X of a line, a band or a scatter, the numeric positions of
    /// upright bars (a bar laid along X, as a volume profile's, stands at a price, not at a bar).</summary>
    private static IEnumerable<double[]> Positions(Axes axes) => axes.AllSeries.Select(PositionsOf);

    /// <summary>The heights of everything drawn: the Y of a line, a band or a scatter, the values of bars, NaN left out.</summary>
    private static IEnumerable<double> Heights(Axes axes) => axes.AllSeries.SelectMany(s => s switch
    {
        XYSeries xy => xy.YData,
        BarSeries bar => bar.Values,
        _ => [],
    }).Where(v => !double.IsNaN(v));

    private static double[] PositionsOf(ISeries series) => series switch
    {
        XYSeries xy => xy.XData,
        BarSeries { Orientation: BarOrientation.Vertical } bar => bar.XCoordinate ?? [],
        _ => [],
    };

    /// <summary>The last value of every line, band and bar stands at the last bar, every value at the bar before the next; the dots of a
    /// stop and reverse stand at the bars of the side the trade is on, so between them they reach the last bar.</summary>
    private static IEnumerable<string> Misplaced(Axes axes, Func<string?, int> shift)
    {
        int last = IndicatorCases.Bars.Count - 1;
        var lines = axes.AllSeries.Where(s => s is not ScatterSeries).Select(s => (s.Label, X: PositionsOf(s))).Where(s => s.X.Length > 0).ToList();
        var dots = axes.AllSeries.OfType<ScatterSeries>().SelectMany(s => s.XData).ToList();
        if (lines.Count == 0 && dots.Count == 0) yield return "it draws nothing at a bar";
        foreach (var (label, x) in lines)
        {
            if (x[^1] != last + shift(label)) yield return $"{label ?? "unlabelled"}: the last value stands at {x[^1]}, the last bar is {last + shift(label)}";
            if (x.Zip(x.Skip(1)).Any(pair => pair.Second != pair.First + 1)) yield return $"{label ?? "unlabelled"}: a value does not stand at the bar after the one before";
        }

        if (dots.Count > 0 && dots.Max() != last) yield return $"the last dot stands at {dots.Max()}, the last bar is {last}";
    }

    /// <summary>The values of a line that are numbers: what a line of flags says as 0 and 1, NaN left out.</summary>
    private static IEnumerable<double> Numbers(Array line) =>
        line.Cast<object>().Select(v => Convert.ToDouble(v, CultureInfo.InvariantCulture)).Where(v => !double.IsNaN(v));

    /// <summary>Every line of what the indicator computes, the main line first: the arrays of its result, in the order it declares them.</summary>
    private static IReadOnlyList<Array> Lines(Indicator indicator)
    {
        var result = indicator.GetType().GetMethod("Compute", Type.EmptyTypes)!.Invoke(indicator, null)!;
        return [.. result.GetType().GetProperties().Where(p => p.PropertyType.IsArray).Select(p => (Array)p.GetValue(result)!)];
    }

    private static IEnumerable<string> Refused(string form, string what, Func<object> make, string? argument)
    {
        ArgumentException? refused = null;
        try { make(); }
        catch (ArgumentException e) { refused = e; }

        if (refused is null) yield return $"{form} takes {what}";
        else if (refused.ParamName != argument) yield return $"{form} refuses {what} by '{refused.ParamName}', not '{argument}'";
    }

    /// <summary>An indicator of a caller's own: three values after a warm-up of two bars, placed by the protected tools.</summary>
    private sealed class OwnWarmUp : Indicator<SignalResult>
    {
        public override SignalResult Compute() => new double[] { 1, 2, 3 };

        public override void Apply(Axes axes) => PlotSignal(axes, Compute(), 2);
    }

    /// <summary>An indicator that places its line through the base without saying how many bars it was given.</summary>
    private sealed class NoBars : Indicator<SignalResult>
    {
        public override SignalResult Compute() => new double[] { 1, 2, 3 };

        public override void Apply(Axes axes) => PlotLine(axes, Compute());
    }
}
