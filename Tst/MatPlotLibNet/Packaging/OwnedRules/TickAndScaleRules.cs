// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Packaging.OwnedRules;

/// <summary>The rules of ticks and scales: the walk of a ladder of ticks, its reach, its decades and its budget, the minus sign of
/// a label, the kind of an axis, where the free ends of an axis stop, and what a scale does, said once by its
/// <c>ScaleRule</c>.</summary>
internal static class TickAndScaleRules
{
/// <summary>The FIND of the step that gave the scale one owner, as it was run: a scale named (log, logit, symmetric log, date) or
    /// compared. The files allowed to spell it: the rule itself, the one map from the enum to its rule, and the verbs that put an axis
    /// on a scale (an assignment, which decides nothing).</summary>
    private static readonly OwnedRule ScaleNamedOrCompared = new(
        "a scale is named or compared only where the scale's rule is made, picked, or set on an axis: every other site reads scale.Rule",
        @"AxisScale\.(Log|Logit|SymLog|Date)|== AxisScale|!= AxisScale",
        "Src/MatPlotLibNet/Rendering/ScaleRule.cs",
        "Src/MatPlotLibNet/Rendering/AxisScaleExtensions.cs",
        "Src/MatPlotLibNet/Builders/AxisVerbs.cs") { Finds = [
        "        _axes.YAxis.Scale = AxisScale.SymLog;"] };

/// <summary>The other spellings of a decision on a scale that the FIND above does not reach: a pattern (<c>is AxisScale.X</c>), a
    /// case, a switch arm (<c>AxisScale.X =&gt;</c>), a list (<c>AxisScale.X or</c>), a scale chosen by a condition
    /// (<c>? AxisScale.X :</c>).</summary>
    private static readonly OwnedRule ScaleSwitched = new(
        "a scale is switched on only where its rule is picked: no member writes a case, an arm, a pattern or a choice per scale",
        @"\bis\s+(?:not\s+)?AxisScale\.|\bcase\s+AxisScale\.|AxisScale\.\w+\s*(?:=>|\bor\b|\bwhen\b)|\?\s*AxisScale\.\w+\s*:",
        "Src/MatPlotLibNet/Rendering/AxisScaleExtensions.cs") { Finds = [
        "        if (axis.Scale is AxisScale.Log or AxisScale.Logit)",
        "            case AxisScale.Log:",
        "            AxisScale.SymLog => SymlogTransform.Forward(value, linthresh),",
        "        var s = logScale ? AxisScale.Log : AxisScale.Linear;"], Ignores = [
        "    internal void SetScale(AxisScale scale) => axis.Scale = scale;",
        "        AxisScale.Linear, AxisScale.Linear, 1.0, 1.0) { }"] };

