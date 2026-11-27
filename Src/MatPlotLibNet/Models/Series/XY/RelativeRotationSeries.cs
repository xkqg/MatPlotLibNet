// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Relative Rotation Graph (Julius de Kempenaer, 2004–2005): 2D scatter of
/// (RS-Ratio, RS-Momentum) per asset relative to a benchmark, with a fading
/// <see cref="TailLength"/>-period trail per asset and a 100/100 quadrant grid.</summary>
/// <remarks>The proprietary JdK formula is not public; this series ships the de-facto
/// open-source reconstruction. Default formula is <see cref="RestingFormula"/> —
/// canonical JdK behaviour, no mean-reversion assumption, suitable for trending assets.
/// Opt-in formulas (<see cref="RrgFormula.ZScore"/>, <see cref="RrgFormula.LogReturn"/>)
/// are available via <see cref="Formula"/>.</remarks>
public sealed class RelativeRotationSeries : ChartSeries, IColormappable, IKeepsDataCopy, ISeriesKind<RelativeRotationSeries>, IGivenTextSize
{
    /// <inheritdoc />
    /// <remarks>2: each asset's trail is a line, a <c>Line2D</c>.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Line;

    /// <summary>The value both RS-Ratio and RS-Momentum are centred on: 100 means "as strong as the benchmark" and "no change in
    /// strength". The quadrant grid is drawn around it, the formulas are scaled to it, and the range of the axes holds it, so
    /// there is one 100 and the three cannot disagree.</summary>
    internal const double Centre = 100;

    /// <summary>Per-asset close price series. Length must equal <see cref="AssetLabels"/>.</summary>
    public IReadOnlyList<double[]> AssetCloses { get; }

    /// <summary>Benchmark close price series. Length must equal every element of <see cref="AssetCloses"/>.</summary>
    public IReadOnlyList<double> BenchmarkCloses { get; }

    /// <summary>Asset labels (ticker symbols or display names). Length must equal <see cref="AssetCloses"/>.</summary>
    public IReadOnlyList<string> AssetLabels { get; }

    private const RrgFormula RestingFormula = RrgFormula.DualEma;
    /// <summary>RS-Ratio / RS-Momentum calculation model. Default <see cref="RestingFormula"/>.</summary>
    public RrgFormula Formula { get; set => Change(ref field, value); } = RestingFormula;

    private const int RestingShortPeriod = 10;
    /// <summary>Short EMA period (DualEma) or rolling-window width (ZScore/LogReturn), in bars. Default <see cref="RestingShortPeriod"/>.</summary>
    /// <exception cref="ArgumentException">The period is below one bar; the period the series had is kept.</exception>
    public int ShortPeriod { get; set => Change(ref field, value.ThrowIfBelow(1, nameof(ShortPeriod))); } = RestingShortPeriod;

    private const int RestingLongPeriod = 26;
    /// <summary>Long EMA period (DualEma) or log-return lookback (LogReturn), in bars. Default <see cref="RestingLongPeriod"/>.</summary>
    /// <exception cref="ArgumentException">The period is below one bar; the period the series had is kept.</exception>
    public int LongPeriod { get; set => Change(ref field, value.ThrowIfBelow(1, nameof(LongPeriod))); } = RestingLongPeriod;

    private const int RestingMomentumLookback = 10;
    /// <summary>ROC lookback for the ZScore momentum pipeline, in bars. Default <see cref="RestingMomentumLookback"/>.</summary>
    /// <exception cref="ArgumentException">The lookback is below one bar; the lookback the series had is kept.</exception>
    public int MomentumLookback { get; set => Change(ref field, value.ThrowIfBelow(1, nameof(MomentumLookback))); } = RestingMomentumLookback;

    private const int RestingTailLength = 8;
    /// <summary>Number of most-recent periods drawn per asset as a fading polyline.
    /// Alpha ramps from 0.2 (oldest) to 1.0 (head). Default <see cref="RestingTailLength"/>.</summary>
    /// <exception cref="ArgumentException">The tail is below one period (a tail holds at least its head); the length the series had is kept.</exception>
    public int TailLength { get; set => Change(ref field, value.ThrowIfBelow(1, nameof(TailLength))); } = RestingTailLength;

