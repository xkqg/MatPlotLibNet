// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet;

/// <summary>The indicator verbs of the axes builder that draw a panel oscillating around a level: momentum oscillators and the
/// cycle filters of Ehlers.</summary>
public static class AxesBuilderOscillatorVerbs
{
    extension(AxesBuilder axes)
    {
        /// <summary>Adds an RSI panel indicator.</summary>
        public AxesBuilder Rsi(double[] prices, int period = Indicators.Rsi.DefaultPeriod, Action<Indicators.Rsi>? configure = null)
            => axes.ApplyIndicator(new Indicators.Rsi(prices, period), configure);

        /// <summary>Adds a Williams %R panel indicator.</summary>
        public AxesBuilder WilliamsR(Indicators.OhlcvBars bars, int period = Indicators.WilliamsR.DefaultPeriod, Action<Indicators.WilliamsR>? configure = null)
            => axes.ApplyIndicator(new Indicators.WilliamsR(bars, period), configure);

        /// <summary>Adds a Commodity Channel Index panel indicator.</summary>
        public AxesBuilder Cci(Indicators.OhlcvBars bars, int period = Indicators.Cci.DefaultPeriod, Action<Indicators.Cci>? configure = null)
            => axes.ApplyIndicator(new Indicators.Cci(bars, period), configure);

        /// <summary>Adds a Kaufman Efficiency Ratio panel indicator. Output bounded to [0, 1].</summary>
        public AxesBuilder KaufmanEfficiencyRatio(double[] prices, int period = Indicators.KaufmanEfficiencyRatio.DefaultPeriod,
            Action<Indicators.KaufmanEfficiencyRatio>? configure = null)
            => axes.ApplyIndicator(new Indicators.KaufmanEfficiencyRatio(prices, period), configure);

        /// <summary>Adds a Laguerre RSI panel indicator (Ehlers 2004). Output bounded to [0, 1].</summary>
        public AxesBuilder LaguerreRsi(double[] prices, double alpha = Indicators.LaguerreRsi.DefaultAlpha,
            Action<Indicators.LaguerreRsi>? configure = null)
            => axes.ApplyIndicator(new Indicators.LaguerreRsi(prices, alpha), configure);

        /// <summary>Adds a Squeeze Momentum (LazyBear) panel indicator.</summary>
        public AxesBuilder SqueezeMomentum(Indicators.OhlcvBars bars,
            int period = Indicators.SqueezeMomentum.DefaultPeriod, double bbMult = Indicators.SqueezeMomentum.DefaultBbMult, double kcMult = Indicators.SqueezeMomentum.DefaultKcMult,
            Action<Indicators.SqueezeMomentum>? configure = null)
            => axes.ApplyIndicator(new Indicators.SqueezeMomentum(bars, period, bbMult, kcMult), configure);

        /// <summary>Adds an Ehlers Cyber Cycle panel indicator (Ehlers 2002).</summary>
        public AxesBuilder CyberCycle(double[] prices, double alpha = Indicators.CyberCycle.DefaultAlpha,
            Action<Indicators.CyberCycle>? configure = null)
            => axes.ApplyIndicator(new Indicators.CyberCycle(prices, alpha), configure);

        /// <summary>Adds an Ehlers Roofing Filter panel indicator (band-pass, Ehlers 2014).</summary>
        public AxesBuilder RoofingFilter(double[] prices, int hpPeriod = Indicators.RoofingFilter.DefaultHpPeriod, int lpPeriod = Indicators.RoofingFilter.DefaultLpPeriod,
            Action<Indicators.RoofingFilter>? configure = null)
            => axes.ApplyIndicator(new Indicators.RoofingFilter(prices, hpPeriod, lpPeriod), configure);

        /// <summary>Adds an Ehlers Sinewave indicator (Ehlers 2002) — two lines + cycle/trend flag.</summary>
        public AxesBuilder EhlersSineWave(double[] prices,
            Action<Indicators.EhlersSineWave>? configure = null)
            => axes.ApplyIndicator(new Indicators.EhlersSineWave(prices), configure);

        /// <summary>Adds an Ehlers Adaptive Stochastic panel indicator. Output in [0, 100].</summary>
        public AxesBuilder AdaptiveStochastic(Indicators.OhlcvBars bars,
            int smoothingPeriod = Indicators.AdaptiveStochastic.DefaultSmoothingPeriod,
            Action<Indicators.AdaptiveStochastic>? configure = null)
            => axes.ApplyIndicator(new Indicators.AdaptiveStochastic(bars, smoothingPeriod), configure);

        /// <summary>Adds an Aroon Oscillator panel indicator (Chande 1995). Output in [-100, 100].</summary>
        public AxesBuilder AroonOscillator(Indicators.OhlcvBars bars, int period = Indicators.AroonOscillator.DefaultPeriod,
            Action<Indicators.AroonOscillator>? configure = null)
            => axes.ApplyIndicator(new Indicators.AroonOscillator(bars, period), configure);

        /// <summary>Adds a Relative Vigor Index panel indicator (Ehlers 2002). Two lines (RVI + Signal).</summary>
        public AxesBuilder RelativeVigorIndex(Indicators.OhlcvBars bars,
            int period = Indicators.RelativeVigorIndex.DefaultPeriod,
            Action<Indicators.RelativeVigorIndex>? configure = null)
            => axes.ApplyIndicator(new Indicators.RelativeVigorIndex(bars, period), configure);

        /// <summary>Adds an Ehlers Center-of-Gravity Oscillator panel (Ehlers 2002) — linearly
        /// weighted price average centred on zero. Recent prices weigh more than older ones.</summary>
        public AxesBuilder CgOscillator(double[] prices, int period = Indicators.CgOscillator.DefaultPeriod,
            Action<Indicators.CgOscillator>? configure = null)
            => axes.ApplyIndicator(new Indicators.CgOscillator(prices, period), configure);

        /// <summary>Adds an Ehlers Inverse Fisher Transform panel (Ehlers 2004) — <c>tanh(scale·x)</c>
        /// squash that sharpens any bounded oscillator. Output in <c>[-1, +1]</c>. Apply to RSI,
        /// stochastic, CCI, etc. to produce cleaner crossover signals.</summary>
        public AxesBuilder InverseFisherTransform(double[] input, double scale = Indicators.InverseFisherTransform.DefaultScale,
            Action<Indicators.InverseFisherTransform>? configure = null)
            => axes.ApplyIndicator(new Indicators.InverseFisherTransform(input, scale), configure);

        /// <summary>Adds an Ehlers SuperSmoother panel (Ehlers 2013) — two-pole Butterworth
        /// low-pass. Applicable to any numerical series (price, indicator output, residuals).</summary>
        public AxesBuilder EhlersSuperSmoother(double[] input, int period = Indicators.EhlersSuperSmoother.DefaultPeriod,
            Action<Indicators.EhlersSuperSmoother>? configure = null)
            => axes.ApplyIndicator(new Indicators.EhlersSuperSmoother(input, period), configure);
    }
}
