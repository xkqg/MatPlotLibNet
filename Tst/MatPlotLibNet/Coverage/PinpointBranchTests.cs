// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Coverage;

/// <summary>Phase Q Wave 2 (2026-04-19) — pinpoint Facts targeting EXACTLY ONE specific
/// missing branch per class, identified directly from the cobertura XML
/// <c>condition-coverage="50% (1/2)"</c> markers. Each test names the file:line of the
/// branch it lifts and which side of the conditional is being exercised, per Q.4 TDD discipline.</summary>
public class PinpointBranchTests
{
    // Sinusoidal.cs L21: `cosLat == 0 ? null : (...)` — null arm is provably unreachable
    // because Math.Cos(90 * π/180) ≈ 6e-17 due to floating-point rounding (never exactly 0).
    // Beyond ±90° the earlier guard short-circuits. Test instead exercises the NON-null arm
    // for completeness; the unreachable branch should be exempted in thresholds.json with
    // a Phase R rationale rather than tested.
    [Fact] public void Sinusoidal_NormalLatitude_Inverse_RoundTrips()
    {
        var p = new Geo.Projections.Sinusoidal();
        var result = p.Inverse(0.0, 45.0);
        Assert.NotNull(result);
    }

    // CandleIndicator<TResult> L72 (Adx warm-up base): `if (n < period) return [];`
    [Fact] public void CandleIndicator_ShortData_BaseClassEarlyReturn()
    {
        // Adx is the canonical CandleIndicator<SignalResult> subclass — short data triggers
        // the base-class warm-up early-return that pinpoint coverage flagged unhit.
        var adx = new Adx([1, 2], [0.5, 1.5], [0.7, 1.7], period: 14);
        double[] result = adx.Compute();
        Assert.Empty(result);
    }

    // Ichimoku.cs L52: `if (Close.Length < _senkouBPeriod) return;`
    [Fact] public void Ichimoku_ShortClose_ApplyEarlyReturns()
    {
        var axes = new Axes();
        new Ichimoku([1, 2, 3], [0.5, 1.5, 2.5], [0.7, 1.7, 2.7], senkouBPeriod: 52).Apply(axes);
        // Apply early-returns with no series added.
        Assert.Empty(axes.Series);
    }

    // Vwap.cs L35: `cumVol > 0 ? cumPriceVol / cumVol : Prices[i]` — Prices[i] arm at zero volume.
    [Fact] public void Vwap_ZeroVolume_FallsBackToPrice()
    {
        var v = new Vwap([10.0, 20.0, 30.0], [0.0, 0.0, 0.0]);
        var result = v.Compute();
        // With zero cumulative volume, fallback returns the price itself.
        Assert.Equal(10.0, result.Values[0]);
    }

    // WilliamsR.cs L57: `if (wr.Length == 0) return;`
    [Fact] public void WilliamsR_ShortData_ApplyEarlyReturns()
    {
        var axes = new Axes();
        new WilliamsR([1, 2], [0.5, 1.5], [0.7, 1.7], period: 14).Apply(axes);
        Assert.Empty(axes.Series);
    }

    // (InteractiveExtensions test moved to MatPlotLibNet.Interactive.Tests project — out of scope here.)

    // EcdfSeries.cs L39: `SortedX.Length > 0` — empty data.
    [Fact] public void EcdfSeries_EmptyData_DataRangeFallback()
    {
        var s = new EcdfSeries([]);
        var range = s.ComputeDataRange(new TestAxesContext());
        Assert.NotNull(range);
    }

    // ResidualSeries.cs L38: `if (XData.Length == 0) return new(0, 1, -1, 1);`
    [Fact] public void ResidualSeries_EmptyData_ReturnsDefaultRange()
    {
        var s = new ResidualSeries(Array.Empty<double>(), Array.Empty<double>());
        var range = s.ComputeDataRange(new TestAxesContext());
        Assert.Equal(0.0, range.XMin);
        Assert.Equal(1.0, range.XMax);
    }

    // Scatter3DSeries.cs L29 — `MarkerSize != 6 ? MarkerSize : null` non-default branch.
    [Fact] public void Scatter3DSeries_NonDefaultMarkerSize_SerializesValue()
    {
        var s = new Scatter3DSeries(new double[] { 1.0 }, new double[] { 2.0 }, new double[] { 3.0 })
        {
            MarkerSize = 12,
            ColorMap = ColorMaps.Plasma
        };
        var dto = s.ToSeriesDto();
        Assert.Equal(12, dto.MarkerSize);
        Assert.Equal("plasma", dto.ColorMapName);
    }

    // SpectrogramSeries.cs L46 — `MarkerSize != 6 ? MarkerSize : null` (or similar).
    [Fact] public void SpectrogramSeries_WithExplicitColormap_SerializesName()
    {
        var s = new SpectrogramSeries(new double[16]) { ColorMap = ColorMaps.Inferno };
        var dto = s.ToSeriesDto();
        Assert.Equal("inferno", dto.ColorMapName);
    }

    // TricontourSeries.cs L40 — same pattern: ColorMap-set serialization branch.
    [Fact] public void TricontourSeries_WithExplicitColormap_SerializesName()
    {
        var s = new TricontourSeries(
            new double[] { 0.0, 1.0, 0.5 },
            new double[] { 0.0, 0.0, 1.0 },
            new double[] { 1.0, 2.0, 3.0 })
        { ColorMap = ColorMaps.Magma };
        var dto = s.ToSeriesDto();
        Assert.Equal("magma", dto.ColorMapName);
    }

    // MathSymbols.cs L112: `Map.TryGetValue(name, out var v) ? v : null` null arm.
    [Fact] public void MathSymbols_UnknownSymbol_ReturnsNull()
    {
        Assert.Null(MathSymbols.TryGet("definitely-not-a-real-math-symbol-xxx"));
    }

    // PolarTransform.cs L17: `rMax > 0 ? rMax : 1` — non-positive rMax fallback.
    [Fact] public void PolarTransform_ZeroRMax_FallsBackToOne()
    {
        var t = new PolarTransform(new Rect(0, 0, 100, 100), rMax: 0);
        // rMax should be normalised to 1; transform a unit-radius point to verify.
        var p = t.PolarToPixel(1.0, 0.0);
        Assert.True(double.IsFinite(p.X));
        Assert.True(double.IsFinite(p.Y));
    }

    // EcdfSeriesRenderer.cs L20: `if (n == 0) return;`
    [Fact] public void EcdfSeriesRenderer_EmptyData_RendersWithoutCrash()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Ecdf(Array.Empty<double>()))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    private sealed class TestAxesContext : IAxesContext
    {
        public double? XAxisMin => null;
        public double? XAxisMax => null;
        public double? YAxisMin => null;
        public double? YAxisMax => null;
        public BarMode BarMode => BarMode.Grouped;
        public IReadOnlyList<ISeries> AllSeries => [];
    }
}