    private const bool RestingShowQuadrantGrid = true;
    /// <summary>Draws the 100/100 crosshair and four faint quadrant fills when true, and keeps the centre inside the range of the
    /// axes so they have an area to stand on. Default <see cref="RestingShowQuadrantGrid"/>.</summary>
    public bool ShowQuadrantGrid { get; set => Change(ref field, value); } = RestingShowQuadrantGrid;

    /// <inheritdoc cref="IColormappable.ColorMap"/>
    /// <remarks>Maps asset index (in [0, 1]) through the colour map. Defaults to Tab10 at render time when null.</remarks>
    public IColorMap? ColorMap { get; set => Change(ref field, value); }

    /// <summary>Per-bar absorption ratio [0..1], same length as the close-price series.
    /// When set, each trail dot is filled through a green→red diverging colormap instead of the uniform
    /// asset colour; the asset colour becomes the edge ring. Null means uniform fill (default).</summary>
    /// <exception cref="ArgumentException">The array does not hold one ratio for each close; the ratios the series had are kept.</exception>
    public double[]? AbsorptionRatioPerBar { get; set => Change(ref field, value.ThrowIfNotOnePer(BenchmarkCloses.Count, nameof(AbsorptionRatioPerBar))); }

    /// <summary>Per-bar Effective Number of Bets (ENB), same length as the close-price series.
    /// When set, each trail dot's radius scales with the ENB value (larger = more diversified portfolio).
    /// Null means fixed head radius (default).</summary>
    /// <exception cref="ArgumentException">The array does not hold one value for each close; the values the series had are kept.</exception>
    public double[]? EnbPerBar { get; set => Change(ref field, value.ThrowIfNotOnePer(BenchmarkCloses.Count, nameof(EnbPerBar))); }

    /// <summary>Initializes a new <see cref="RelativeRotationSeries"/>.</summary>
    /// <param name="assetCloses">Per-asset close price arrays. May be empty (renders blank figure).</param>
    /// <param name="benchmarkCloses">Benchmark close price series.</param>
    /// <param name="assetLabels">Display labels — one per asset.</param>
    /// <exception cref="ArgumentException">When asset and label counts differ, or any asset length differs from benchmark.</exception>
    public RelativeRotationSeries(double[][] assetCloses, double[] benchmarkCloses, string[] assetLabels)
    {
        assetLabels.Length.ThrowIfMisaligned(assetCloses.Length, nameof(assetLabels));
        assetCloses.Select(asset => asset.Length).ThrowIfNotAllEqual(nameof(assetCloses), "asset", benchmarkCloses.Length);

        AssetCloses      = assetCloses;
        BenchmarkCloses  = benchmarkCloses;
        AssetLabels      = assetLabels;
    }

    // ── Compute pipeline ──────────────────────────────────────────────────────

    /// <summary>Computes RS-Ratio and RS-Momentum for each asset using the current <see cref="Formula"/>.
    /// Both output arrays are the same length as the input close series; leading values are
    /// <see cref="double.NaN"/> where the lookback windows are not yet full.</summary>
    /// <returns>A new array, one point per asset, that the caller may keep and change.</returns>
    public RrsPoint[] ComputeRsData()
    {
        var results = new RrsPoint[AssetCloses.Count];
        // The constructor takes the benchmark as an array.
        var bench = (double[])BenchmarkCloses;
        for (int a = 0; a < AssetCloses.Count; a++)
        {
            results[a] = Formula switch
            {
                RrgFormula.ZScore    => ZScore(AssetCloses[a], bench, ShortPeriod, MomentumLookback),
                RrgFormula.LogReturn => LogReturn(AssetCloses[a], bench, ShortPeriod, LongPeriod),
                _                    => DualEma(AssetCloses[a], bench, ShortPeriod, LongPeriod),
            };
        }

        return results;
    }

