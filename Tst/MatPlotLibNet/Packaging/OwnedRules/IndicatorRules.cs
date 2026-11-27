// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Packaging.OwnedRules;

/// <summary>The rules of an indicator: its band, the colour of a part it gives none, the limits of its pane and where its values
/// stand.</summary>
internal static class IndicatorRules
{
/// <summary>An indicator fills its band through the area's factories, never through the public fill verb and a width set after.</summary>
    private static readonly OwnedRule IndicatorBand = OwnedRule.Nowhere(
        "an indicator's band is AreaSeries.Band or AreaSeries.Channel: no indicator fills through FillBetween and sets the outline after",
        @"\.FillBetween\(", under: "Src/MatPlotLibNet/Indicators/") with { Finds = [
        "        var series = axes.FillBetween(x, negDd);"] };

    /// <summary>The label an indicator is named by when one number says it (<c>RSI(14)</c>, <c>Aroon(25)</c>) is made by
    /// <c>IndicatorSpec</c>, in the one spelling <c>NAME(number)</c>: the batch classes name the indicator and the number, through
    /// <c>OfWindow</c> (and <c>OfCheckedWindow</c>, which refuses a number below its least first), or <c>Labelled</c> where the label
    /// is also the name of a line. A label of two numbers or of a decimal is its own spelling and not read.</summary>
    private static readonly OwnedRule WindowLabel = new(
        "an indicator named by one number (NAME(period)) is labelled by IndicatorSpec (OfWindow, OfCheckedWindow, Labelled): no indicator interpolates the name and the number itself",
        @"\bInvariant\(\$""[^""]*\(\{\w+\}\)""\)",
        "Src/MatPlotLibNet/Indicators/IndicatorSpec.cs") { Under = "Src/MatPlotLibNet/Indicators/", Finds = [
        "    internal static IndicatorSpec Spec(int period) => IndicatorSpec.Of(period, string.Invariant($\"ATR({period})\"));",
        "        string label = string.Invariant($\"ADX({period})\");",
        "        return IndicatorSpec.Of(period - 1, string.Invariant($\"CG({period})\"));"], Ignores = [
        "        string label = string.Invariant($\"BB({period},{stdDevMultiplier.InvariantText()})\");",
        "        return IndicatorSpec.Of(3, $\"CC({alpha.InvariantText(\"0.00\")})\");",
        "        return IndicatorSpec.OfWindow(\"ATR\", period, period);"] };

