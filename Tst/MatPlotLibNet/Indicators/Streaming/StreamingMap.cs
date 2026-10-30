// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Indicators.Streaming;

/// <summary>What the classification map says about one indicator: how it is streamed, or why it cannot be.</summary>
/// <param name="Name">The batch indicator.</param>
/// <param name="Kind">How the streaming form gets its value; null when the indicator cannot be streamed.</param>
/// <param name="Reason">Why the indicator cannot be streamed; null when it can.</param>
public readonly record struct StreamingClass(string Name, StreamingKind? Kind, string? Reason)
{
    /// <summary>Whether a streaming form exists for the indicator.</summary>
    public bool IsStreamed => Kind is not null;
}

/// <summary>The classification map: every indicator the package calculates, and for each how it is streamed or why it is not.
/// It is the one place that states the kind of a streaming form; a calculator added without a line here turns
/// <see cref="StreamingMapTests"/> red, and the line is where the choice between keeping a window and carrying a state is
/// written down.</summary>
public static class StreamingMap
{
    private static StreamingClass Streamed(string name, StreamingKind kind) => new(name, kind, null);

    private static StreamingClass Not(string name, string reason) => new(name, null, reason);

    /// <summary>Every indicator, by name.</summary>
    public static IReadOnlyList<StreamingClass> All { get; } =
    [
        Streamed("AdaptiveStochastic", StreamingKind.Recursive),
        Streamed("Adx", StreamingKind.Recursive),
        Streamed("AmihudIlliquidity", StreamingKind.Window),
        Streamed("AroonOscillator", StreamingKind.Window),
        Streamed("Atr", StreamingKind.Recursive),
        Streamed("Bocpd", StreamingKind.Recursive),
        Streamed("BollingerBands", StreamingKind.Window),
        Streamed("Cci", StreamingKind.Window),
        Streamed("CgOscillator", StreamingKind.Window),
        Streamed("CorwinSchultz", StreamingKind.Window),
        Streamed("Cusum", StreamingKind.StateMachine),
        Streamed("CyberCycle", StreamingKind.Recursive),
        Streamed("Decycler", StreamingKind.Recursive),
        Streamed("DispersionIndex", StreamingKind.Multivariate),
        Streamed("DrawDown", StreamingKind.Cumulative),
        Streamed("EaseOfMovement", StreamingKind.Window),
        Streamed("EhlersITrend", StreamingKind.Recursive),
        Streamed("EhlersSineWave", StreamingKind.Recursive),
        Streamed("EhlersSuperSmoother", StreamingKind.Recursive),
        Streamed("Ema", StreamingKind.Recursive),
        Streamed("EquityCurve", StreamingKind.Cumulative),
        Not("FibonacciRetracement", "five fixed levels drawn from two numbers the caller gives; there is no value per bar for a stream to say"),
        Streamed("ForceIndex", StreamingKind.Recursive),
        Streamed("FractionalDifferentiation", StreamingKind.Window),
        Streamed("GarmanKlass", StreamingKind.Window),
        Streamed("Ichimoku", StreamingKind.Window),
        Streamed("InverseFisherTransform", StreamingKind.Window),
        Streamed("KaufmanEfficiencyRatio", StreamingKind.Window),
        Streamed("KeltnerChannels", StreamingKind.Recursive),
        Streamed("KlingerVolumeOscillator", StreamingKind.StateMachine),
        Streamed("LaguerreRsi", StreamingKind.Recursive),
        Streamed("Macd", StreamingKind.Recursive),
        Streamed("MamaFama", StreamingKind.Recursive),
        Streamed("Obv", StreamingKind.Cumulative),
        Streamed("ParabolicSar", StreamingKind.StateMachine),
        Streamed("PermutationEntropy", StreamingKind.Window),
        Streamed("ProfitLoss", StreamingKind.Window),
        Streamed("RelativeVigorIndex", StreamingKind.Window),
        Streamed("Roc", StreamingKind.Window),
        Streamed("RollSpread", StreamingKind.Window),
        Streamed("RoofingFilter", StreamingKind.Recursive),
        Streamed("Rsi", StreamingKind.Recursive),
        Streamed("Sma", StreamingKind.Window),
        Streamed("SqueezeMomentum", StreamingKind.Window),
        Streamed("Stochastic", StreamingKind.Window),
        Streamed("Supertrend", StreamingKind.StateMachine),
        Streamed("TransferEntropy", StreamingKind.Window),
        Streamed("TurbulenceIndex", StreamingKind.Multivariate),
        Streamed("TwiggsMoneyFlow", StreamingKind.Recursive),
        Streamed("VolumeIndicator", StreamingKind.Window),
        Not("VolumeProfile", "one histogram of the whole range whose edges move with every new extreme, re-binning all earlier bars; there is no value per bar for a stream to say"),
        Streamed("Vpin", StreamingKind.Window),
        Streamed("Vwap", StreamingKind.Cumulative),
        Streamed("VwapZScore", StreamingKind.Window),
        Streamed("WaveletEnergyRatio", StreamingKind.Window),
        Streamed("WaveletEntropy", StreamingKind.Window),
        Streamed("WilliamsR", StreamingKind.Window),
        Streamed("YangZhang", StreamingKind.Window),
        Streamed("YangZhangVolRatio", StreamingKind.Window),
    ];

    /// <summary>The line of an indicator.</summary>
    public static StreamingClass Of(string name) => All.Single(c => c.Name == name);
}