    /// <summary>The relative strength, the asset over the benchmark times the centre; no value where the benchmark is zero.</summary>
    private static double[] Strength(double[] asset, double[] bench)
    {
        int n = asset.Length;
        var rs = new double[n];
        for (int t = 0; t < n; t++)
            rs[t] = bench[t] == 0.0 ? double.NaN : asset[t] / bench[t] * Centre;
        return rs;
    }

    private static double[] Missing(int n)
    {
        var a = new double[n];
        Array.Fill(a, double.NaN);
        return a;
    }

    /// <summary>RS-Ratio: the short EMA of the strength over its long EMA; RS-Momentum: the same over the RS-Ratio.</summary>
    private static RrsPoint DualEma(double[] asset, double[] bench, int shortP, int longP)
    {
        int n = asset.Length;
        var rs = Strength(asset, bench);

        double[] emaShortRs = new Ema(rs, shortP).Compute();
        double[] emaLongRs  = new Ema(rs, longP).Compute();

        var rsRatio = Missing(n);
        if (emaShortRs.Length == n && emaLongRs.Length == n)
        {
            for (int t = 0; t < n; t++)
            {
                if (!double.IsNaN(emaShortRs[t]) && !double.IsNaN(emaLongRs[t]) && emaLongRs[t] != 0.0)
                    rsRatio[t] = emaShortRs[t] / emaLongRs[t] * Centre;
            }
        }

        // Momentum: the same two averages over the part of the ratio that has values.
        int validStart = FirstValue(rsRatio);
        if (validStart < 0) return new(rsRatio, Missing(n));

        var rsRatioValid = rsRatio[validStart..];
        double[] emaShortMom = new Ema(rsRatioValid, shortP).Compute();
        double[] emaLongMom  = new Ema(rsRatioValid, longP).Compute();

        var rsMomentum = Missing(n);
        if (emaShortMom.Length == rsRatioValid.Length && emaLongMom.Length == rsRatioValid.Length)
        {
            for (int i = 0; i < rsRatioValid.Length; i++)
            {
                if (!double.IsNaN(emaShortMom[i]) && !double.IsNaN(emaLongMom[i]) && emaLongMom[i] != 0.0)
                    rsMomentum[validStart + i] = emaShortMom[i] / emaLongMom[i] * Centre;
            }
        }

        return new(rsRatio, rsMomentum);
    }

    /// <summary>RS-Ratio: the z-score of the strength; RS-Momentum: the z-score of its rate of change.</summary>
    private static RrsPoint ZScore(double[] asset, double[] bench, int window, int rocLookback)
    {
        int n = asset.Length;
        var rs = Strength(asset, bench);

        var rsRatio = ZScores(rs, window);

        double[] rocShort = new Roc(rs, rocLookback).Compute();
        if (rocShort.Length == 0) return new(rsRatio, Missing(n));

        var rocZScore = ZScores(rocShort, window);
        var rsMomentum = Missing(n);
        Array.Copy(rocZScore, 0, rsMomentum, rocLookback, rocZScore.Length);

        return new(rsRatio, rsMomentum);
    }

    /// <summary>RS-Ratio: the z-score of the strength; RS-Momentum: the z-score of ln(1 + long return) - ln(1 + short return).</summary>
    private static RrsPoint LogReturn(double[] asset, double[] bench, int shortP, int longP)
    {
        int n = asset.Length;
        var rs = Strength(asset, bench);

        var rsRatio = ZScores(rs, shortP);

        double[] rocLongArr  = new Roc(rs, longP).Compute();
        double[] rocShortArr = new Roc(rs, shortP).Compute();

        // Both rates of change are shorter than the closes; they overlap from the later of the two lookbacks on, whichever of the
        // two periods is the longer.
        int start = Math.Max(longP, shortP);
        int overlapLen = Math.Min(rocLongArr.Length, rocShortArr.Length);
        if (overlapLen == 0) return new(rsRatio, Missing(n));

        var logReturns = new double[overlapLen];
        int longOffset = start - longP, shortOffset = start - shortP;
        for (int i = 0; i < overlapLen; i++)
        {
            double rLong  = rocLongArr[i + longOffset];
            double rShort = rocShortArr[i + shortOffset];
            logReturns[i] = Math.Log(1.0 + rLong + 1e-10) - Math.Log(1.0 + rShort + 1e-10);
        }

        var logRetZScore = ZScores(logReturns, shortP);
        var rsMomentum = Missing(n);
        Array.Copy(logRetZScore, 0, rsMomentum, start, logRetZScore.Length);

        return new(rsRatio, rsMomentum);
    }

