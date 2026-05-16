// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Relative Rotation Graph (Julius de Kempenaer, 2004–2005): 2D scatter of
/// (RS-Ratio, RS-Momentum) per asset relative to a benchmark, with a fading
/// <see cref="TailLength"/>-period trail per asset and a 100/100 quadrant grid.</summary>
/// <remarks>The proprietary JdK formula is not public; this series ships the de-facto
/// open-source reconstruction. Default formula is <see cref="RrgFormula.DualEma"/> —
/// canonical JdK behaviour, no mean-reversion assumption, suitable for trending assets.
/// Opt-in formulas (<see cref="RrgFormula.ZScore"/>, <see cref="RrgFormula.LogReturn"/>)
/// are available via <see cref="Formula"/>.</remarks>
public sealed class RelativeRotationSeries : ChartSeries, IColormappable
{
    /// <summary>Per-asset close price series. Length must equal <see cref="AssetLabels"/>.</summary>
    public IReadOnlyList<double[]> AssetCloses { get; }

    /// <summary>Benchmark close price series. Length must equal every element of <see cref="AssetCloses"/>.</summary>
    public IReadOnlyList<double> BenchmarkCloses { get; }

    /// <summary>Asset labels (ticker symbols or display names). Length must equal <see cref="AssetCloses"/>.</summary>
    public IReadOnlyList<string> AssetLabels { get; }

    /// <summary>RS-Ratio / RS-Momentum calculation model. Default <see cref="RrgFormula.DualEma"/>.</summary>
    public RrgFormula Formula { get; set; } = RrgFormula.DualEma;

    /// <summary>Short EMA period (DualEma) or rolling-window width (ZScore/LogReturn). Default <c>10</c>.</summary>
    public int ShortPeriod { get; set; } = 10;

    /// <summary>Long EMA period (DualEma) or log-return lookback (LogReturn). Default <c>26</c>.</summary>
    public int LongPeriod { get; set; } = 26;

    /// <summary>ROC lookback for the ZScore momentum pipeline. Default <c>10</c>.</summary>
    public int MomentumLookback { get; set; } = 10;

    /// <summary>Number of most-recent periods drawn per asset as a fading polyline.
    /// Alpha ramps from 0.2 (oldest) to 1.0 (head). Default <c>8</c>.</summary>
    public int TailLength { get; set; } = 8;

    /// <summary>Draws the 100/100 crosshair and four faint quadrant fills when true. Default <see langword="true"/>.</summary>
    public bool ShowQuadrantGrid { get; set; } = true;

    /// <inheritdoc cref="IColormappable.ColorMap"/>
    /// <remarks>Maps asset index (in [0, 1]) through the colour map. Defaults to Tab10 at render time when null.</remarks>
    public IColorMap? ColorMap { get; set; }

    /// <summary>Per-bar absorption ratio [0..1], same length as the close-price series.
    /// When set, each trail dot is filled through a green→red diverging colormap instead of the uniform
    /// asset colour; the asset colour becomes the edge ring. Null means uniform fill (default).</summary>
    public double[]? AbsorptionRatioPerBar { get; set; }

    /// <summary>Per-bar Effective Number of Bets (ENB), same length as the close-price series.
    /// When set, each trail dot's radius scales with the ENB value (larger = more diversified portfolio).
    /// Null means fixed head radius (default).</summary>
    public double[]? EnbPerBar { get; set; }

    /// <summary>Initializes a new <see cref="RelativeRotationSeries"/>.</summary>
    /// <param name="assetCloses">Per-asset close price arrays. May be empty (renders blank figure).</param>
    /// <param name="benchmarkCloses">Benchmark close price series.</param>
    /// <param name="assetLabels">Display labels — one per asset.</param>
    /// <exception cref="ArgumentException">When asset and label counts differ, or any asset length differs from benchmark.</exception>
    public RelativeRotationSeries(double[][] assetCloses, double[] benchmarkCloses, string[] assetLabels)
    {
        if (assetCloses.Length != assetLabels.Length)
            throw new ArgumentException(
                $"Asset count ({assetCloses.Length}) must equal label count ({assetLabels.Length}).",
                nameof(assetLabels));

        for (int a = 0; a < assetCloses.Length; a++)
        {
            if (assetCloses[a].Length != benchmarkCloses.Length)
                throw new ArgumentException(
                    $"Asset[{a}] length ({assetCloses[a].Length}) must equal benchmark length ({benchmarkCloses.Length}).",
                    nameof(assetCloses));
        }

        AssetCloses      = assetCloses;
        BenchmarkCloses  = benchmarkCloses;
        AssetLabels      = assetLabels;
    }

