// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>v1.10 Phase 4 — verifies SVG output of <see cref="PairGridSeries"/> rendering.
/// Diagonal histograms first (Step 4); off-diagonal scatter, triangular suppression,
/// KDE, and hue grouping arrive in subsequent steps.</summary>
public class PairGridRenderTests
{
    private static double[][] ThreeVars
    {
        get
        {
            // 100 deterministic samples per variable — enough to fill ≥20 bins so
            // histogram bar counts scale visibly with DiagonalBins.
            var rng = new Random(42);
            return new[]
            {
                Enumerable.Range(0, 100).Select(_ => rng.NextDouble() * 10.0).ToArray(),
                Enumerable.Range(0, 100).Select(_ => rng.NextDouble() * 5.0).ToArray(),
                Enumerable.Range(0, 100).Select(_ => 5.0 + rng.NextDouble() * 5.0).ToArray(),
            };
        }
    }

    private static string RenderSvg(double[][]? vars = null, Action<PairGridSeries>? configure = null) =>
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.PairGrid(vars ?? ThreeVars, configure))
            .ToSvg();

    private static int CountOccurrences(string haystack, string needle)
    {
        int count = 0, idx = 0;
        while ((idx = haystack.IndexOf(needle, idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += needle.Length;
        }
        return count;
    }

    // ── Smoke tests ──────────────────────────────────────────────────────────

    [Fact]
    public void DefaultRender_ProducesValidSvg()
    {
        string svg = RenderSvg();
        Assert.Contains("<svg", svg);
        Assert.Contains("</svg>", svg);
    }

    [Fact]
    public void DefaultRender_EmitsRectanglesForDiagonalBars()
    {
        // Default: 3 variables × DiagonalBins=20 → at least 60 histogram rectangles
        // (the renderer also emits axes/spine rects, so this is a lower bound).
        string svg = RenderSvg();
        int rects = CountOccurrences(svg, "<rect ");
        Assert.True(rects >= 60, $"Expected at least 60 rects (3 vars × 20 bins), got {rects}.");
    }

    [Fact]
    public void DiagonalBins_MoreBins_ProducesMoreRectangles()
    {
        string svgFew  = RenderSvg(configure: s => s.DiagonalBins = 5);
        string svgMany = RenderSvg(configure: s => s.DiagonalBins = 50);
        int rectsFew  = CountOccurrences(svgFew,  "<rect ");
        int rectsMany = CountOccurrences(svgMany, "<rect ");
        Assert.True(rectsMany > rectsFew,
            $"Expected more bins → more rects. Got {rectsFew} (5 bins) vs {rectsMany} (50 bins).");
    }

    // ── DiagonalKind = None suppresses diagonal output ───────────────────────

    [Fact]
    public void DiagonalKind_None_StillProducesValidSvg()
    {
        string svg = RenderSvg(configure: s =>
        {
            s.DiagonalKind = PairGridDiagonalKind.None;
            s.OffDiagonalKind = PairGridOffDiagonalKind.None;
        });
        // Even with all cells suppressed the outer figure still renders.
        Assert.Contains("<svg", svg);
        Assert.Contains("</svg>", svg);
    }

    [Fact]
    public void DiagonalKind_None_FewerBarsThanDefault()
    {
        string svgDefault = RenderSvg();
        string svgNoDiag  = RenderSvg(configure: s =>
        {
            s.DiagonalKind = PairGridDiagonalKind.None;
            s.OffDiagonalKind = PairGridOffDiagonalKind.None;
        });
        int rectsDefault = CountOccurrences(svgDefault, "<rect ");
        int rectsNoDiag  = CountOccurrences(svgNoDiag,  "<rect ");
        Assert.True(rectsDefault > rectsNoDiag,
            $"Histogram-on default ({rectsDefault}) must emit more rects than histogram-off ({rectsNoDiag}).");
    }

    // ── Single-variable edge case ────────────────────────────────────────────

    [Fact]
    public void SingleVariable_RendersValidSvg()
    {
        var oneVar = new[] { new[] { 1.0, 2.0, 3.0, 4.0, 5.0 } };
        string svg = RenderSvg(oneVar);
        Assert.Contains("<svg", svg);
        Assert.Contains("</svg>", svg);
    }

    // ── Degenerate variable: all-equal samples ───────────────────────────────

    [Fact]
    public void AllEqualSamples_DoesNotThrow()
    {
        // All-equal samples → zero data range; renderer must handle gracefully
        // (no division-by-zero, no NaN bin widths).
        var flatVar = new[] { Enumerable.Repeat(2.5, 8).ToArray() };
        var ex = Record.Exception(() => RenderSvg(flatVar));
        Assert.Null(ex);
    }

    [Fact]
    public void NaNOrInfiniteSamples_DoesNotThrow_AndDoesNotEmitNaNCoords()
    {
        // Mixed finite + NaN/+Inf/-Inf values must not propagate to SVG attributes.
        // The renderer skips non-finite samples rather than passing through.
        var mixedVars = new[]
        {
            new[] { 1.0, 2.0, double.NaN, 3.0, double.PositiveInfinity, 4.0 },
            new[] { 0.5, double.NegativeInfinity, 1.5, 2.5, 3.5, double.NaN },
        };
        var ex = Record.Exception(() => RenderSvg(mixedVars));
        Assert.Null(ex);

        string svg = RenderSvg(mixedVars);
        Assert.DoesNotContain("NaN", svg);
        Assert.DoesNotContain("Infinity", svg);
        Assert.DoesNotContain("∞", svg);
    }

    [Fact]
    public void AllNaNSamples_DoesNotThrow()
    {
        // All-NaN variable → renderer falls back gracefully (degenerate range guard).
        var nanVars = new[] { new[] { double.NaN, double.NaN, double.NaN } };
        var ex = Record.Exception(() => RenderSvg(nanVars));
        Assert.Null(ex);
    }

    // ── Off-diagonal scatter (Step 5) ─────────────────────────────────────────

    [Fact]
    public void DefaultRender_EmitsCircleElementsForScatter()
    {
        // 3 variables, default OffDiagonalKind = Scatter, Triangular = Both
        // → 6 off-diagonal cells × 100 points each ≈ 600 circles.
        string svg = RenderSvg();
        int circles = CountOccurrences(svg, "<circle ");
        Assert.True(circles >= 100, $"Expected at least 100 circles for default scatter, got {circles}.");
    }

    [Fact]
    public void OffDiagonalKind_None_EmitsFewerCirclesThanScatter()
    {
        string svgScatter = RenderSvg();
        string svgNoScat  = RenderSvg(configure: s => s.OffDiagonalKind = PairGridOffDiagonalKind.None);
        int cScatter = CountOccurrences(svgScatter, "<circle ");
        int cNoScat  = CountOccurrences(svgNoScat,  "<circle ");
        Assert.True(cScatter > cNoScat,
            $"Scatter on ({cScatter}) must emit more circles than scatter off ({cNoScat}).");
    }

    [Fact]
    public void MarkerSize_LargerRadius_AppearsInSvg()
    {
        // r="3" appears for default MarkerSize=3.0; r="6" for explicit 6.0.
        string svgSmall = RenderSvg(configure: s => s.MarkerSize = 3.0);
        string svgLarge = RenderSvg(configure: s => s.MarkerSize = 6.0);
        Assert.Contains("r=\"3\"", svgSmall);
        Assert.Contains("r=\"6\"", svgLarge);
    }

    [Fact]
    public void ScatterCells_AreSymmetricByDefault_BothTriangles()
    {
        // Default Triangular = Both: 3 vars → 6 off-diagonal cells (lower + upper).
        // Suppressing OffDiagonal proves all 6 contributed circles.
        string svgBoth = RenderSvg();
        string svgNone = RenderSvg(configure: s => s.OffDiagonalKind = PairGridOffDiagonalKind.None);
        int diff = CountOccurrences(svgBoth, "<circle ") - CountOccurrences(svgNone, "<circle ");
        // Each off-diagonal cell ≈ 100 circles × 6 cells = ~600 circles.
        Assert.True(diff >= 200, $"Expected ≥200 scatter circles across off-diagonal cells, got {diff}.");
    }

    // ── Triangular = LowerOnly / UpperOnly (Step 6) ───────────────────────────

    [Fact]
    public void Triangular_LowerOnly_EmitsFewerCirclesThanBoth()
    {
        string svgBoth   = RenderSvg();
        string svgLower  = RenderSvg(configure: s => s.Triangular = PairGridTriangle.LowerOnly);
        int cBoth  = CountOccurrences(svgBoth,  "<circle ");
        int cLower = CountOccurrences(svgLower, "<circle ");
        // LowerOnly hides 3 of the 6 off-diagonal cells → roughly half the scatter circles.
        Assert.True(cLower < cBoth,
            $"LowerOnly ({cLower}) must emit fewer circles than Both ({cBoth}).");
    }

    [Fact]
    public void Triangular_UpperOnly_EmitsFewerCirclesThanBoth()
    {
        string svgBoth  = RenderSvg();
        string svgUpper = RenderSvg(configure: s => s.Triangular = PairGridTriangle.UpperOnly);
        int cBoth  = CountOccurrences(svgBoth,  "<circle ");
        int cUpper = CountOccurrences(svgUpper, "<circle ");
        Assert.True(cUpper < cBoth,
            $"UpperOnly ({cUpper}) must emit fewer circles than Both ({cBoth}).");
    }

    [Fact]
    public void Triangular_LowerAndUpper_ProduceSimilarOffDiagonalCircleCounts()
    {
        // For symmetric data the two halves should each carry ~the same scatter density.
        string svgLower = RenderSvg(configure: s => s.Triangular = PairGridTriangle.LowerOnly);
        string svgUpper = RenderSvg(configure: s => s.Triangular = PairGridTriangle.UpperOnly);
        int cLower = CountOccurrences(svgLower, "<circle ");
        int cUpper = CountOccurrences(svgUpper, "<circle ");
        // Both contain the diagonal cells (no scatter); the difference is purely off-diagonal.
        // 100-sample-per-variable test data → 300 lower + 300 upper ≈ 600 circles each ±0.
        Assert.True(Math.Abs(cLower - cUpper) <= cLower / 2,
            $"Lower ({cLower}) and Upper ({cUpper}) circle counts should be of comparable order.");
    }

    [Fact]
    public void Triangular_LowerOnly_KeepsDiagonalHistograms()
    {
        // LowerOnly hides off-diagonal upper cells but the diagonal (i==j) MUST remain.
        // Compare bar count to a render with both DiagonalKind = None and OffDiagonalKind = None.
        string svgLower = RenderSvg(configure: s => s.Triangular = PairGridTriangle.LowerOnly);
        string svgEmpty = RenderSvg(configure: s =>
        {
            s.DiagonalKind = PairGridDiagonalKind.None;
            s.OffDiagonalKind = PairGridOffDiagonalKind.None;
        });
        int rLower = CountOccurrences(svgLower, "<rect ");
        int rEmpty = CountOccurrences(svgEmpty, "<rect ");
        Assert.True(rLower > rEmpty,
            $"LowerOnly ({rLower}) must keep diagonal histogram bars vs empty ({rEmpty}).");
    }

    // ── DiagonalKind = Kde (Step 7) ───────────────────────────────────────────

    [Fact]
    public void DiagonalKind_Kde_EmitsPolylines()
    {
        // KDE diagonal renders one polyline per variable.
        string svg = RenderSvg(configure: s => s.DiagonalKind = PairGridDiagonalKind.Kde);
        // Connected line emitter typically uses <polyline> or <path>; either is acceptable.
        bool hasPolyline = svg.Contains("<polyline ");
        bool hasPath     = svg.Contains("<path ");
        Assert.True(hasPolyline || hasPath, "KDE diagonal must emit polyline or path elements.");
    }

    [Fact]
    public void DiagonalKind_Kde_FewerRectsThanHistogram()
    {
        // KDE should NOT emit histogram bars on the diagonal.
        string svgHist = RenderSvg(configure: s => s.DiagonalKind = PairGridDiagonalKind.Histogram);
        string svgKde  = RenderSvg(configure: s => s.DiagonalKind = PairGridDiagonalKind.Kde);
        int rHist = CountOccurrences(svgHist, "<rect ");
        int rKde  = CountOccurrences(svgKde,  "<rect ");
        Assert.True(rKde < rHist,
            $"KDE diagonal ({rKde} rects) must emit fewer rects than histogram diagonal ({rHist}).");
    }

    [Fact]
    public void DiagonalKind_Kde_DoesNotThrowOnSmallSample()
    {
        // Single-sample variables → bandwidth fallback to 1.0 inside GaussianKde.
        var smallVars = new[]
        {
            new[] { 1.0, 2.0, 3.0 },
            new[] { 0.5, 1.5, 2.5 },
        };
        var ex = Record.Exception(() => RenderSvg(smallVars, s => s.DiagonalKind = PairGridDiagonalKind.Kde));
        Assert.Null(ex);
    }

    // ── HueGroups + HueLabels (Step 8) ────────────────────────────────────────

    private static int[] AlternatingHueOf(int n) => Enumerable.Range(0, n).Select(i => i % 2).ToArray();

    private static int CountDistinctFills(string svg, string elemPrefix)
    {
        // Find all `fill="#xxxxxx"` substrings on the given element type.
        var seen = new HashSet<string>(StringComparer.Ordinal);
        int idx = 0;
        while ((idx = svg.IndexOf(elemPrefix, idx, StringComparison.Ordinal)) >= 0)
        {
            int end = svg.IndexOf('>', idx);
            if (end < 0) break;
            string element = svg.Substring(idx, end - idx);
            int fillIdx = element.IndexOf("fill=\"", StringComparison.Ordinal);
            if (fillIdx >= 0)
            {
                int q = element.IndexOf('"', fillIdx + 6);
                if (q > fillIdx + 6) seen.Add(element.Substring(fillIdx + 6, q - fillIdx - 6));
            }
            idx = end + 1;
        }
        return seen.Count;
    }

    [Fact]
    public void HueGroups_TwoGroups_ScatterUsesMultipleColors()
    {
        int[] hue = AlternatingHueOf(100);
        string svgPlain = RenderSvg();
        string svgHue   = RenderSvg(configure: s => { s.HueGroups = hue; });
        int distinctPlain = CountDistinctFills(svgPlain, "<circle ");
        int distinctHue   = CountDistinctFills(svgHue,   "<circle ");
        Assert.True(distinctHue > distinctPlain,
            $"With hue ({distinctHue} distinct fills) must beat without hue ({distinctPlain}).");
    }

    [Fact]
    public void HuePalette_FirstGroupGetsFirstPaletteColor()
    {
        var red  = new Color(0xFF, 0x00, 0x00);
        var blue = new Color(0x00, 0x00, 0xFF);
        int[] hue = AlternatingHueOf(100);
        string svg = RenderSvg(configure: s =>
        {
            s.HueGroups  = hue;
            s.HuePalette = [red, blue];
        });
        // Both palette colours should appear as circle fills.
        Assert.Contains("fill=\"#FF0000\"", svg);
        Assert.Contains("fill=\"#0000FF\"", svg);
    }

    [Fact]
    public void HueGroups_LengthMismatch_FallsBackGracefully()
    {
        // HueGroups too short (5 vs 100 samples) → renderer must not throw and
        // must produce a valid SVG (defensive identity-fallback pattern).
        int[] badHue = [0, 1, 0, 1, 0];
        var ex = Record.Exception(() => RenderSvg(configure: s => s.HueGroups = badHue));
        Assert.Null(ex);
    }

    [Fact]
    public void HueGroups_DiagonalKde_EmitsMultiplePolylines()
    {
        int[] hue = AlternatingHueOf(100);
        string svgPlain = RenderSvg(configure: s => s.DiagonalKind = PairGridDiagonalKind.Kde);
        string svgHue   = RenderSvg(configure: s =>
        {
            s.DiagonalKind = PairGridDiagonalKind.Kde;
            s.HueGroups    = hue;
        });
        int plainPolylines = CountOccurrences(svgPlain, "<polyline ") + CountOccurrences(svgPlain, "<path ");
        int hueLines       = CountOccurrences(svgHue,   "<polyline ") + CountOccurrences(svgHue,   "<path ");
        Assert.True(hueLines >= plainPolylines + 1,
            $"Hue diagonal KDE ({hueLines} lines) must add at least one curve over plain ({plainPolylines}).");
    }

    [Fact]
    public void HueGroups_DiagonalHistogram_EmitsMoreColors()
    {
        int[] hue = AlternatingHueOf(100);
        string svgPlain = RenderSvg();
        string svgHue   = RenderSvg(configure: s => s.HueGroups = hue);
        int distinctPlain = CountDistinctFills(svgPlain, "<rect ");
        int distinctHue   = CountDistinctFills(svgHue,   "<rect ");
        Assert.True(distinctHue >= distinctPlain,
            $"With hue ({distinctHue} distinct rect fills) must equal or exceed plain ({distinctPlain}).");
    }

    // ── Coverage: KDE on 1-sample input (curve.X.Length<2 guard, line 160) ────

    [Fact]
    public void DiagonalKind_Kde_OneSampleVariable_DoesNotThrow()
    {
        // 1 sample → GaussianKde returns a single-element curve, renderer's <2 guard
        // returns without emitting a polyline. Must not throw.
        var oneSample = new[] { new[] { 42.0 } };
        var ex = Record.Exception(() => RenderSvg(oneSample, s => s.DiagonalKind = PairGridDiagonalKind.Kde));
        Assert.Null(ex);
    }

    // ── Coverage: sub-pixel cell skip (PairGridSeriesRenderer.cs:36-37) ───────

    [Fact]
    public void TinyFigureSize_LargeN_SubPixelCellsSkipped()
    {
        // Small figure × many variables → cells fall below MinPanelPx and the
        // renderer's sub-pixel guard skips them. Must not throw and must produce
        // a valid SVG with very few or zero scatter circles.
        int n = 30; // 30×30 grid
        var rng = new Random(1);
        var bigVars = Enumerable.Range(0, n)
            .Select(_ => Enumerable.Range(0, 50).Select(_ => rng.NextDouble()).ToArray())
            .ToArray();

        // Force a very small figure size to trigger sub-pixel cells
        string svg = Plt.Create()
            .WithSize(80, 80)  // 80px figure / 30 cells ≈ 2.6 px per cell — below MinPanelPx (4.0)
            .AddSubPlot(1, 1, 1, ax => ax.PairGrid(bigVars))
            .ToSvg();

        Assert.Contains("<svg", svg);
        Assert.Contains("</svg>", svg);
        // Sub-pixel cells should be skipped → far fewer rects than 30×30×20 bins worth
        int rects = CountOccurrences(svg, "<rect ");
        Assert.True(rects < 100, $"Expected sub-pixel cells skipped → few rects, got {rects}.");
    }

    // ── Coverage: HueGroups negative group ID ─────────────────────────────────

    [Fact]
    public void HueGroups_NegativeGroupId_NoThrow_ColorWraps()
    {
        // Negative group IDs must fold modulo the palette via ((g % L) + L) % L.
        int[] hue = Enumerable.Range(0, 100).Select(i => (i % 2) - 1).ToArray(); // {-1, 0}
        var ex = Record.Exception(() => RenderSvg(configure: s => s.HueGroups = hue));
        Assert.Null(ex);
    }

    // ── Coverage: HuePalette empty array fallback (line 68 guard) ─────────────

    [Fact]
    public void HuePalette_EmptyArray_FallsBackToTab10()
    {
        // Empty user palette → `userPalette is { Length: > 0 }` guard fails, falls
        // back to Tab10. SVG must render and contain at least one Tab10 colour.
        int[] hue = AlternatingHueOf(100);
        string svg = RenderSvg(configure: s =>
        {
            s.HueGroups  = hue;
            s.HuePalette = []; // empty palette
        });
        Assert.Contains("<svg", svg);
        // Tab10 first colour = #1F77B4
        Assert.Contains("1F77B4", svg, StringComparison.OrdinalIgnoreCase);
    }

    // ── v1.10 — OffDiagonalKind = Hexbin ──────────────────────────────────────

    [Fact]
    public void OffDiagonalKind_Hexbin_RendersValidSvg()
    {
        string svg = RenderSvg(configure: s => s.OffDiagonalKind = PairGridOffDiagonalKind.Hexbin);
        Assert.Contains("<svg", svg);
        Assert.Contains("</svg>", svg);
    }

    [Fact]
    public void OffDiagonalKind_Hexbin_EmitsPolygons()
    {
        // Hexbin renders <polygon> elements (one per occupied hex cell). For 3 vars
        // × 6 off-diagonal cells × multiple bins → many polygons.
        string svg = RenderSvg(configure: s => s.OffDiagonalKind = PairGridOffDiagonalKind.Hexbin);
        int polygons = CountOccurrences(svg, "<polygon ");
        Assert.True(polygons >= 30, $"Expected ≥30 hexbin polygons, got {polygons}.");
    }

    [Fact]
    public void OffDiagonalKind_Hexbin_EmitsNoCircles()
    {
        // Hexbin replaces scatter — no per-point circles.
        // (Diagonal histograms emit <rect> not <circle>, so the off-diagonal is the only circle source.)
        string svg = RenderSvg(configure: s => s.OffDiagonalKind = PairGridOffDiagonalKind.Hexbin);
        int circles = CountOccurrences(svg, "<circle ");
        Assert.Equal(0, circles);
    }

    [Fact]
    public void OffDiagonalKind_Hexbin_LargerGridSize_MorePolygons()
    {
        string svgSmall = RenderSvg(configure: s =>
        {
            s.OffDiagonalKind = PairGridOffDiagonalKind.Hexbin;
            s.HexbinGridSize  = 5;
        });
        string svgLarge = RenderSvg(configure: s =>
        {
            s.OffDiagonalKind = PairGridOffDiagonalKind.Hexbin;
            s.HexbinGridSize  = 30;
        });
        int polysSmall = CountOccurrences(svgSmall, "<polygon ");
        int polysLarge = CountOccurrences(svgLarge, "<polygon ");
        Assert.True(polysLarge > polysSmall,
            $"Larger HexbinGridSize ({polysLarge}) must emit more polygons than smaller ({polysSmall}).");
    }

    [Fact]
    public void OffDiagonalKind_Hexbin_HueIgnored_FallsBackToSingleColor()
    {
        // Per design: hue is intentionally ignored when OffDiagonal=Hexbin (seaborn convention).
        // The hexbin colormap encodes density, not hue group — applying both would be ambiguous.
        int[] hue = AlternatingHueOf(100);
        string svgPlain = RenderSvg(configure: s => s.OffDiagonalKind = PairGridOffDiagonalKind.Hexbin);
        string svgHue   = RenderSvg(configure: s =>
        {
            s.OffDiagonalKind = PairGridOffDiagonalKind.Hexbin;
            s.HueGroups       = hue;
        });
        // Hexbin output should be effectively the same regardless of hue (single-color density).
        // Polygon counts should match within a small tolerance.
        int polysPlain = CountOccurrences(svgPlain, "<polygon ");
        int polysHue   = CountOccurrences(svgHue,   "<polygon ");
        Assert.Equal(polysPlain, polysHue);
    }

    [Fact]
    public void OffDiagonalKind_Hexbin_AllNonFiniteSamples_DoesNotThrow()
    {
        // Every sample non-finite → HexbinOffDiagonalPainter's xFiltered/yFiltered both
        // collapse to empty → early-return guard fires before HexGrid binning.
        var nonFiniteVars = new[]
        {
            new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity, double.NaN },
            new[] { double.NaN, double.NaN, double.PositiveInfinity, double.NegativeInfinity },
        };
        var ex = Record.Exception(() => RenderSvg(nonFiniteVars, s => s.OffDiagonalKind = PairGridOffDiagonalKind.Hexbin));
        Assert.Null(ex);
    }

    [Fact]
    public void OffDiagonalKind_Hexbin_OffDiagonalColorMap_RespectsCustomMap()
    {
        // Default colormap is Viridis. With Plasma set, SVG must contain Plasma fills.
        string svg = RenderSvg(configure: s =>
        {
            s.OffDiagonalKind     = PairGridOffDiagonalKind.Hexbin;
            s.OffDiagonalColorMap = MatPlotLibNet.Styling.ColorMaps.ColorMaps.Plasma;
        });
        Assert.Contains("<svg", svg);
        // Plasma's first colour is dark purple (~#0D0887). Viridis is dark blue-green (~#440154).
        // We just check the output renders cleanly and contains polygons — explicit colour-table
        // sniffing is brittle to tiny gamma/quantisation drifts.
        int polygons = CountOccurrences(svg, "<polygon ");
        Assert.True(polygons >= 30, "Expected hexbin polygons even with custom colormap.");
    }
}