    /// <summary>The centre plus the z-score of each value over the <paramref name="period"/> values up to it (the centre itself
    /// where they do not vary); no value for the first <paramref name="period"/> - 1.</summary>
    private static double[] ZScores(double[] src, int period)
    {
        int n = src.Length;
        if (n < period) return Missing(n);

        var means   = src.RollingMean(period);
        var stddevs = new double[means.Length];
        src.RollingStdDev(period, means, stddevs);

        var result = Missing(n);
        int offset = period - 1;
        for (int i = 0; i < means.Length; i++)
        {
            int t = i + offset;
            result[t] = stddevs[i] < 1e-12
                ? Centre
                : Centre + (src[t] - means[i]) / stddevs[i];
        }

        return result;
    }

    private static int FirstValue(double[] arr)
    {
        for (int i = 0; i < arr.Length; i++)
            if (!double.IsNaN(arr[i])) return i;
        return -1;
    }

    /// <summary>The rotation the last draw read, with what it was computed from.</summary>
    [RenderCache]
    private KeptEstimate<RrsPoint[]>? _rotation;

    [RenderCache]
    private DataSnapshot? _dataCopy;

    ref DataSnapshot? IKeepsDataCopy.DataCopy => ref _dataCopy;

    /// <summary>The RS-Ratio and RS-Momentum of every asset as the range of the axes, the renderer and the data table read them:
    /// computed once and kept until a setting or a close changes (<see cref="KeptEstimate{T}"/>), so a draw computes it at
    /// most once and a figure drawn again with the same data not at all. It is shared, so nothing may write into it; a caller
    /// that wants its own copy asks <see cref="ComputeRsData"/>.</summary>
    /// <returns>One point per asset.</returns>
    internal RrsPoint[] Rotation() => KeptEstimate<RrsPoint[]>.Of(ref _rotation, this, static series => series.ComputeRsData());

    // ── ChartSeries overrides ─────────────────────────────────────────────────

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (AssetCloses.Count == 0) return default;

        var rsData = Rotation();
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

        if (!anyValid) return default;

        // The quadrants and the crosshair are drawn around the centre, so the plot holds it, even when every asset stands on
        // one side of it (all leading, all lagging): the empty space is what shows which quadrant they are in, as StockCharts'
        // chart does. Without the grid nothing is drawn there, and the range is the data's alone.
        if (ShowQuadrantGrid)
        {
            xMin = Math.Min(xMin, Centre); xMax = Math.Max(xMax, Centre);
            yMin = Math.Min(yMin, Centre); yMax = Math.Max(yMax, Centre);
        }

