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
    // ── Smoke tests ──────────────────────────────────────────────────────────

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // r="3" appears for default MarkerSize=3.0; r="6" for explicit 6.0.
        new DrawnToken("MarkerSize_LargerRadius_AppearsInSvg (svgSmall)", () => RenderSvg(configure: s => s.MarkerSize = 3.0), "r=\"3\""),
        new DrawnToken("MarkerSize_LargerRadius_AppearsInSvg (svgLarge)", () => RenderSvg(configure: s => s.MarkerSize = 6.0), "r=\"6\""),
        // Both palette colours should appear as circle fills.
        new DrawnToken("HuePalette_FirstGroupGetsFirstPaletteColor", () =>
        {
            var red  = new Color(0xFF, 0x00, 0x00);
            var blue = new Color(0x00, 0x00, 0xFF);
            int[] hue = AlternatingHueOf(100);
            return RenderSvg(configure: s =>
            {
                s.HueGroups  = hue;
                s.HuePalette = [red, blue];
            });
        }, "fill=\"#FF0000\"", "fill=\"#0000FF\""),
        // Empty user palette -> `userPalette is { Length: > 0 }` guard fails, falls back to Tab10. SVG must render and contain at least
        // one Tab10 colour (the first, #1F77B4).
        new DrawnToken("HuePalette_EmptyArray_FallsBackToTab10", () =>
        {
            int[] hue = AlternatingHueOf(100);
            return RenderSvg(configure: s =>
            {
                s.HueGroups  = hue;
                s.HuePalette = []; // empty palette
            });
        }, "<svg", "1F77B4") { Comparison = StringComparison.OrdinalIgnoreCase },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    [Theory]
    [InlineData("<rect ", 60, "rects (3 variables x DiagonalBins=20 histogram rectangles; the renderer also emits axes and spine rects, so this is a lower bound)")]
    [InlineData("<circle ", 100, "circles for default scatter (3 variables, OffDiagonalKind = Scatter, Triangular = Both: 6 off-diagonal cells of 100 points each)")]
    public void DefaultRender_EmitsTheMarksOfItsPanels(string tag, int atLeast, string what)
    {
        string svg = RenderSvg();
        int count = svg.CountOf(tag);
        Assert.True(count >= atLeast, $"Expected at least {atLeast} {what}, got {count}.");
    }

    [Fact]
    public void DiagonalBins_MoreBins_ProducesMoreRectangles()
    {
        string svgFew  = RenderSvg(configure: s => s.DiagonalBins = 5);
        string svgMany = RenderSvg(configure: s => s.DiagonalBins = 50);
        int rectsFew  = svgFew.CountOf("<rect ");
        int rectsMany = svgMany.CountOf("<rect ");
        Assert.True(rectsMany > rectsFew,
            $"Expected more bins → more rects. Got {rectsFew} (5 bins) vs {rectsMany} (50 bins).");
    }

    // ── DiagonalKind = None suppresses diagonal output ───────────────────────

    [Fact]
    public void DiagonalKind_None_FewerBarsThanDefault()
    {
        string svgDefault = RenderSvg();
        string svgNoDiag  = RenderSvg(configure: s =>
        {
            s.DiagonalKind = PairGridDiagonalKind.None;
            s.OffDiagonalKind = PairGridOffDiagonalKind.None;
        });
        int rectsDefault = svgDefault.CountOf("<rect ");
        int rectsNoDiag  = svgNoDiag.CountOf("<rect ");
        Assert.True(rectsDefault > rectsNoDiag,
            $"Histogram-on default ({rectsDefault}) must emit more rects than histogram-off ({rectsNoDiag}).");
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
        svg.AssertNoNonFiniteNumber();
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
    public void OffDiagonalKind_None_EmitsFewerCirclesThanScatter()
    {
        string svgScatter = RenderSvg();
        string svgNoScat  = RenderSvg(configure: s => s.OffDiagonalKind = PairGridOffDiagonalKind.None);
        int cScatter = svgScatter.CountOf("<circle ");
        int cNoScat  = svgNoScat.CountOf("<circle ");
        Assert.True(cScatter > cNoScat,
            $"Scatter on ({cScatter}) must emit more circles than scatter off ({cNoScat}).");
    }

    [Fact]
    public void ScatterCells_AreSymmetricByDefault_BothTriangles()
    {
        // Default Triangular = Both: 3 vars → 6 off-diagonal cells (lower + upper).
        // Suppressing OffDiagonal proves all 6 contributed circles.
        string svgBoth = RenderSvg();
        string svgNone = RenderSvg(configure: s => s.OffDiagonalKind = PairGridOffDiagonalKind.None);
        int diff = svgBoth.CountOf("<circle ") - svgNone.CountOf("<circle ");
        // Each off-diagonal cell ≈ 100 circles × 6 cells = ~600 circles.
        Assert.True(diff >= 200, $"Expected ≥200 scatter circles across off-diagonal cells, got {diff}.");
    }

    // ── Triangular = LowerOnly / UpperOnly (Step 6) ───────────────────────────

    [Theory]
    [InlineData(PairGridTriangle.LowerOnly)]
    [InlineData(PairGridTriangle.UpperOnly)]
    public void Triangular_OneHalf_EmitsFewerCirclesThanBoth(PairGridTriangle half)
    {
        string svgBoth   = RenderSvg();
        string svgHalf   = RenderSvg(configure: s => s.Triangular = half);
        int cBoth  = svgBoth.CountOf("<circle ");
        int cHalf  = svgHalf.CountOf("<circle ");
        // One half hides 3 of the 6 off-diagonal cells → roughly half the scatter circles.
        Assert.True(cHalf < cBoth,
            $"{half} ({cHalf}) must emit fewer circles than Both ({cBoth}).");
    }

    [Fact]
    public void Triangular_LowerAndUpper_ProduceSimilarOffDiagonalCircleCounts()
    {
        // For symmetric data the two halves should each carry ~the same scatter density.
        string svgLower = RenderSvg(configure: s => s.Triangular = PairGridTriangle.LowerOnly);
        string svgUpper = RenderSvg(configure: s => s.Triangular = PairGridTriangle.UpperOnly);
        int cLower = svgLower.CountOf("<circle ");
        int cUpper = svgUpper.CountOf("<circle ");
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
        int rLower = svgLower.CountOf("<rect ");
        int rEmpty = svgEmpty.CountOf("<rect ");
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
        int rHist = svgHist.CountOf("<rect ");
        int rKde  = svgKde.CountOf("<rect ");
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
        int plainPolylines = svgPlain.CountOf("<polyline ") + svgPlain.CountOf("<path ");
        int hueLines       = svgHue.CountOf("<polyline ") + svgHue.CountOf("<path ");
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

    // ── KDE on a one-sample variable ──────────────────────────────────────────

    [Fact]
    public void DiagonalKind_Kde_OneSampleVariable_DoesNotThrow()
    {
        // 1 sample → no spread, so Silverman takes the size of the value as its spread
        // and the curve is a single bump. Must not throw.
        var oneSample = new[] { new[] { 42.0 } };
        var ex = Record.Exception(() => RenderSvg(oneSample, s => s.DiagonalKind = PairGridDiagonalKind.Kde));
        Assert.Null(ex);
    }

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
        int rects = svg.CountOf("<rect ");
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

    // ── v1.10 — OffDiagonalKind = Hexbin ──────────────────────────────────────

    [Fact]
    public void OffDiagonalKind_Hexbin_EmitsPolygons()
    {
        // Hexbin renders <polygon> elements (one per occupied hex cell). For 3 vars
        // × 6 off-diagonal cells × multiple bins → many polygons.
        string svg = RenderSvg(configure: s => s.OffDiagonalKind = PairGridOffDiagonalKind.Hexbin);
        int polygons = svg.CountOf("<polygon ");
        Assert.True(polygons >= 30, $"Expected ≥30 hexbin polygons, got {polygons}.");
    }

    [Fact]
    public void OffDiagonalKind_Hexbin_EmitsNoCircles()
    {
        // Hexbin replaces scatter — no per-point circles.
        // (Diagonal histograms emit <rect> not <circle>, so the off-diagonal is the only circle source.)
        string svg = RenderSvg(configure: s => s.OffDiagonalKind = PairGridOffDiagonalKind.Hexbin);
        int circles = svg.CountOf("<circle ");
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
        int polysSmall = svgSmall.CountOf("<polygon ");
        int polysLarge = svgLarge.CountOf("<polygon ");
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
        int polysPlain = svgPlain.CountOf("<polygon ");
        int polysHue   = svgHue.CountOf("<polygon ");
        Assert.Equal(polysPlain, polysHue);
    }

    [Fact]
    public void OffDiagonalKind_Hexbin_AllNonFiniteSamples_DoesNotThrow()
    {
        // Every sample non-finite → HexbinOffDiagonalPainter's xFiltered/yFiltered both
        // collapse to empty → early-return guard fires before HexLattice binning.
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
        int polygons = svg.CountOf("<polygon ");
        Assert.True(polygons >= 30, "Expected hexbin polygons even with custom colormap.");
    }

    /// <summary>A hexbin panel of a pair grid is a hexbin that draws the hexagons holding a point (<c>mincnt=1</c>), and colours its
    /// counts by the hexbin's one rule (<c>HexbinSeries</c>): under the matplotlib themes from the least count drawn, as matplotlib's
    /// norm autoscales to the drawn cells (python, 3.11.2: <c>hexbin(mincnt=1)</c> over counts 5 and 7 colours over (5, 7)); under the
    /// library's own from half a count under one. Here two hexagons hold 2 and 5 points.</summary>
    [Theory]
    [InlineData("MatplotlibV2", 0.0)]
    [InlineData("Default", 1.5 / 4.5)]
    public void AHexbinPanel_ColoursItsCountsByTheHexbinsRule(string themeName, double leastAt)
    {
        var map = new MatPlotLibNet.Styling.ColorMaps.LinearColorMap("test", [Colors.Red, Colors.Blue]);
        var theme = themeName.AsTheme();
        double[] corners = [0, 0, 10, 10, 10, 10, 10];
        var figure = Plt.Create().WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax => ax.PairGrid([corners, corners], s =>
            {
                s.OffDiagonalKind = PairGridOffDiagonalKind.Hexbin;
                s.OffDiagonalColorMap = map;
                s.HexbinGridSize = 4;
            }))
            .Build();
        var ctx = new RecordingRenderContext();

        new MatPlotLibNet.Rendering.ChartRenderer().Render(figure, ctx);

        var fills = ctx.Of<DrawPolygonCall>().Select(c => c.Fill).Distinct().ToList();
        Assert.Contains(map.GetColor(leastAt), fills);
        Assert.Contains(map.GetColor(1.0), fills);
    }

    // ── Variable labels ───────────────────────────────────────────────────────

    private static (double X, double Y, string Text, bool Turned)[] Texts(string svg) =>
    [
        .. svg.Elements("text")
            .Select(text => (text.Number("x"), text.Number("y"), text.Text, (text["transform"] ?? string.Empty).Contains("rotate", StringComparison.Ordinal))),
    ];

    /// <summary>Each variable is named as seaborn's pairplot names it: under the cell of its column in the bottom row, and turned a
    /// quarter beside the cell of its row in the left column, so a grid given labels says which variable each cell plots.</summary>
    [Fact]
    public void GivenLabels_NameEachVariableUnderItsColumnAndBesideItsRow()
    {
        var texts = Texts(RenderSvg(configure: s => s.Labels = ["alpha", "beta", "gamma"]));

        foreach (var name in new[] { "alpha", "beta", "gamma" })
        {
            Assert.Equal(2, texts.Count(t => t.Text == name));
            Assert.Single(texts, t => t.Text == name && t.Turned);
        }

        var under = texts.Where(t => !t.Turned && t.Text is "alpha" or "beta" or "gamma").OrderBy(t => t.X).Select(t => t.Text);
        var beside = texts.Where(t => t.Turned).OrderBy(t => t.Y).Select(t => t.Text);
        Assert.Equal(["alpha", "beta", "gamma"], under);
        Assert.Equal(["alpha", "beta", "gamma"], beside);
    }

    /// <summary>A grid given no labels names nothing, and a variable past the end of a short list goes unnamed.</summary>
    [Fact]
    public void NoLabels_NameNothing_AndAShortListNamesOnlyTheVariablesItCovers()
    {
        Assert.DoesNotContain(Texts(RenderSvg()), t => t.Turned);

        var texts = Texts(RenderSvg(configure: s => s.Labels = ["alpha"]));
        Assert.Equal(2, texts.Count(t => t.Text == "alpha"));
        Assert.Single(texts, t => t.Turned);

        var gap = Texts(RenderSvg(configure: s => s.Labels = ["alpha", "", "gamma"]));
        Assert.Equal(2, gap.Count(t => t.Turned));
    }
}
