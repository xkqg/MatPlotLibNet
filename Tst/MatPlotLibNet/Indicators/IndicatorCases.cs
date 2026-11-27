// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Data;
using MatPlotLibNet.Indicators;
using MatPlotLibNet.Indicators.Streaming;
using MatPlotLibNet.Tests.Indicators.Streaming;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>One argument an indicator does not take: the indicator made with it, its streaming form made with it (null when the form
/// has no such argument), and the parameter the refusal names.</summary>
/// <param name="What">What the argument is, as a failure names it.</param>
/// <param name="Batch">The batch indicator made with the argument.</param>
/// <param name="Streaming">The streaming form made with the argument; null when there is none.</param>
/// <param name="Argument">The parameter the refusal names; null when it names none (a check of the bars).</param>
internal readonly record struct Refusal(string What, Func<object> Batch, Func<object>? Streaming, string? Argument)
{
    /// <summary>The parameter the streaming form's refusal names, where it names another than the batch indicator's: a form given
    /// its rows one at a time names the row it is given.</summary>
    public string? FormArgument { get; init; } = Argument;
}

/// <summary>One calculator of the package, as the indicator contract reads it: how it is made over a walk of bars, and what it then
/// draws, pins, computes and refuses.</summary>
/// <param name="Name">The batch indicator.</param>
/// <param name="Make">The indicator over the given bars, with the parameters <see cref="Draws"/> names.</param>
internal readonly record struct IndicatorCase(string Name, Func<OracleBars, Indicator> Make)
{
    /// <summary>What <c>Apply</c> draws over <see cref="IndicatorCases.Bars"/>, in the order it is added: each series as its kind
    /// without the <c>Series</c> ending and its label (<c>"Line GK(5)"</c>, <c>"Area"</c> for an unlabelled band), then each reference
    /// line at its value.</summary>
    public string[] Draws { get; init; } = [];

    /// <summary>The lowest and highest value the indicator pins its pane's Y axis to, null on a side it leaves to the data.</summary>
    public PaneRange Pane { get; init; }

    /// <summary>Whether what it draws stands at the bars it is worked out at; false for bars by category, a histogram across the
    /// price range and levels from two prices.</summary>
    public bool AtBars { get; init; } = true;

    /// <summary>How far a line stands ahead of the bar its value is worked out at, by label (an Ichimoku's spans and cloud stand its
    /// displacement ahead, its lagging line behind); null when every line stands at its own bar.</summary>
    public Func<string?, int>? Shift { get; init; }

    /// <summary>The bar the first value of the main line (the first line of the result) is worked out at, over
    /// <see cref="IndicatorCases.Bars"/> with the parameters of <see cref="Make"/>; null when the indicator has no value per bar.</summary>
    public int? FirstValue { get; init; }

    /// <summary>The fewest bars it computes a value over: it has none over one bar fewer, and none over no bars at all; by default the
    /// bar after <see cref="FirstValue"/>, null when it has no value per bar or refuses an empty input.</summary>
    public int? Needs
    {
        get => RefusesNoBars ? null : field ?? FirstValue + 1;
        init;
    }

    /// <summary>Whether it refuses an empty input (one of <see cref="Refuses"/> says how) rather than computing nothing.</summary>
    public bool RefusesNoBars { get; init; }

    /// <summary>The label of the indicator made with nothing but its bars, every parameter left at its default; null when a parameter
    /// has no default.</summary>
    public string? ByDefault { get; init; }

    /// <summary>The value every value of the main line takes over bars that never move; null when it is not one value.</summary>
    public double? OnFlatBars { get; init; }

    /// <summary>Whether what it draws may leave the range its pane shows: a smoothing that overshoots the range of what it smooths.</summary>
    public bool LeavesItsPane { get; init; }

    /// <summary>The arguments it does not take.</summary>
    public Refusal[] Refuses { get; init; } = [];
}

/// <summary>Every calculator of the package, one case each: the one list the indicator contract (<see cref="IndicatorContractTests"/>)
/// reads. A calculator added without a case turns the contract's guard red.</summary>
internal static class IndicatorCases
{
    /// <summary>The bars every case is made over and drawn on.</summary>
    public static OracleBars Bars { get; } = OracleBars.Walk(60);

    private static double[] Close => Bars.Close;

    private static OhlcvBars Hlc => OhlcvBars.Hlc(Bars.High, Bars.Low, Bars.Close);

    private static OhlcvBars Hl => OhlcvBars.Hl(Bars.High, Bars.Low);

    private static OhlcvBars Cv => OhlcvBars.Cv(Bars.Close, Bars.Volume);

    private static OhlcvBars Ohlc => OhlcvBars.Ohlc(Bars.Open, Bars.High, Bars.Low, Bars.Close);

    private static OhlcvBars Hlcv => OhlcvBars.Hlcv(Bars.High, Bars.Low, Bars.Close, Bars.Volume);

    /// <summary>Two bars, the second with its high below its low.</summary>
    private static OhlcvBars Crossed => OhlcvBars.Hlc([101.0, 99], [99.0, 101], [100.0, 100.5]);

    /// <summary>Two bars, the second at a price of zero on every side.</summary>
    private static OhlcvBars Unpriced => OhlcvBars.Ohlc([1.0, 0], [1.0, 0], [1.0, 0], [1.0, 0]);

    private static readonly TransferEntropyOptions EntropyOptions = new(Neighbours: 3, Window: 24);

    private static TransferEntropy Entropy(TransferEntropyOptions options, int count = 50) => new(Close[..count], Close[..count], options);

    /// <summary>A streaming form after it is given what <paramref name="feed"/> gives it.</summary>
    private static object Fed<TForm>(TForm form, Action<TForm> feed) where TForm : IStreamingIndicator
    {
        feed(form);
        return form;
    }

    /// <summary>Every case, by name.</summary>
    public static IReadOnlyList<IndicatorCase> All { get; } =
    [
        new("AdaptiveStochastic", b => new AdaptiveStochastic(OhlcvBars.Hlc(b.High, b.Low, b.Close), 3))
        {
            Draws = ["Line AdaptStoch(3)"], Pane = new(0, 100), LeavesItsPane = true, FirstValue = 6, ByDefault = "AdaptStoch(3)", OnFlatBars = 50,
            Refuses = [new("a smoothing of 0", () => new AdaptiveStochastic(Hlc, 0), () => new StreamingAdaptiveStochastic(0), "smoothingPeriod"),
                       new("a high below its low", () => new AdaptiveStochastic(Crossed), () => Fed(new StreamingAdaptiveStochastic(), f => f.AppendBar(new OhlcvBar(10, 9, 11, 10, 100))), null)],
        },
        new("Adx", b => new Adx(OhlcvBars.Hlc(b.High, b.Low, b.Close), 4))
        {
            Draws = ["Line ADX(4)", "Line +DI", "Line -DI"], Pane = new(0, 100), FirstValue = 8, ByDefault = "ADX(14)", OnFlatBars = 0,
        },
        new("AmihudIlliquidity", b => new AmihudIlliquidity(OhlcvBars.Cv(b.Close, b.Volume), 5))
        {
            Draws = ["Line ILLIQ(5)"], FirstValue = 5, ByDefault = "ILLIQ(20)", OnFlatBars = 0,
            Refuses = [new("a period of 0", () => new AmihudIlliquidity(Cv, 0), () => new StreamingAmihudIlliquidity(0), "period"),
                       new("a close of 0", () => new AmihudIlliquidity(OhlcvBars.Cv([100.0, 0], [1.0, 1])), null, "bars")],
        },
        new("AroonOscillator", b => new AroonOscillator(OhlcvBars.Hl(b.High, b.Low), 5))
        {
            Draws = ["Line Aroon(5)"], Pane = new(-100, 100), FirstValue = 5, ByDefault = "Aroon(25)", OnFlatBars = 0,
            Refuses = [new("a period of 1", () => new AroonOscillator(Hl, 1), () => new StreamingAroonOscillator(1), "period"),
                       new("a high below its low", () => new AroonOscillator(Crossed), null, null)],
        },
        new("Atr", b => new Atr(OhlcvBars.Hlc(b.High, b.Low, b.Close), 5))
        {
            Draws = ["Line ATR(5)"], FirstValue = 5, ByDefault = "ATR(14)", OnFlatBars = 0,
        },
        new("Bocpd", b => new Bocpd(b.Close, 0.05, 4.0, 30))
        {
            Draws = ["Line BOCPD(h=0.05)"], Pane = new(0, 1), FirstValue = 1, ByDefault = "BOCPD(h=0.01)", OnFlatBars = 0.05,
            Refuses = [new("a hazard of 0", () => new Bocpd(Close, 0), () => new StreamingBocpd(hazard: 0), "hazard"),
                       new("a hazard of 1", () => new Bocpd(Close, 1), () => new StreamingBocpd(hazard: 1), "hazard"),
                       new("a prior variance of 0", () => new Bocpd(Close, priorVariance: 0), () => new StreamingBocpd(priorVariance: 0), "priorVariance"),
                       new("a longest run of 0", () => new Bocpd(Close, maxRunLength: 0), () => new StreamingBocpd(maxRunLength: 0), "maxRunLength")],
        },
        new("BollingerBands", b => new BollingerBands(b.Close, 5, 2.0))
        {
            Draws = ["Area", "Line BB(5,2)"], FirstValue = 4, ByDefault = "BB(20,2)", OnFlatBars = 100,
        },
        new("Cci", b => new Cci(OhlcvBars.Hlc(b.High, b.Low, b.Close), 5))
        {
            Draws = ["Line CCI(5)", "Reference 100", "Reference -100"], FirstValue = 4, ByDefault = "CCI(20)", OnFlatBars = 0,
        },
        new("CgOscillator", b => new CgOscillator(b.Close, 5))
        {
            Draws = ["Line CG(5)"], FirstValue = 4, ByDefault = "CG(10)", OnFlatBars = 0,
            Refuses = [new("a period of 1", () => new CgOscillator(Close, 1), () => new StreamingCgOscillator(1), "period")],
        },
        new("CorwinSchultz", b => new CorwinSchultz(OhlcvBars.Hl(b.High, b.Low), 5))
        {
            Draws = ["Line CS(5)"], FirstValue = 5, ByDefault = "CS(20)", OnFlatBars = 0,
            Refuses = [new("a period of 1", () => new CorwinSchultz(Hl, 1), () => new StreamingCorwinSchultz(1), "period"),
                       new("a price of 0", () => new CorwinSchultz(OhlcvBars.Hl([101.0, 0], [99.0, 0])), null, null),
                       new("a high below its low", () => new CorwinSchultz(Crossed), null, null)],
        },
        new("Cusum", b => new Cusum(b.Close, 0.02))
        {
            Draws = ["Line CUSUM(h=0.02)"], FirstValue = 1, OnFlatBars = 0,
            Refuses = [new("a threshold of 0", () => new Cusum(Close, 0), () => new StreamingCusum(0), "threshold"),
                       new("a price of 0", () => new Cusum([100.0, 0], 0.01), () => Fed(new StreamingCusum(1), f => f.Append(0.0)), "prices")],
        },
        new("CyberCycle", b => new CyberCycle(b.Close, 0.07))
        {
            Draws = ["Line CC(0.07)"], FirstValue = 3, ByDefault = "CC(0.07)", OnFlatBars = 0,
            Refuses = [new("an alpha of 0", () => new CyberCycle(Close, 0), () => new StreamingCyberCycle(0), "alpha"),
                       new("an alpha of 1", () => new CyberCycle(Close, 1), () => new StreamingCyberCycle(1), "alpha")],
        },
        new("Decycler", b => new Decycler(b.Close, 10))
        {
            Draws = ["Line Decycler(10)"], FirstValue = 0, Needs = 3, ByDefault = "Decycler(60)", OnFlatBars = 100,
            Refuses = [new("a high-pass period of 3", () => new Decycler(Close, 3), () => new StreamingDecycler(3), "hpPeriod")],
        },
        new("DispersionIndex", b => new DispersionIndex(StreamingRows.FeatureRows(b)))
        {
            Draws = ["Line Dispersion(3)"], FirstValue = 0, ByDefault = "Dispersion(3)",
            Refuses = [new("rows of one signal", () => new DispersionIndex([[0.5]]), () => Fed(new StreamingDispersionIndex(), f => f.Append([1.0])), "signals") { FormArgument = "input" },
                       new("rows of no signal", () => new DispersionIndex([[]]), null, "signals"),
                       new("rows of two lengths", () => new DispersionIndex([[1.0, 2, 3], [1.0, 2]]), () => Fed(new StreamingDispersionIndex(), f => { f.Append([1.0, 2, 3]); f.Append([1.0, 2]); }), "features") { FormArgument = "input" }],
        },
        new("DrawDown", b => new DrawDown(b.Close))
        {
            Draws = ["Area Drawdown"], Pane = new(null, 0), FirstValue = 0, ByDefault = "Drawdown", OnFlatBars = 0,
        },
        new("EaseOfMovement", b => new EaseOfMovement(OhlcvBars.Hlv(b.High, b.Low, b.Volume), 5))
        {
            Draws = ["Line EMV(5)"], FirstValue = 5, ByDefault = "EMV(14)", OnFlatBars = 0,
            Refuses = [new("a period of 1", () => new EaseOfMovement(Hlcv, 1), () => new StreamingEaseOfMovement(1), "period"),
                       new("a scale of 0", () => new EaseOfMovement(Hlcv, scale: 0), () => new StreamingEaseOfMovement(scale: 0), "scale")],
        },
        new("EhlersITrend", b => new EhlersITrend(b.Close))
        {
            Draws = ["Line iTrend"], FirstValue = 6, ByDefault = "iTrend", OnFlatBars = 100,
        },
        new("EhlersSineWave", b => new EhlersSineWave(b.Close))
        {
            Draws = ["Line SineWave", "Line LeadSine"], Pane = new(-1.2, 1.2), FirstValue = 6, ByDefault = "SineWave",
        },
        new("EhlersSuperSmoother", b => new EhlersSuperSmoother(b.Close, 5))
        {
            Draws = ["Line SS(5)"], FirstValue = 0, ByDefault = "SS(10)", OnFlatBars = 100,
            Refuses = [new("no input", () => new EhlersSuperSmoother(null!), null, "input"),
                       new("a period of 1", () => new EhlersSuperSmoother(Close, 1), () => new StreamingEhlersSuperSmoother(1), "period")],
        },
        new("Ema", b => new Ema(b.Close, 5))
        {
            Draws = ["Line EMA(5)"], FirstValue = 0, Needs = 5,
        },
        new("EquityCurve", b => new EquityCurve(b.Close, 10000))
        {
            Draws = ["Line Equity"], ByDefault = "Equity",
        },
        new("FibonacciRetracement", _ => new FibonacciRetracement(100, 200))
        {
            Draws = ["Reference 176.4", "Reference 161.8", "Reference 150", "Reference 138.2", "Reference 121.4"], AtBars = false,
        },
        new("ForceIndex", b => new ForceIndex(OhlcvBars.Cv(b.Close, b.Volume), 5))
        {
            Draws = ["Line Force(5)"], FirstValue = 1, ByDefault = "Force(13)", OnFlatBars = 0,
            Refuses = [new("a period of 0", () => new ForceIndex(Cv, 0), () => new StreamingForceIndex(0), "period")],
        },
        new("FractionalDifferentiation", b => new FractionalDifferentiation(b.Close, 0.4, 1e-3))
        {
            Draws = ["Line FFD(d=0.40)"], FirstValue = 54, ByDefault = "FFD(d=0.40)",
            Refuses = [new("a d of 0", () => new FractionalDifferentiation(Close, 0), () => new StreamingFractionalDifferentiation(0), "d"),
                       new("a d of 1", () => new FractionalDifferentiation(Close, 1), () => new StreamingFractionalDifferentiation(1), "d"),
                       new("a tolerance of 0", () => new FractionalDifferentiation(Close, 0.4, 0), () => new StreamingFractionalDifferentiation(0.4, 0), "tolerance")],
        },
        new("GarmanKlass", b => new GarmanKlass(OhlcvBars.Ohlc(b.Open, b.High, b.Low, b.Close), 5))
        {
            Draws = ["Line GK(5)"], FirstValue = 4, ByDefault = "GK(20)", OnFlatBars = 0,
            Refuses = [new("a price of 0", () => new GarmanKlass(Unpriced), null, null)],
        },
        new("Ichimoku", b => new Ichimoku(OhlcvBars.Hlc(b.High, b.Low, b.Close), 3, 6, 12, 6))
        {
            Draws = ["Line Span A", "Line Span B", "Line Tenkan", "Line Kijun", "Line Chikou", "Area Cloud"], Shift = IchimokuShift,
            FirstValue = 2, ByDefault = "Ichimoku", OnFlatBars = 100,
        },
        new("InverseFisherTransform", b => new InverseFisherTransform(b.Close, 0.05))
        {
            Draws = ["Line IFT(scale=0.05)"], Pane = new(-1, 1), FirstValue = 0, RefusesNoBars = true, ByDefault = "IFT(scale=1)",
            Refuses = [new("no input", () => new InverseFisherTransform(null!), null, "input"),
                       new("an empty input", () => new InverseFisherTransform([]), null, "input"),
                       new("a scale of 0", () => new InverseFisherTransform(Close, 0), () => new StreamingInverseFisherTransform(0), "scale")],
        },
        new("KaufmanEfficiencyRatio", b => new KaufmanEfficiencyRatio(b.Close, 5))
        {
            Draws = ["Line ER(5)"], Pane = new(0, 1), FirstValue = 5, ByDefault = "ER(10)", OnFlatBars = 0,
            Refuses = [new("a period of 0", () => new KaufmanEfficiencyRatio(Close, 0), () => new StreamingKaufmanEfficiencyRatio(0), "period")],
        },
        new("KeltnerChannels", b => new KeltnerChannels(OhlcvBars.Hlc(b.High, b.Low, b.Close), 5, 1.5))
        {
            Draws = ["Area", "Line KC(5,1.5)"], FirstValue = 5, ByDefault = "KC(20,1.5)", OnFlatBars = 100,
        },
        new("KlingerVolumeOscillator", b => new KlingerVolumeOscillator(OhlcvBars.Hlcv(b.High, b.Low, b.Close, b.Volume), 3, 6, 4))
        {
            Draws = ["Line KVO(3/6/4)", "Line Signal"], FirstValue = 1, ByDefault = "KVO(34/55/13)", OnFlatBars = 0,
            Refuses = [new("a fast period of 1", () => new KlingerVolumeOscillator(Hlcv, 1), () => new StreamingKlingerVolumeOscillator(1), "fastPeriod"),
                       new("a slow period not above the fast", () => new KlingerVolumeOscillator(Hlcv, 34, 34), () => new StreamingKlingerVolumeOscillator(34, 34), "slowPeriod"),
                       new("a signal period of 0", () => new KlingerVolumeOscillator(Hlcv, signalPeriod: 0), () => new StreamingKlingerVolumeOscillator(signalPeriod: 0), "signalPeriod")],
        },
        new("LaguerreRsi", b => new LaguerreRsi(b.Close, 0.2))
        {
            Draws = ["Line LagRSI(0.20)"], Pane = new(0, 1), FirstValue = 1, ByDefault = "LagRSI(0.20)", OnFlatBars = 0,
            Refuses = [new("an alpha of 0", () => new LaguerreRsi(Close, 0), () => new StreamingLaguerreRsi(0), "alpha"),
                       new("an alpha of 1", () => new LaguerreRsi(Close, 1), () => new StreamingLaguerreRsi(1), "alpha")],
        },
        new("Macd", b => new Macd(b.Close, 3, 6, 4))
        {
            Draws = ["Line MACD(3,6,4)", "Line Signal", "Bar Histogram"], FirstValue = 5, ByDefault = "MACD(12,26,9)", OnFlatBars = 0,
        },
        new("MamaFama", b => new MamaFama(b.Close, 0.5, 0.05))
        {
            Draws = ["Line MAMA(0.50/0.05)", "Line FAMA"], FirstValue = 6, ByDefault = "MAMA(0.50/0.05)", OnFlatBars = 100,
            Refuses = [new("a fast limit of 0", () => new MamaFama(Close, 0), () => new StreamingMamaFama(0), "fastLimit"),
                       new("a fast limit of 1.5", () => new MamaFama(Close, 1.5), () => new StreamingMamaFama(1.5), "fastLimit"),
                       new("a slow limit of 0", () => new MamaFama(Close, 0.5, 0), () => new StreamingMamaFama(0.5, 0), "slowLimit"),
                       new("a slow limit not below the fast", () => new MamaFama(Close, 0.5, 0.5), () => new StreamingMamaFama(0.5, 0.5), "slowLimit")],
        },
        new("Obv", b => new Obv(b.Close, b.Volume))
        {
            Draws = ["Line OBV"], FirstValue = 0, ByDefault = "OBV", OnFlatBars = 1000,
        },
        new("ParabolicSar", b => new ParabolicSar(b.High, b.Low, 0.02, 0.2))
        {
            Draws = ["Scatter SAR (Long)", "Scatter SAR (Short)"], FirstValue = 0, Needs = 2, ByDefault = "SAR",
        },
        new("PermutationEntropy", b => new PermutationEntropy(b.Close, 3, 8))
        {
            Draws = ["Line PE(d=3,W=8)"], Pane = new(0, 1), FirstValue = 7, ByDefault = "PE(d=4,W=100)", OnFlatBars = 0,
            Refuses = [new("an order of 1", () => new PermutationEntropy(Close, 1, 4), () => new StreamingPermutationEntropy(1, 4), "order"),
                       new("an order of 8", () => new PermutationEntropy(Close, 8, 20), () => new StreamingPermutationEntropy(8, 20), "order"),
                       new("a window not above the order", () => new PermutationEntropy(Close, 4, 4), () => new StreamingPermutationEntropy(4, 4), "window")],
        },
        new("ProfitLoss", b => new ProfitLoss([.. b.Close.Select(c => c - 100)]))
        {
            Draws = ["Bar Profit", "Bar Loss"], Pane = new((Bars.Close.Min() - 100) * 1.1, null), AtBars = false, ByDefault = "P&L", OnFlatBars = 0,
        },
        new("RelativeVigorIndex", b => new RelativeVigorIndex(OhlcvBars.Ohlc(b.Open, b.High, b.Low, b.Close), 4))
        {
            Draws = ["Line RVI(4)", "Line Signal"], FirstValue = 6, Needs = 8, ByDefault = "RVI(10)", OnFlatBars = 0,
            Refuses = [new("a period of 1", () => new RelativeVigorIndex(Ohlc, 1), () => new StreamingRelativeVigorIndex(1), "period")],
        },
        new("Roc", b => new Roc(b.Close, 5))
        {
            Draws = ["Line ROC(5)"], FirstValue = 5, OnFlatBars = 0,
        },
        new("RollSpread", b => new RollSpread(b.Close, 5))
        {
            Draws = ["Line Roll(5)"], FirstValue = 6, ByDefault = "Roll(20)", OnFlatBars = 0,
            Refuses = [new("a period of 1", () => new RollSpread(Close, 1), () => new StreamingRollSpread(1), "period")],
        },
        new("RoofingFilter", b => new RoofingFilter(b.Close, 12, 5))
        {
            Draws = ["Line Roof(12/5)"], FirstValue = 0, Needs = 3, ByDefault = "Roof(48/10)", OnFlatBars = 0,
            Refuses = [new("a high-pass period of 3", () => new RoofingFilter(Close, 3), () => new StreamingRoofingFilter(3), "hpPeriod"),
                       new("a low-pass period of 1", () => new RoofingFilter(Close, 48, 1), () => new StreamingRoofingFilter(48, 1), "lpPeriod"),
                       new("a low-pass period not below the high-pass", () => new RoofingFilter(Close, 10, 10), () => new StreamingRoofingFilter(10, 10), "lpPeriod")],
        },
        new("Rsi", b => new Rsi(b.Close, 5))
        {
            Draws = ["Line RSI(5)"], Pane = new(0, 100), FirstValue = 5, ByDefault = "RSI(14)", OnFlatBars = 100,
        },
        new("Sma", b => new Sma(b.Close, 5))
        {
            Draws = ["Line SMA(5)"], FirstValue = 4, OnFlatBars = 100,
        },
        new("SqueezeMomentum", b => new SqueezeMomentum(OhlcvBars.Hlc(b.High, b.Low, b.Close), 5, 2.0, 1.5))
        {
            Draws = ["Line Squeeze(5)"], FirstValue = 6, ByDefault = "Squeeze(20)", OnFlatBars = 0,
            Refuses = [new("a period of 1", () => new SqueezeMomentum(Hlc, 1), () => new StreamingSqueezeMomentum(1), "period"),
                       new("a band multiplier of 0", () => new SqueezeMomentum(Hlc, 20, 0), () => new StreamingSqueezeMomentum(20, 0), "bbMult"),
                       new("a channel multiplier of 0", () => new SqueezeMomentum(Hlc, 20, 2, 0), () => new StreamingSqueezeMomentum(20, 2, 0), "kcMult"),
                       new("a high below its low", () => new SqueezeMomentum(Crossed), null, null)],
        },
        new("Stochastic", b => new Stochastic(OhlcvBars.Hlc(b.High, b.Low, b.Close), 5, 3))
        {
            Draws = ["Line %K(5)", "Line %D(3)"], Pane = new(0, 100), FirstValue = 6, ByDefault = "%K(14)", OnFlatBars = 50,
        },
        new("Supertrend", b => new Supertrend(OhlcvBars.Hlc(b.High, b.Low, b.Close), 5, 3.0))
        {
            Draws = ["Line ST(5,3)"], FirstValue = 5, ByDefault = "ST(10,3)", OnFlatBars = 100,
            Refuses = [new("a period of 0", () => new Supertrend(Hlc, 0), () => new StreamingSupertrend(0), "period"),
                       new("a multiplier of 0", () => new Supertrend(Hlc, multiplier: 0), () => new StreamingSupertrend(multiplier: 0), "multiplier")],
        },
        new("TransferEntropy", b => new TransferEntropy(
            [.. Enumerable.Range(0, b.Count).Select(i => b.Close[i] - b.Open[i])],
            [.. Enumerable.Range(0, b.Count).Select(i => b.High[i] - b.Low[i])], EntropyOptions))
        {
            Draws = ["Line TE(k=3,lag=1,W=24)"], FirstValue = 0, ByDefault = "TE(bins=8,lag=1)",
            Refuses = [new("no source", () => new TransferEntropy(null!, Close), null, "source"),
                       new("no target", () => new TransferEntropy(Close, null!), null, "target"),
                       new("a target of another length", () => new TransferEntropy(Close, Close[..10]), null, "target"),
                       new("series too short for the lag", () => new TransferEntropy(Close[..2], Close[..2], 8, 1), null, "source"),
                       new("1 bin", () => new TransferEntropy(Close, Close, 1), null, "Bins"),
                       new("a lag of 0", () => new TransferEntropy(Close, Close, 4, 0), null, "Lag"),
                       new("no neighbour", () => Entropy(new(Neighbours: 0)), null, "Neighbours"),
                       new("series too short for the neighbours", () => Entropy(new(Neighbours: 4), 5), null, "source"),
                       new("a lag past every series", () => new TransferEntropy(Close, Close, 8, int.MaxValue), null, "source"),
                       new("a lag and neighbours past every series", () => Entropy(new(Lag: int.MaxValue - 2)), null, "source"),
                       new("a lag and neighbours past every window", () => Entropy(new(Lag: int.MaxValue - 2, Window: 30)), () => new StreamingTransferEntropy(new(Lag: int.MaxValue - 2, Window: 30)), "Window"),
                       new("a condition of another length", () => Entropy(new(Condition: Close[..49])), null, "Condition"),
                       new("a condition with the binned estimate", () => Entropy(new(TransferEntropyEstimator.Binned, Condition: Close[..50])), null, "Condition"),
                       new("a window too short for the neighbours", () => Entropy(new(Neighbours: 4, Window: 5)), () => new StreamingTransferEntropy(new(Neighbours: 4, Window: 5)), "Window"),
                       new("a stride of 0", () => Entropy(new(Window: 20, Stride: 0)), () => new StreamingTransferEntropy(new(Window: 30, Stride: 0)), "Stride"),
                       new("a window with the binned estimate", () => Entropy(new(TransferEntropyEstimator.Binned, Window: 20)), () => new StreamingTransferEntropy(new(TransferEntropyEstimator.Binned, Window: 30)), "Window"),
                       new("a stride without a window", () => Entropy(new(Stride: 5)), null, "Stride")],
        },
        new("TurbulenceIndex", b => new TurbulenceIndex(StreamingRows.FeatureRows(b), 10, 1e-6))
        {
            Draws = ["Line Turb(10)"], FirstValue = 10, ByDefault = "Turb(252)", OnFlatBars = 0,
            Refuses = [new("a window of 1", () => new TurbulenceIndex(StreamingRows.FeatureRows(Bars), 1), () => new StreamingTurbulenceIndex(1), "window"),
                       new("a regularisation below 0", () => new TurbulenceIndex(StreamingRows.FeatureRows(Bars), 10, -1), () => new StreamingTurbulenceIndex(regularization: -1), "regularization"),
                       new("eleven features", () => new TurbulenceIndex([new double[11], new double[11]]), () => Fed(new StreamingTurbulenceIndex(5), f => f.Append(new double[11])), "features") { FormArgument = "input" },
                       new("rows of two lengths", () => new TurbulenceIndex([[1.0, 2], [3.0]]), () => Fed(new StreamingTurbulenceIndex(5), f => { f.Append([1.0, 2]); f.Append([1.0, 2, 3]); }), "features") { FormArgument = "input" }],
        },
        new("TwiggsMoneyFlow", b => new TwiggsMoneyFlow(OhlcvBars.Hlcv(b.High, b.Low, b.Close, b.Volume), 5))
        {
            Draws = ["Line TMF(5)"], Pane = new(-1, 1), FirstValue = 1, ByDefault = "TMF(21)", OnFlatBars = 0,
            Refuses = [new("a period of 1", () => new TwiggsMoneyFlow(Hlcv, 1), () => new StreamingTwiggsMoneyFlow(1), "period")],
        },
        new("VolumeIndicator", b => new VolumeIndicator(b.Volume))
        {
            Draws = ["Bar Volume"], Pane = new(0, null), AtBars = false, FirstValue = 0, ByDefault = "Volume", OnFlatBars = 1000,
        },
        new("VolumeProfile", b => new VolumeProfile(OhlcvBars.Hlcv(b.High, b.Low, b.Close, b.Volume), 8))
        {
            Draws = ["Bar Volume profile"], AtBars = false, ByDefault = "Volume profile",
            Refuses = [new("no bin", () => new VolumeProfile(Hlcv, 0), null, "bins")],
        },
        new("Vpin", b => new Vpin(OhlcvBars.Cv(b.Close, b.Volume), 4, 4))
        {
            Draws = ["Line VPIN(4)"], Pane = new(0, 1), FirstValue = 5, ByDefault = "VPIN(50)", OnFlatBars = 0,
            Refuses = [new("a bucket period of 0", () => new Vpin(Cv, 0), () => new StreamingVpin(0), "bucketPeriod"),
                       new("a sigma period of 0", () => new Vpin(Cv, 50, 0), () => new StreamingVpin(50, 0), "sigmaPeriod"),
                       new("a close of 0", () => new Vpin(OhlcvBars.Cv([100.0, 0], [1.0, 1]), 2, 2), null, "bars")],
        },
        new("Vwap", b => new Vwap(OhlcvBars.Hlcv(b.High, b.Low, b.Close, b.Volume)))
        {
            Draws = ["Line VWAP"], FirstValue = 0, RefusesNoBars = true, ByDefault = "VWAP", OnFlatBars = 100,
            Refuses = [new("an anchor before the first bar", () => new Vwap(Hlcv, -1), null, "anchor"),
                       new("an anchor past the last bar", () => new Vwap(Hlcv, Bars.Count), null, "anchor"),
                       new("an anchor over no bars", () => new Vwap(OhlcvBars.Hlcv([], [], [], [])), null, "anchor"),
                       new("volumes of another length", () => new Vwap(Close, Close[..10]), null, null),
                       new("bars of another length", () => new Vwap(new OhlcvBars(Close, Close, Close, [], Close[..10])), null, null)],
        },
        new("VwapZScore", b => new VwapZScore(OhlcvBars.Cv(b.Close, b.Volume), 5))
        {
            Draws = ["Line VwapZ(5)"], FirstValue = 4, ByDefault = "VwapZ(20)", OnFlatBars = 0,
            Refuses = [new("a window of 1", () => new VwapZScore(Cv, 1), () => new StreamingVwapZScore(1), "window")],
        },
        new("WaveletEnergyRatio", b => new WaveletEnergyRatio(b.Close, 8, 0))
        {
            Draws = ["Line WER(W=8,L=0)"], Pane = new(0, 1), FirstValue = 7, ByDefault = "WER(W=64,L=0)", OnFlatBars = 0,
            Refuses = [new("a window of 48", () => new WaveletEnergyRatio(Close, 48), () => new StreamingWaveletEnergyRatio(48), "window"),
                       new("a window of 2", () => new WaveletEnergyRatio(Close, 2), () => new StreamingWaveletEnergyRatio(2), "window"),
                       new("a level below 0", () => new WaveletEnergyRatio(Close, 8, -1), () => new StreamingWaveletEnergyRatio(8, -1), "level"),
                       new("a level as deep as the window", () => new WaveletEnergyRatio(Close, 64, 6), () => new StreamingWaveletEnergyRatio(64, 6), "level")],
        },
        new("WaveletEntropy", b => new WaveletEntropy(b.Close, 8))
        {
            Draws = ["Line WEnt(W=8)"], Pane = new(0, 1), FirstValue = 7, ByDefault = "WEnt(W=64)", OnFlatBars = 0,
            Refuses = [new("a window of 48", () => new WaveletEntropy(Close, 48), () => new StreamingWaveletEntropy(48), "window"),
                       new("a window of 2", () => new WaveletEntropy(Close, 2), () => new StreamingWaveletEntropy(2), "window")],
        },
        new("WilliamsR", b => new WilliamsR(OhlcvBars.Hlc(b.High, b.Low, b.Close), 5))
        {
            Draws = ["Line %R(5)", "Reference -20", "Reference -80"], Pane = new(-100, 0), FirstValue = 4, ByDefault = "%R(14)", OnFlatBars = -50,
        },
        new("YangZhang", b => new YangZhang(OhlcvBars.Ohlc(b.Open, b.High, b.Low, b.Close), 5))
        {
            Draws = ["Line YZ(5)"], FirstValue = 5, ByDefault = "YZ(20)", OnFlatBars = 0,
            Refuses = [new("a period of 1", () => new YangZhang(Ohlc, 1), () => new StreamingYangZhang(1), "period"),
                       new("a price of 0", () => new YangZhang(Unpriced), null, null)],
        },
        new("YangZhangVolRatio", b => new YangZhangVolRatio(OhlcvBars.Ohlc(b.Open, b.High, b.Low, b.Close), 3, 6))
        {
            Draws = ["Line YZRatio(3/6)"], FirstValue = 6, ByDefault = "YZRatio(20/60)", OnFlatBars = 1,
            Refuses = [new("a short window of 1", () => new YangZhangVolRatio(Ohlc, 1, 60), () => new StreamingYangZhangVolRatio(1, 60), "shortWindow"),
                       new("a long window not above the short", () => new YangZhangVolRatio(Ohlc, 20, 20), () => new StreamingYangZhangVolRatio(20, 20), "longWindow")],
        },
    ];

    /// <summary>The indicator made over <see cref="IndicatorCases.Bars"/> with nothing but its bars, every other parameter left at
    /// its default; null when every constructor asks for a number of its own.</summary>
    public static Indicator? MadeByDefault(string name)
    {
        var type = Of(name).Make(Bars).GetType();
        var b = Bars;
        foreach (var constructor in type.GetConstructors())
        {
            var arguments = constructor.GetParameters().Select(p => p.HasDefaultValue ? p.DefaultValue
                : p.ParameterType == typeof(OhlcvBars) ? new OhlcvBars(b.High, b.Low, b.Close, b.Open, b.Volume)
                : p.ParameterType == typeof(double[][]) ? StreamingRows.FeatureRows(b)
                : p.ParameterType == typeof(double[]) ? (p.Name is "volume" or "volumes" ? b.Volume : b.Close)
                : Missing.Value).ToArray();
            if (!arguments.Contains(Missing.Value)) return (Indicator)constructor.Invoke(arguments);
        }

        return null;
    }

    /// <summary>The names of the cases, as a theory takes them.</summary>
    public static TheoryData<string> Names() => new(All.Select(c => c.Name));

    /// <summary>The case of an indicator.</summary>
    public static IndicatorCase Of(string name) => All.Single(c => c.Name == name);

    private static int IchimokuShift(string? label) => label switch
    {
        "Span A" or "Span B" or "Cloud" => 6,
        "Chikou" => -6,
        _ => 0,
    };
}