        return DataRangeContribution.Opened(new(xMin, xMax), new(yMin, yMax));
    }

    static string ISeriesKind<RelativeRotationSeries>.Kind => "relativerotation";

    /// <summary>The size of the name written beside the head of each tail, in pixels (the unit of <see cref="Font.Size"/>). Default <see cref="RestingFontSize"/>. Given no size, the name written beside the head of each tail
    /// is the size of the theme's text under the matplotlib themes, as matplotlib draws the text it writes beside its data
    /// (<c>font.size</c>), and <see cref="RestingFontSize"/> under every other theme; a size that is given, the default included, is drawn as given.</summary>
    public double FontSize { get => GivenFontSize ?? RestingFontSize; set => GivenFontSize = value; }

    /// <summary>The size a name takes under a theme that names none.</summary>
    private const double RestingFontSize = 9;

    /// <summary>The size that was given, <see langword="null"/> when none was.</summary>
    internal double? GivenFontSize { get; private set => Change(ref field, value); }

    double? IGivenTextSize.GivenTextSize => GivenFontSize;

    double IGivenTextSize.RestingTextSize => RestingFontSize;

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type                 = this.WireKind(),
        FontSize = GivenFontSize,
        RrgAssetCloses       = AssetCloses.Select(a => a.ToList()).ToList(),
        RrgBenchmarkCloses   = BenchmarkCloses.ToList(),
        RrgAssetLabels       = AssetLabels.ToArray(),
        ColorScale = this.ColorScaleOnTheWire(),
        RrgFormula            = Formula.OrNullAtRest(RestingFormula)?.ToString(),
        RrgShortPeriod        = ShortPeriod.OrNullAtRest(RestingShortPeriod),
        RrgLongPeriod         = LongPeriod.OrNullAtRest(RestingLongPeriod),
        RrgMomentumLookback   = MomentumLookback.OrNullAtRest(RestingMomentumLookback),
        RrgTailLength         = TailLength.OrNullAtRest(RestingTailLength),
        RrgShowQuadrantGrid   = ShowQuadrantGrid.OrNullAtRest(RestingShowQuadrantGrid),
        RrgAbsorptionPerBar   = AbsorptionRatioPerBar?.ToList(),
        RrgEnbPerBar          = EnbPerBar?.ToList(),
    };

    /// <summary>The reader of a <see cref="RelativeRotationSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static RelativeRotationSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var assetCloses    = (dto.RrgAssetCloses   ?? []).Select(a => a.ToArray()).ToArray();
        var benchmarkCloses = (dto.RrgBenchmarkCloses ?? []).ToArray();
        var labels         = dto.RrgAssetLabels ?? [];
        var s = axes.RelativeRotation(assetCloses, benchmarkCloses, labels);
        s.RestoreColorScale(dto);
        if (dto.RrgFormula.WireEnum<RrgFormula>() is { } rrgFormula) s.Formula = rrgFormula;
        if (dto.RrgShortPeriod is { } rrgShortPeriod) s.ShortPeriod = rrgShortPeriod;
        if (dto.RrgLongPeriod is { } rrgLongPeriod) s.LongPeriod = rrgLongPeriod;
        if (dto.RrgMomentumLookback is { } rrgMomentumLookback) s.MomentumLookback = rrgMomentumLookback;
        if (dto.RrgTailLength is { } rrgTailLength) s.TailLength = rrgTailLength;
        if (dto.RrgShowQuadrantGrid is { } rrgShowQuadrantGrid) s.ShowQuadrantGrid = rrgShowQuadrantGrid;
        if (dto.RrgAbsorptionPerBar is { } rrgAbsorptionPerBar) s.AbsorptionRatioPerBar = rrgAbsorptionPerBar.ToArray();
        if (dto.RrgEnbPerBar is { } rrgEnbPerBar) s.EnbPerBar = rrgEnbPerBar.ToArray();
        if (dto.FontSize is { } fontSize) s.FontSize = fontSize;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>The computed RS-ratio and RS-momentum per asset per period — what the quadrants show. The raw
    /// closes are the input; the coordinates are what the reader is looking at.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        // The constructor refuses a label count that differs from the asset count, so there is one label
        // per asset here by construction — no fallback to guard a case the type cannot be built in.
        var cells = Rotation().Select(point => Enumerable.Range(0, Math.Min(point.RsRatio.Length, point.RsMomentum.Length))
            .Select(i => new[] { DataCell.FromNumber(point.RsRatio[i]), DataCell.FromNumber(point.RsMomentum[i]) }).ToArray()).ToArray();
        return ChartDataTable.OfGroups([new("asset", DataColumnKind.Text), new("rs-ratio"), new("rs-momentum")], AssetLabels, "", cells, static row => row);
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