    /// <summary>The rules of the area, in the order the scan reads them.</summary>
    internal static OwnedRule[] All { get; } =
    [
        IndicatorBand,
        WindowLabel,
        new("the Fibonacci ratios, and the price a ratio of a move stands at, are the retracement tool's (Ratios, PriceAt): the indicator's Levels are the tool's inner ratios and no other file types a ratio",
            @"\b0\.(?:236|382|618|786)\b|\b\w*[hH]igh\w*\s*-\s*\w*\s*\*\s*\w+\b",
            "Src/MatPlotLibNet/Models/Tools/FibonacciRetracement.cs") { Under = "Src/MatPlotLibNet/", Finds = [
            "    public static readonly double[] Levels = [0.236, 0.382, 0.5, 0.618, 0.786];",
            "            result[i] = _high - range * Levels[i];"], Ignores = [
            "    public static readonly double[] Levels = Models.Tools.FibonacciRetracement.Ratios[1..^1];"] },
        new("the colour an indicator chooses for a part its caller gave none is read from IndicatorLook, one path: its Lines (the standard cycle's first two colours, CyclePair), its Moves (a rise and a fall, UpDownColors) and its Ichimoku hexes; an indicator names no colour of its own and reaches for neither pair itself",
            @"\bColors\.(?:Tab10\w+|Green|Red)\b|\bFromHex\(|\bUpDownColors\.Standard\b|\bCyclePair\.Standard\b",
            "Src/MatPlotLibNet/Indicators/IchimokuColors.cs", "Src/MatPlotLibNet/Indicators/IndicatorLook.cs") { Under = "Src/MatPlotLibNet/Indicators/", Finds = [
            "            PlotSignal(axes, plusDi, offset, \"+DI\", PlusDiColor ?? Colors.Green).LineWidth = 1;"] },
        new("a value worked out for each bar from that bar and the one before it, rolled into a mean over a window, is walked by CandleIndicator.RollingMeanOfPairs: no indicator fills an array of bar pairs and rolls it itself",
            @"new double\[\s*(?:\w+\s*-\s*1|PairsIn)\s*\](?:(?!\n    \})[\s\S])*?\.RollingMean\(|for \(int (\w+) = 1; \1 < \w+; \1\+\+\)(?:(?!\n    \})[\s\S])*?\.RollingMean\(|(?:\.AsSpan\(1\)|\[1\.\.\]|\.Skip\(1\)(?:\.ToArray\(\))?)\.RollingMean\(",
            "Src/MatPlotLibNet/Indicators/CandleIndicator.cs") { Under = "Src/MatPlotLibNet/Indicators/", AcrossLines = true, Finds = [
            "    public override SignalResult Compute()\n    {\n        var illiq = new double[n - 1];\n        for (int i = 0; i < n - 1; i++)\n            illiq[i] = Math.Abs(Close[i + 1] - Close[i]);\n\n        return illiq.RollingMean(_period);\n    }",
            "        var emv1 = new double[n];\n        for (int i = 1; i < n; i++)\n            emv1[i] = High[i] - High[i - 1];\n\n        return emv1.RollingMean(_period);\n    }",
            "        return perBar.AsSpan(1).RollingMean(_period);",
            "        return perBar[1..].RollingMean(_period);",
            "        var moves = new double[PairsIn];\n        for (int i = 0; i < moves.Length; i++)\n            moves[i] = Close[i + 1] - Close[i];\n\n        return moves.RollingMean(_period);\n    }"], Ignores = [
            "        var sigma2 = new double[n];\n        for (int i = 0; i < n; i++)\n            sigma2[i] = High[i] - Low[i];\n\n        double[] result = sigma2.RollingMean(_period);\n    }",
            "        var absDiff = new double[n - 1];\n        for (int i = 0; i < n - 1; i++)\n            absDiff[i] = Math.Abs(Prices[i + 1] - Prices[i]);\n    }\n\n    private double[] Smooth()\n    {\n        return Prices.RollingMean(_period);\n    }",
            "        return RollingMeanOfPairs(_period, t => Math.Abs(Close[t] - Close[t - 1]));"] },
        new("how many values an indicator has is Indicator<TResult>'s count (ValueCount, from the first value its spec names; ValuedBars for one that writes a value at every bar), and with no value it gives nothing because it writes that many: no indicator tests the count, or its bars, against its first value or against nothing and returns early itself, in any spelling. Three thresholds are of another rule, each of a line that starts before the indicator's first value: the window of CandleIndicator.ComputeDonchianMid, a stochastic's raw %K window and a relative vigour index's RVI line",
            @"\bValueCount\s*(?:==|!=|<=|>=|<|>|is(?:\s+not)?)\s*[01]\b|\b[01]\s*(?:==|!=|<=|>=|<|>)\s*ValueCount\b|\b(?:int|var)\s+(\w+)\s*=\s*ValueCount\s*;(?:(?!\n    \})[\s\S])*?(?:\b\1\s*(?:==|!=|<=|<|is(?:\s+not)?)\s*0\b|\b0\s*(?:==|!=|>=|<)\s*\1\b)|\bif\s*\(\s*(?:n|BarCount|BarsIn|Prices\.Length|len|outLen)\s*(?:<=|<)(?>\s*)(?!(?:period|_kPeriod|_period\s*\+\s*4)\s*\))[^)]+\)|\b(?:BarCount|BarsIn|(?:Prices|Open|High|Low|Close|Volume)\.Length)\s*(?:==|<=|<)\s*[01]\b|\b(?:Prices|Open|High|Low|Close|Volume)\s+is\s+\[\]",
            "Src/MatPlotLibNet/Indicators/Indicator.cs") { Under = "Src/MatPlotLibNet/Indicators/", AcrossLines = true, Finds = [
            "        if (ValueCount == 0) return Array.Empty<double>();",
            "        if (ValueCount == 0)\n            return new SqueezeResult(Array.Empty<bool>(), Array.Empty<double>());",
            "        if (ValueCount is 0) return [];",
            "        if (ValueCount <= 0) return [];",
            "        if (ValueCount < 1) return [];",
            "        if (0 == ValueCount) return [];",
            "        if (ValueCount > 0) result = Work();",
            "        return ValueCount == 0 ? [] : Work();",
            "        int len = ValueCount;\n        if (len == 0) return Array.Empty<double>();",
            "        var outLen = ValueCount;\n        if (outLen <= 0) return [];",
            "        int len = ValueCount;\n        if (0 == len) return [];",
            "        if (n < _period) return Array.Empty<double>();",
            "        if (BarCount <= _period) return [];",
            "        if (Prices.Length < 2) return [];",
            "        if (Prices.Length == 0) return [];",
            "        if (Close is []) return [];"], Ignores = [
            "        int outLen = ValueCount;\n        var result = new double[outLen];\n        for (int w = 0; w < outLen; w++) result[w] = w;\n    }",
            "        FoldBars<HlcFold<TStep, TOut>, TOut, TLines>(new(this, step), new ValueLine(ValueCount));",
            "        if (n < period) return [];",
            "        if (n < _kPeriod) return new StochasticResult([], []);",
            "        if (n < _period + 4)",
            "        if (macdLen <= 0) return new MacdResult([], [], []);",
            "            if (n == 0) return Array.Empty<double>();",
            "        int n = ValuedBars;"] },
        new("the pairs of consecutive bars an indicator walks are counted by Indicator<TResult>.PairsIn, none when it has no bar: no indicator sizes its pairs as its bars less one itself",
            @"\bnew\s+double\[\s*(?:\w+\.)?\w+\s*-\s*1\s*\]|\bint\s+\w*Len\s*=\s*(?:\w+\.)?\w+\s*-\s*1\s*;|\bMath\.Max\(\s*0\s*,\s*(?:BarsIn|BarCount|n|\w+\.Length)\s*-\s*1\s*\)",
            "Src/MatPlotLibNet/Indicators/Indicator.cs") { Under = "Src/MatPlotLibNet/Indicators/", Finds = [
            "        var values = new double[BarCount - 1];",
            "        var absDiff = new double[n - 1];",
            "        int rLen = n - 1;",
            "        var dp = new double[Math.Max(0, Prices.Length - 1)];"], Ignores = [
            "        var absDiff = new double[PairsIn];",
            "        for (int i = end - 1; i >= start; i--)",
            "        return IndicatorSpec.Of(kPeriod + Math.Max(0, smoothK - 1) + dPeriod - 2, label);"] },
        new("the true range of a bar (its own range or its distance from the close before it, whichever is larger) is PriceSources.TrueRange: no indicator measures a bar's distance from the close before it itself",
            @"\bMath\.Abs\(\s*\w*(?:[hH]igh|[lL]ow)\w*(?:\[[^\]]*\])?\s*-\s*\w*[cC]lose\w*(?:\[[^\]]*\])?\s*\)",
            "Src/MatPlotLibNet/Indicators/PriceSource.cs") { Under = "Src/", Finds = [
            "            double hc = Math.Abs(High[t] - Close[t - 1]);",
            "            double lc = Math.Abs(low - prevClose);"], Ignores = [
            "            tr[t] = PriceSources.TrueRange(High[t], Low[t], Close[t - 1]);",
            "            double change = Math.Abs(Prices[t] - Prices[t - _period]);"] },
        new("an indicator pins its pane's Y axis with the PaneRange its values live in, which PaneRange.Pin sets: no indicator assigns a limit of the axis itself",
            @"\bYAxis\.(?:Min|Max)\s*=[^=]",
            "Src/MatPlotLibNet/Indicators/PaneRange.cs") { Under = "Src/MatPlotLibNet/Indicators/", Finds = [
            "        axes.YAxis.Min = 0;"] },
        new("an indicator's values stand at their own bars, which Indicator<TResult> works out from the bars it was given (BarsIn) and the values it draws: no indicator passes a warm-up to MakeX, PlotSignal or PlotBands, or adds an index to a lead of its own",
            @"\b(?:MakeX|PlotSignal|PlotBands)\(|=\s*\w*(?:[oO]ffset|[wW]armup|[fF]irst|[lL]ead)\w*\s*\+\s*(?:\(double\)\s*)?i\s*;",
            "Src/MatPlotLibNet/Indicators/Indicator.cs") { Under = "Src/MatPlotLibNet/Indicators/", Finds = [
            "        PlotSignal(axes, Compute(), warmup: 6);"] },
    ];
}
