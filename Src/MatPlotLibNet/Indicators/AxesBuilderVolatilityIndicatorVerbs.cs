// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet;

/// <summary>The indicator verbs of the axes builder that draw a panel of volatility, of a change of regime, or of the
/// information in a series: volatility estimators, change-point detectors, entropies and the transfer of information between
/// two series.</summary>
public static class AxesBuilderVolatilityIndicatorVerbs
{
    extension(AxesBuilder axes)
    {
        /// <summary>Adds a Garman-Klass volatility panel indicator.</summary>
        public AxesBuilder GarmanKlass(Indicators.OhlcvBars bars, int period = Indicators.GarmanKlass.DefaultPeriod, Action<Indicators.GarmanKlass>? configure = null)
            => axes.ApplyIndicator(new Indicators.GarmanKlass(bars, period), configure);

        /// <summary>Adds a Yang-Zhang volatility panel indicator.</summary>
        public AxesBuilder YangZhang(Indicators.OhlcvBars bars, int period = Indicators.YangZhang.DefaultPeriod, Action<Indicators.YangZhang>? configure = null)
            => axes.ApplyIndicator(new Indicators.YangZhang(bars, period), configure);

        /// <summary>Adds a CUSUM filter panel indicator (regime-break detector).</summary>
        public AxesBuilder Cusum(double[] prices, double threshold, double drift = Indicators.Cusum.DefaultDrift,
            Action<Indicators.Cusum>? configure = null)
            => axes.ApplyIndicator(new Indicators.Cusum(prices, threshold, drift), configure);

        /// <summary>Adds a fractional-differentiation panel indicator (Lopez de Prado §5.5).</summary>
        public AxesBuilder Ffd(double[] prices, double d = Indicators.FractionalDifferentiation.DefaultD, double tolerance = Indicators.FractionalDifferentiation.DefaultTolerance,
            Action<Indicators.FractionalDifferentiation>? configure = null)
            => axes.ApplyIndicator(new Indicators.FractionalDifferentiation(prices, d, tolerance), configure);

        /// <summary>Adds a BOCPD (Bayesian Online Changepoint Detection) panel indicator.
        /// Output is per-bar changepoint probability in [0, 1].</summary>
        public AxesBuilder Bocpd(double[] prices, double hazard = Indicators.Bocpd.DefaultHazard,
            double priorVariance = Indicators.Bocpd.DefaultPriorVariance, int maxRunLength = Indicators.Bocpd.DefaultMaxRunLength,
            Action<Indicators.Bocpd>? configure = null)
            => axes.ApplyIndicator(new Indicators.Bocpd(prices, hazard, priorVariance, maxRunLength), configure);

        /// <summary>Adds a Turbulence Index panel indicator (Kritzman &amp; Li 2010).
        /// Rolling Mahalanobis distance — crisis detector.</summary>
        public AxesBuilder TurbulenceIndex(double[][] features, int window = Indicators.TurbulenceIndex.DefaultWindow,
            double regularization = Indicators.TurbulenceIndex.DefaultRegularization,
            Action<Indicators.TurbulenceIndex>? configure = null)
            => axes.ApplyIndicator(new Indicators.TurbulenceIndex(features, window, regularization), configure);

        /// <summary>Adds a Dispersion Index panel indicator — population stddev across signals per bar.</summary>
        public AxesBuilder DispersionIndex(double[][] signals,
            Action<Indicators.DispersionIndex>? configure = null)
            => axes.ApplyIndicator(new Indicators.DispersionIndex(signals), configure);

        /// <summary>Adds a Permutation Entropy panel indicator (Bandt &amp; Pompe 2002). Output in [0, 1].</summary>
        public AxesBuilder PermutationEntropy(double[] prices, int order = Indicators.PermutationEntropy.DefaultOrder, int window = Indicators.PermutationEntropy.DefaultWindow,
            Action<Indicators.PermutationEntropy>? configure = null)
            => axes.ApplyIndicator(new Indicators.PermutationEntropy(prices, order, window), configure);

        /// <summary>Adds a Haar-DWT energy-ratio panel indicator. Output in [0, 1] — fraction of
        /// band energy at the target detail level.</summary>
        public AxesBuilder WaveletEnergyRatio(double[] prices, int window = Indicators.WaveletEnergyRatio.DefaultWindow, int level = Indicators.WaveletEnergyRatio.DefaultLevel,
            Action<Indicators.WaveletEnergyRatio>? configure = null)
            => axes.ApplyIndicator(new Indicators.WaveletEnergyRatio(prices, window, level), configure);

        /// <summary>Adds a Haar-DWT entropy panel indicator. Output in [0, 1] — Shannon entropy
        /// of the energy distribution over wavelet bands.</summary>
        public AxesBuilder WaveletEntropy(double[] prices, int window = Indicators.WaveletEntropy.DefaultWindow,
            Action<Indicators.WaveletEntropy>? configure = null)
            => axes.ApplyIndicator(new Indicators.WaveletEntropy(prices, window), configure);

        /// <summary>Adds a Yang-Zhang volatility ratio panel — <c>short-window YZ / long-window YZ</c>.
        /// Ratio &gt; 1 = vol expansion (possible breakout); &lt; 1 = consolidation. Reuses
        /// <see cref="Indicators.YangZhang"/> for both numerator and denominator.</summary>
        public AxesBuilder YangZhangVolRatio(Indicators.OhlcvBars bars,
            int shortWindow = Indicators.YangZhangVolRatio.DefaultShortWindow, int longWindow = Indicators.YangZhangVolRatio.DefaultLongWindow,
            Action<Indicators.YangZhangVolRatio>? configure = null)
            => axes.ApplyIndicator(new Indicators.YangZhangVolRatio(bars, shortWindow, longWindow), configure);

        /// <summary>Adds a Transfer Entropy computation (Schreiber 2000). Scalar output in nats —
        /// measures directional information flow from <paramref name="source"/> to
        /// <paramref name="target"/>. Unlike line-emitting indicators, <c>Apply</c> is a no-op;
        /// callers typically consume <c>Compute()</c> for display as an annotation or ML feature.</summary>
        public AxesBuilder TransferEntropy(double[] source, double[] target,
            int bins = Indicators.TransferEntropy.DefaultBins, int lag = Indicators.TransferEntropy.DefaultLag,
            Action<Indicators.TransferEntropy>? configure = null)
            => axes.ApplyIndicator(new Indicators.TransferEntropy(source, target, bins, lag), configure);

        /// <summary>Adds a Transfer Entropy computation as the options say (the default is the Kraskov nearest-neighbour estimate). With a
        /// <see cref="Indicators.TransferEntropyOptions.Window"/> it is a rolling entropy, one value for each bar, drawn as a line (NaN until the
        /// first window is full); without one it is a single number and nothing is drawn.</summary>
        /// <param name="source">The series whose influence is measured.</param>
        /// <param name="target">The series that is influenced; the same length as <paramref name="source"/>.</param>
        /// <param name="options">The estimator and its settings.</param>
        /// <param name="configure">Optional hook to style the indicator.</param>
        public AxesBuilder TransferEntropy(double[] source, double[] target, Indicators.TransferEntropyOptions options,
            Action<Indicators.TransferEntropy>? configure = null)
            => axes.ApplyIndicator(new Indicators.TransferEntropy(source, target, options), configure);
    }
}
