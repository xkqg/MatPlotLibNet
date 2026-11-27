// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet;

/// <summary>The indicator verbs of the axes builder that draw a panel of the traded volume or of the cost of trading: money
/// flow, volume oscillators and liquidity measures.</summary>
public static class AxesBuilderVolumeIndicatorVerbs
{
    extension(AxesBuilder axes)
    {
        /// <summary>Adds an On-Balance Volume panel indicator.</summary>
        public AxesBuilder Obv(double[] close, double[] volume, Action<Indicators.Obv>? configure = null)
            => axes.ApplyIndicator(new Indicators.Obv(close, volume), configure);

        /// <summary>Adds an Amihud illiquidity panel indicator.</summary>
        public AxesBuilder AmihudIlliquidity(Indicators.OhlcvBars bars, int period = Indicators.AmihudIlliquidity.DefaultPeriod,
            Action<Indicators.AmihudIlliquidity>? configure = null)
            => axes.ApplyIndicator(new Indicators.AmihudIlliquidity(bars, period), configure);

        /// <summary>Adds a Corwin-Schultz bid-ask-spread panel indicator.</summary>
        public AxesBuilder CorwinSchultz(Indicators.OhlcvBars bars, int period = Indicators.CorwinSchultz.DefaultPeriod,
            Action<Indicators.CorwinSchultz>? configure = null)
            => axes.ApplyIndicator(new Indicators.CorwinSchultz(bars, period), configure);

        /// <summary>Adds a VPIN (Volume-Synchronized Probability of Informed Trading) panel indicator.
        /// Output bounded to [0, 1].</summary>
        public AxesBuilder Vpin(Indicators.OhlcvBars bars, int bucketPeriod = Indicators.Vpin.DefaultBucketPeriod, int sigmaPeriod = Indicators.Vpin.DefaultSigmaPeriod,
            Action<Indicators.Vpin>? configure = null)
            => axes.ApplyIndicator(new Indicators.Vpin(bars, bucketPeriod, sigmaPeriod), configure);

        /// <summary>Adds a Roll bid-ask-spread panel indicator.</summary>
        public AxesBuilder RollSpread(double[] prices, int period = Indicators.RollSpread.DefaultPeriod,
            Action<Indicators.RollSpread>? configure = null)
            => axes.ApplyIndicator(new Indicators.RollSpread(prices, period), configure);

        /// <summary>Adds an Elder Force Index panel indicator (Elder 1993). Signed momentum.</summary>
        public AxesBuilder ForceIndex(Indicators.OhlcvBars bars, int period = Indicators.ForceIndex.DefaultPeriod,
            Action<Indicators.ForceIndex>? configure = null)
            => axes.ApplyIndicator(new Indicators.ForceIndex(bars, period), configure);

        /// <summary>Adds an Ease of Movement panel indicator (Arms 1975). Signed — positive =
        /// price rises easily on light volume (accumulation), negative = easy decline.</summary>
        public AxesBuilder EaseOfMovement(Indicators.OhlcvBars bars,
            int period = Indicators.EaseOfMovement.DefaultPeriod, double scale = Indicators.EaseOfMovement.DefaultScale,
            Action<Indicators.EaseOfMovement>? configure = null)
            => axes.ApplyIndicator(new Indicators.EaseOfMovement(bars, period, scale), configure);

        /// <summary>Adds a Klinger Volume Oscillator panel indicator (Klinger 1977). Two lines —
        /// KVO (fast EMA − slow EMA of volume force) + Signal (EMA of KVO). Crossovers are the
        /// standard buy / sell trigger.</summary>
        public AxesBuilder KlingerVolumeOscillator(Indicators.OhlcvBars bars,
            int fastPeriod = Indicators.KlingerVolumeOscillator.DefaultFastPeriod, int slowPeriod = Indicators.KlingerVolumeOscillator.DefaultSlowPeriod, int signalPeriod = Indicators.KlingerVolumeOscillator.DefaultSignalPeriod,
            Action<Indicators.KlingerVolumeOscillator>? configure = null)
            => axes.ApplyIndicator(new Indicators.KlingerVolumeOscillator(bars, fastPeriod, slowPeriod, signalPeriod), configure);

        /// <summary>Adds a Twiggs Money Flow panel indicator (Twiggs 2002). Output bounded in
        /// <c>[-1, 1]</c>; positive = accumulation, negative = distribution.</summary>
        public AxesBuilder TwiggsMoneyFlow(Indicators.OhlcvBars bars,
            int period = Indicators.TwiggsMoneyFlow.DefaultPeriod,
            Action<Indicators.TwiggsMoneyFlow>? configure = null)
            => axes.ApplyIndicator(new Indicators.TwiggsMoneyFlow(bars, period), configure);

        /// <summary>Adds a VWAP Z-Score panel indicator — standardised deviation from the rolling
        /// volume-weighted average price. Mean-reversion signal; extremes beyond ±2σ mark
        /// dislocations from fair value.</summary>
        public AxesBuilder VwapZScore(Indicators.OhlcvBars bars, int window = Indicators.VwapZScore.DefaultWindow,
            Action<Indicators.VwapZScore>? configure = null)
            => axes.ApplyIndicator(new Indicators.VwapZScore(bars, window), configure);
    }
}