    // ── Compute pipeline ──────────────────────────────────────────────────────

    /// <summary>Computes RS-Ratio and RS-Momentum for each asset using the current <see cref="Formula"/>.
    /// Both output arrays are the same length as the input close series; leading values are
    /// <see cref="double.NaN"/> where the lookback windows are not yet full.</summary>
    public RrsPoint[] ComputeRsData()
    {
        var results = new RrsPoint[AssetCloses.Count];
        var bench = (double[])BenchmarkCloses is double[] ba
            ? ba
            : BenchmarkCloses.ToArray();

        for (int a = 0; a < AssetCloses.Count; a++)
        {
            var (ratio, momentum) = Formula switch
            {
                RrgFormula.ZScore    => ComputeZScore(AssetCloses[a], bench, ShortPeriod, MomentumLookback),
                RrgFormula.LogReturn => ComputeLogReturn(AssetCloses[a], bench, ShortPeriod, LongPeriod),
                _                   => ComputeDualEma(AssetCloses[a], bench, ShortPeriod, LongPeriod),
            };
            results[a] = new RrsPoint(ratio, momentum);
        }
        return results;
    }

    private static double[] BuildRs(double[] asset, double[] bench)
    {
        int n = asset.Length;
        var rs = new double[n];
        for (int t = 0; t < n; t++)
            rs[t] = bench[t] == 0.0 ? double.NaN : asset[t] / bench[t] * 100.0;
        return rs;
    }

    private static double[] NaNArray(int n)
    {
        var a = new double[n];
        Array.Fill(a, double.NaN);
        return a;
    }

    private static (double[], double[]) ComputeDualEma(double[] asset, double[] bench, int shortP, int longP)
    {
        int n = asset.Length;
        var rs = BuildRs(asset, bench);

        double[] emaShortRs = new Ema(rs, shortP).Compute();
        double[] emaLongRs  = new Ema(rs, longP).Compute();

        var rsRatio = NaNArray(n);
        if (emaShortRs.Length == n && emaLongRs.Length == n)
        {
            for (int t = 0; t < n; t++)
            {
                if (!double.IsNaN(emaShortRs[t]) && !double.IsNaN(emaLongRs[t]) && emaLongRs[t] != 0.0)
                    rsRatio[t] = emaShortRs[t] / emaLongRs[t] * 100.0;
            }
        }

        // Momentum: apply DualEma to the valid suffix of rsRatio.
        int validStart = IndexOfFirstValid(rsRatio);
        if (validStart < 0) return (rsRatio, NaNArray(n));

        var rsRatioValid = rsRatio[validStart..];
        double[] emaShortMom = new Ema(rsRatioValid, shortP).Compute();
        double[] emaLongMom  = new Ema(rsRatioValid, longP).Compute();

        var rsMomentum = NaNArray(n);
        if (emaShortMom.Length == rsRatioValid.Length && emaLongMom.Length == rsRatioValid.Length)
        {
            for (int i = 0; i < rsRatioValid.Length; i++)
            {
                if (!double.IsNaN(emaShortMom[i]) && !double.IsNaN(emaLongMom[i]) && emaLongMom[i] != 0.0)
                    rsMomentum[validStart + i] = emaShortMom[i] / emaLongMom[i] * 100.0;
            }
        }

        return (rsRatio, rsMomentum);
    }

    private static (double[], double[]) ComputeZScore(double[] asset, double[] bench, int window, int rocLookback)
    {
        int n = asset.Length;
        var rs = BuildRs(asset, bench);

        // RS-Ratio: z-score of RS with 100 baseline.
        var rsRatio = ZScoreNormalize(rs, window);

        // RS-Momentum: ROC of RS then z-score.
        double[] rocShort = new Roc(rs, rocLookback).Compute();
        if (rocShort.Length == 0) return (rsRatio, NaNArray(n));

        var rocZScore = ZScoreNormalize(rocShort, window);
        var rsMomentum = NaNArray(n);
        Array.Copy(rocZScore, 0, rsMomentum, rocLookback, rocZScore.Length);

        return (rsRatio, rsMomentum);
    }