    /// <summary>The rules of the area, in the order the scan reads them.</summary>
    internal static OwnedRule[] All { get; } =
    [
        new("a tick of a regular ladder is rounded to ten decimals by ArithmeticTicks, the one walk of the ticks of an axis, a cube, a MultipleLocator and the minor values; the offset text rounds the significand it writes, another rule",
            @"Math\.Round\([^;]*,\s*10\)",
            "Src/MatPlotLibNet/Rendering/TickLocators/ArithmeticTicks.cs", "Src/MatPlotLibNet/Rendering/TickLabelExtensions.cs") { Under = "Src/MatPlotLibNet/Rendering/", Finds = [
            "                mt = Math.Round(mt, 10);"] },
        OwnedRule.Nowhere("no walk of values adds its step to the value before (far from zero the sum stalls, and a rounding at each step drifts): ArithmeticTicks works each value out from its index",
            @"\bfor\s*\(\s*(?:double\s[^;]*)?;[^;]*;\s*\w+\s*\+=\s*\w*(?:[sS]tep|_base)\b|^\s*_\w+\s*\+=\s*_step\b", under: "Src/MatPlotLibNet/") with { Finds = [
            "for (double t = first; t <= limit; t += chosenStep) ticksInRange++;",
            "for (double t = first; t <= max + _base * 1e-10; t += _base)",
            "for (; _next <= _limit; _next += _step)",
            "    _next += _step;"], Ignores = [
            "for (int cell = 0; cell < cells; cell += step)",
            "case 'ArrowRight': vb[0] += step; break;",
            "for (long i = 0; ; i++)"] },
        OwnedRule.Nowhere("how far past the end of its range a ladder of ticks reaches is ArithmeticTicks.Reach (a hundredth of a step, or of the range a locator filters its candidates to) or the edge of SteppedTicks: no walk writes its own slack",
            @"\b\w*(?:[sS]tep|_base)\s*\*\s*(?:0\.01|1e-10)\b|\(\s*\w+\s*-\s*\w+\s*\)\s*\*\s*0\.01\b", under: "Src/MatPlotLibNet/Rendering/") with { Finds = [
            "            for (double mt = rangeStart; mt <= rangeEnd + minorStep * 0.01; mt += minorStep)"] },
        new("the multiple of a step at or below and at or above a value is counted by one value, TickEdge (matplotlib's _Edge_integer: the step and the tolerance its offset gives, worked out once), and a price chart's step by PriceStepExtensions with its own tolerance: no site hands a step and its offset or tolerance over apart",
            @"\.StepsAtOr(?:Below|Above)\(",
            "Src/MatPlotLibNet/Rendering/TickLocators/TickEdge.cs", "Src/MatPlotLibNet/Numerics/PriceStepExtensions.cs") { Under = "Src/", Finds = [
            "    internal double AtOrBelow(double x) => x.StepsAtOrBelow(Step, Tolerance);"] },
        new("the power of ten at or below, at or above and nearest a value, and its exponent, are DecadeExtensions (matplotlib's _decade_less_equal and _decade_greater_equal): no site rounds a logarithm to a decade itself",
            @"Math\.(?:Floor|Ceiling|Round)\(\s*(?:Math\.BitDecrement\(\s*)?Math\.Log10\(",
            "Src/MatPlotLibNet/Numerics/DecadeExtensions.cs") { Under = "Src/", Finds = [
            "        double magnitude = Math.Pow(10, Math.Floor(Math.Log10(range / tickSpace)));"] },
        OwnedRule.Nowhere("every count a budget of ticks asks for is walked by TickBudget's enumerator: no layout loops from a fewest to a most or a ladder's bins by hand",
            @"for\s*\(\s*int\s+\w+\s*=\s*[^;]*;\s*\w+\s*<=\s*[^;]*\b(?:Most|Bins)\s*;", under: "Src/MatPlotLibNet/") with { Finds = [
            "        for (int n = budget.Fewest; n <= budget.Most; n++)"] },
        OwnedRule.Nowhere("the multiple of a step at or below and at or above a value is TickEdge.AtOrBelow and AtOrAbove, matplotlib's _Edge_integer with its one tolerance: no locator floors or ceils a value over its step",
            @"Math\.(?:Ceiling|Floor)\([^;]*/\s*\(?\s*(?:\w*[sS]tep|_base)\b", under: "Src/MatPlotLibNet/") with { Finds = [
            "        internal int FloorStep(double origin, double step) => (int)Math.Floor((price - origin) / step + Tolerance);"] },
        OwnedRule.Nowhere("how many ticks the room along an axis asks for is TickBudget.Clamp, its band a value of TickBudget (Standard, Logit, a ladder's Budget): no locator clamps a tick space, keeps a fewest or most of its own, or answers a room that is no number by itself",
            @"\bFewestTicks\w*|\bMostTicksAsked\b|MaxNLocator\.(?:Fewest|Most)Ticks\b|\bMaxIntervals\b|Math\.(?:Clamp|Max|Min)\([^;]*\b[tT]ickSpace\b|double\.Is(?:NaN|Finite)\(\s*tickSpace\s*\)",
            under: "Src/MatPlotLibNet/") with { Finds = [
            "            double[] readAt = times ?? [.. new Rendering.TickLocators.MaxNLocator(Rendering.TickLocators.MaxNLocator.MostTicks).Locate(0, followed.Max(d => d.Max()))];"] },
        OwnedRule.Nowhere("how a tick label, an offset text or a colour bar's label signs a negative number is asked of the theme's TickLabelLook (Signed, Labelled): no renderer reads its true-minus flag or threads it as a bool",
            @"\bUnicodeMinus\b|\bWithTrueMinus\b|\btrueMinus\b", under: "Src/MatPlotLibNet/Rendering/") with { Finds = [
            "        string text = label.WithTrueMinus(look.UnicodeMinus);"] },
        new("the true minus sign is TickLabelLook.TrueMinusSign; the tables keyed by character (glyph widths, the STIX glyphs) name every character they hold",
            @"'\\u2212'|'\u2212'",
            "Src/MatPlotLibNet/Styling/Looks/TickLabelLook.cs", "Src/MatPlotLibNet/Rendering/TextMeasurement/DefaultFontMetrics.cs",
            "Src/MatPlotLibNet/Rendering/MathText/StixFallback.Data.cs") { Under = "Src/MatPlotLibNet/", Finds = [
            "    internal const char TrueMinusSign = '\\u2212';"] },
        new("how many label sizes a tick takes along an axis is MaxNLocator.LabelSpanAlong(alongX): a caller says its direction, never picks a span",
            @"\b(?:Horizontal|Vertical)LabelSpan\b",
            "Src/MatPlotLibNet/Rendering/TickLocators/MaxNLocator.cs") { Under = "Src/MatPlotLibNet/", Finds = [
            "        double[] rings = [.. MaxNLocator.ForLength(maxRadiusPx, tickFontPx, MaxNLocator.VerticalLabelSpan)"] },
        OwnedRule.Nowhere("which minor-tick facts a theme asks for is asked of its AxisTickLook (MinorNamer, PlacesMinors, CarriesMinorGrid, GridStandsOnMinors, MinorGridStroke): no renderer reads its minor flags",
            @"\b(?:MinorsByLocator|MinorGridLikeMajor)\b", under: "Src/MatPlotLibNet/Rendering/") with { Finds = [
            "        if (look.MinorGridLikeMajor) DrawMinorGrid(stroke);"] },
        new("where the free ends of an axis stop (the theme's axes.autolimit_mode, or the library's nice bounds) is picked once from the look as an AutolimitRule leaf: the 2-D range, the 3-D cube, the radius of a polar axes and the tick rule ask the leaf; only the rule and the matplotlib theme factory name a mode",
            @"\bAutolimitMode\.(?:Data|RoundNumbers)\b|\bRange\.Autolimit\b",
            "Src/MatPlotLibNet/Rendering/AutolimitRule.cs",
            "Src/MatPlotLibNet/Styling/Themes/MatplotlibThemeFactory.cs") { Under = "Src/MatPlotLibNet/", Finds = [
            "        AutolimitMode.Data => Data,"] },
        OwnedRule.Nowhere("a scale's rule holds the threshold it maps by (ScaleRule.SymLogAt, AxisScale.RuleAt, an axis' ScaleRule): no site hands a rule a threshold beside it, and no reader carries the two as a pair",
            @"(?<!SymlogTransform)\.(?:Forward|ForwardAll|Inverse|ForwardRange|Locator)\([^;]*\b\w*[lL]in[tT]hresh\w*\b|\bScaleRule\s+\w+\s*,\s*double\s+\w*[lL]in[tT]hresh\b|\bdouble\s+LinThresh\s*\{",
            under: "Src/MatPlotLibNet/") with { Finds = [
            "        new(scale.Inverse(scale.Forward(span.Min, linThresh) + delta, linThresh),"] },
        new("the symlog threshold an axis is drawn with (the one given, else its theme's, else 1) is said by the range look (SymLogLinThreshOver) and read in one place, SymLogLinThreshUnder: only the verb that gives it, the wire form and the interaction's cache key read the axis' property",
            @"\.SymLogLinThresh\b",
            "Src/MatPlotLibNet/Builders/AxisVerbs.cs",
            "Src/MatPlotLibNet/Serialization/AxisDto.cs", "Src/MatPlotLibNet/Interaction/ChartLayout.cs") { Under = "Src/", Finds = [
            "        _axes.YAxis.SymLogLinThresh = linthresh;"] },
        OwnedRule.Nowhere("what a kind of axis does where neither the axis nor the theme places its ticks (the library's placement, the lists at any length, how much of a ladder's run it keeps) is a member of its TickAxisKind leaf: no site compares or switches on a kind",
            @"\bTickAxisKind\.\w+\s*=>|(?:==|!=|\bis(?:\s+not)?)\s*TickAxisKind\.|\bcase\s+TickAxisKind\.", under: "Src/MatPlotLibNet/") with { Finds = [
            "        if (kind == TickAxisKind.Category) return labels;"] },
        new("the steps of matplotlib's default AutoLocator (1, 2, 2.5, 5, 10) are written once, in TickLadder.DefaultStyle: the library's cube ticks read them there",
            @"\[\s*1(?:\.0)?,\s*2(?:\.0)?,\s*2\.5,\s*5(?:\.0)?,\s*10(?:\.0)?\s*\]",
            "Src/MatPlotLibNet/Rendering/TickLocators/SteppedTicks.cs") { Under = "Src/MatPlotLibNet/", Finds = [
            "        double[] stepLadder = [1.0, 2.0, 2.5, 5.0, 10.0];"] },
        OwnedRule.Nowhere("a run of ticks carries the edge it lies on (TickRun.Edge), and a side of a box is told by its EdgeTerms: no caller tells a run, a placement, a request or a side its direction by a literal",
            @"\balongX:\s*(?:true|false)\b", under: "Src/") with { Finds = [
            "        var run = TickRun.Along(placement, alongX: true);"] },
        new("a run's placement is asked along the run's own edge, in TickRun (PlaceOnPlot, HoldsAny, Scale, View, ScaleMinors): no site asks the placement of a run with a direction of its own",
            @"\bPlacement\.(?:OnPlot|ScaleAlong|ViewAlong)\(",
            "Src/MatPlotLibNet/Rendering/TickRun.cs") { Under = "Src/", Finds = [
            "    internal double? PlaceOnPlot(double value) => Placement.OnPlot(value, Edge.AlongX);"] },
        new("what a drawing of ticks puts on the page is asked of its TickInk (DrawsLabels, DrawsMarks, WithoutLabels): no site decodes the flags",
            @"&\s*~?\s*(?:Rendering\.)?TickInk\.",
            "Src/MatPlotLibNet/Rendering/TickInkExtensions.cs") { Under = "Src/", Finds = [
            "        internal bool DrawsLabels => (ink & TickInk.Labels) != 0;"] },
        new("how far a tick mark reaches out of the plot and into it from its edge, for each direction, is TickMarkSpan.Of: no site switches on or compares a TickDirection",
            @"\bTickDirection\.\w+\s*=>|\bcase\s+TickDirection\.|(?:==|!=|\bis(?:\s+not)?)\s*TickDirection\.",
            "Src/MatPlotLibNet/Rendering/TickMarkSpan.cs") { Under = "Src/", Finds = [
            "            double outer = direction == TickDirection.In ? spineHalfWidth : length;"] },
        OwnedRule.Nowhere("a switch over the interval the date locator set names no arm for Seconds, the finest: its `_` is Seconds, so no arm answers for a value that no member names (the locator is the only writer of ChosenInterval)",
            @"\bDateInterval\.Seconds\s*(?:=>|\bor\b|\bwhen\b)|\bcase\s+DateInterval\.Seconds\b", under: "Src/MatPlotLibNet/") with { Finds = [
            "            DateInterval.Seconds => LocateSeconds(dtMin, dtMax),",
            "        DateInterval.Seconds => \"HH:mm:ss\","], Ignores = [
            "            _                      => DateInterval.Seconds"] },
        ScaleNamedOrCompared,
        ScaleSwitched,
        OwnedRule.Nowhere("a rule is never compared with one of the rules to decide by it: the difference is a member of the rule",
            @"[!=]=\s*ScaleRule\.\w+|\bScaleRule\.\w+\s*[!=]=|\bis\s+(?:not\s+)?ScaleRule\.\w+", under: "Src/") with { Finds = [
            "        if (axis.Scale.Rule == ScaleRule.Log) return LogTicks();"] },
        OwnedRule.Nowhere("Range1D is a value: it reads no axis; what an axis does with its range is AxisAutoscale's, which its public members forward to",
            @"\baxis\.\w", under: "Src/MatPlotLibNet/Rendering/ValueObjects/Range1D.cs") with { Finds = [
            "    internal Range1D OrientedBy(Axis axis) => axis.Inverted && Lo < Hi ? new(Hi, Lo) : this;"] },
    ];
}