    private static (double[], double[]) ComputeLogReturn(double[] asset, double[] bench, int shortP, int longP)
    {
        int n = asset.Length;
        var rs = BuildRs(asset, bench);

        // RS-Ratio: same z-score as ZScore formula.
        var rsRatio = ZScoreNormalize(rs, shortP);

        // RS-Momentum: ln(1+r_long) - ln(1+r_short), then z-score.
        double[] rocLongArr  = new Roc(rs, longP).Compute();
        double[] rocShortArr = new Roc(rs, shortP).Compute();

        if (rocLongArr.Length == 0) return (rsRatio, NaNArray(n));

        // Both roc arrays are shorter; overlap starts at index longP in the original.
        int overlapLen = rocLongArr.Length; // n - longP
        var logReturns = new double[overlapLen];
        int shortOffset = longP - shortP; // how far into rocShortArr corresponds to index longP
        for (int i = 0; i < overlapLen; i++)
        {
            double rLong  = rocLongArr[i];
            double rShort = rocShortArr[i + shortOffset];
            logReturns[i] = Math.Log(1.0 + rLong + 1e-10) - Math.Log(1.0 + rShort + 1e-10);
        }

        var logRetZScore = ZScoreNormalize(logReturns, shortP);
        var rsMomentum = NaNArray(n);
        Array.Copy(logRetZScore, 0, rsMomentum, longP, logRetZScore.Length);

        return (rsRatio, rsMomentum);
    }

    // Z-score normalization: 100 + (src - SMA_w) / StdDev_w.
    // Returns full-length array with NaN for first (period-1) elements.
    private static double[] ZScoreNormalize(double[] src, int period)
    {
        int n = src.Length;
        if (n < period)
        {
            var all = NaNArray(n);
            return all;
        }

        var means   = VectorMath.RollingMean(src, period);     // length n - period + 1
        var stddevs = new double[means.Length];
        VectorMath.RollingStdDev(src, period, means, stddevs);

        var result = NaNArray(n);
        int offset = period - 1;
        for (int i = 0; i < means.Length; i++)
        {
            int t = i + offset;
            result[t] = stddevs[i] < 1e-12
                ? 100.0
                : 100.0 + (src[t] - means[i]) / stddevs[i];
        }
        return result;
    }

    private static int IndexOfFirstValid(double[] arr)
    {
        for (int i = 0; i < arr.Length; i++)
            if (!double.IsNaN(arr[i])) return i;
        return -1;
    }

    // ── ChartSeries overrides ─────────────────────────────────────────────────

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (AssetCloses.Count == 0) return new(null, null, null, null);

        var rsData = ComputeRsData();
        double xMin = double.MaxValue, xMax = double.MinValue;
        double yMin = double.MaxValue, yMax = double.MinValue;
        bool anyValid = false;

        foreach (var (rsRatio, rsMom) in rsData)
        {
            // Sample the last TailLength valid points per asset.
            int found = 0;
            for (int t = rsRatio.Length - 1; t >= 0 && found < TailLength; t--)
            {
                if (double.IsNaN(rsRatio[t]) || double.IsNaN(rsMom[t])) continue;
                xMin = Math.Min(xMin, rsRatio[t]); xMax = Math.Max(xMax, rsRatio[t]);
                yMin = Math.Min(yMin, rsMom[t]);   yMax = Math.Max(yMax, rsMom[t]);
                anyValid = true;
                found++;
            }
        }

        if (!anyValid) return new(null, null, null, null);
        if (xMin == xMax) { xMin -= 0.5; xMax += 0.5; }
        if (yMin == yMax) { yMin -= 0.5; yMax += 0.5; }
        return new(xMin, xMax, yMin, yMax);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type                 = "relativerotation",
        RrgAssetCloses       = AssetCloses.Select(a => a.ToList()).ToList(),
        RrgBenchmarkCloses   = BenchmarkCloses.ToList(),
        RrgAssetLabels       = AssetLabels.ToArray(),
        ColorMapName         = ColorMap?.Name,
        RrgFormula            = Formula != RrgFormula.DualEma   ? Formula.ToString() : null,
        RrgShortPeriod        = ShortPeriod != 10               ? ShortPeriod        : null,
        RrgLongPeriod         = LongPeriod  != 26               ? LongPeriod         : null,
        RrgMomentumLookback   = MomentumLookback != 10          ? MomentumLookback   : null,
        RrgTailLength         = TailLength   != 8               ? TailLength         : null,
        RrgShowQuadrantGrid   = !ShowQuadrantGrid               ? false              : null,
        RrgAbsorptionPerBar   = AbsorptionRatioPerBar?.ToList(),
        RrgEnbPerBar          = EnbPerBar?.ToList(),
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
