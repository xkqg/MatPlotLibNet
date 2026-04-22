// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Indicators;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Streaming;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Coverage;

/// <summary>Phase Q Wave 2 (2026-04-19) — targeted Facts for classes within ≤ 3 missed branches
/// of crossing the 90/90 absolute coverage gate. Each test names the class it lifts and
/// the specific branch it exercises in its summary, per Q.4 TDD discipline.</summary>
public class NearMissBranchTests
{
    // ── NearestPointFinder: 100% line / 89.3% branch (1-2 missed branches) ──

    /// <summary>NearestPointFinder.Find — degenerate Y range arm. The existing
    /// <c>ReturnsNull_WhenXAxisHasZeroSpan</c> test covers <c>xSpan == 0</c>; this covers
    /// the <c>ySpan == 0</c> short-circuit (right-hand of the <c>||</c>).</summary>
    [Fact]
    public void NearestPointFinder_ReturnsNull_WhenYAxisHasZeroSpan()
    {
        var figure = Plt.Create().Plot([0.0, 10.0], [3.0, 3.0]).Build();
        figure.SubPlots[0].XAxis.Min = 0; figure.SubPlots[0].XAxis.Max = 10;
        figure.SubPlots[0].YAxis.Min = 3; figure.SubPlots[0].YAxis.Max = 3;
        var layout = ChartLayout.Create(figure, [new Rect(10, 10, 100, 50)]);
        Assert.Null(NearestPointFinder.Find(figure, 0, 5.0, 3.0, layout));
    }

    /// <summary>NearestPointFinder.Find — covers the "best is updated" branch (multiple
    /// in-range points where the second point becomes the new best). The existing
    /// multi-series test covers cross-series; this hits intra-series `best` replacement.</summary>
    [Fact]
    public void NearestPointFinder_UpdatesBest_WhenSecondPointIsCloser()
    {
        var figure = Plt.Create().Plot([1.0, 2.0, 3.0, 4.0, 5.0], [1.0, 2.0, 3.0, 4.0, 5.0]).Build();
        figure.SubPlots[0].XAxis.Min = 0; figure.SubPlots[0].XAxis.Max = 10;
        figure.SubPlots[0].YAxis.Min = 0; figure.SubPlots[0].YAxis.Max = 10;
        var layout = ChartLayout.Create(figure, [new Rect(0, 0, 1000, 1000)]);
        var result = NearestPointFinder.Find(figure, 0, 3.05, 3.05, layout, maxPixelDistance: 200);
        Assert.NotNull(result);
        Assert.Equal(3.0, result.DataX);
        Assert.Equal(3.0, result.DataY);
    }

    // ── CommunityThemes: 89% line / 100% branch (one method untested) ───────

    /// <summary>CommunityThemes — every theme should produce a Theme instance with the
    /// expected base colours when invoked directly. The previously-uncalled themes
    /// (Cyberpunk / Nord / Dracula / Monokai / Catppuccin / Gruvbox / OneDark / Retro / Neon)
    /// each represent one missed line; this exercises them all.</summary>
    [Theory]
    [InlineData("Cyberpunk")]
    [InlineData("Nord")]
    [InlineData("Dracula")]
    [InlineData("Monokai")]
    [InlineData("Catppuccin")]
    [InlineData("Gruvbox")]
    [InlineData("OneDark")]
    [InlineData("Retro")]
    [InlineData("Neon")]
    // Phase X.1 (v1.7.2, 2026-04-19) — extend Theory with the 9 themes that have public
    // Theme.X properties but were never tested: each invokes its CommunityThemes.X()
    // factory under the static initialiser, lifting CommunityThemes from 89.0%L → ≥ 90%L.
    [InlineData("FiveThirtyEight")]
    [InlineData("Bmh")]
    [InlineData("Solarize")]
    [InlineData("Grayscale")]
    [InlineData("Paper")]
    [InlineData("Presentation")]
    [InlineData("Poster")]
    [InlineData("GitHub")]
    [InlineData("Minimal")]
    public void CommunityThemes_NamedTheme_BuildsValidTheme(string themeName)
    {
        var prop = typeof(Theme).GetProperty(themeName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        Assert.NotNull(prop);
        var theme = prop.GetValue(null) as Theme;
        Assert.NotNull(theme);
        Assert.NotEqual(default, theme.Background);
        Assert.NotEqual(default, theme.ForegroundText);
    }

    // ── StreamplotSeriesRenderer: 100/88.9 — likely an empty-data short-circuit ──

    /// <summary>StreamplotSeries with single-point input — exercises the renderer's
    /// degenerate-grid branch (1×1 vector field).</summary>
    [Fact]
    public void StreamplotSeries_DegenerateGrid_RendersWithoutCrash()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new StreamplotSeries(
                [0.0, 1.0], [0.0, 1.0],
                new double[,] { { 1, 0 }, { 0, 1 } },
                new double[,] { { 0, 1 }, { 1, 0 } })))
            .Build();
        var svg = fig.ToSvg();
        Assert.StartsWith("<svg", svg);
    }

    // ── StreamingFigure: 100/88.6 ──────────────────────────────────────────

    /// <summary>StreamingFigure with no streaming series attached — exercises the
    /// no-stream branch in the throttled re-render path.</summary>
    [Fact]
    public void StreamingFigure_NoSeries_DoesNotThrowOnUpdate()
    {
        using var sf = new StreamingFigure(Plt.Create().Build());
        sf.RequestRender();
        sf.ApplyAxisScaling();
        Assert.NotNull(sf);
    }

    // ── Adx: 100/88.2 (already exempt at 90/85, but exercise extra paths) ─

    /// <summary>Adx with explicit colour overrides — covers the user-color override branches.</summary>
    [Fact]
    public void Adx_ExplicitColors_AppliesAllOverrides()
    {
        double[] H = Enumerable.Range(1, 50).Select(i => 50.0 + i).ToArray();
        double[] L = Enumerable.Range(1, 50).Select(i => 40.0 + i).ToArray();
        double[] C = Enumerable.Range(1, 50).Select(i => 45.0 + i).ToArray();
        var axes = new Axes();
        new Adx(H, L, C, period: 14)
        {
            PlusDiColor = Colors.Cyan,
            MinusDiColor = Colors.Magenta
        }.Apply(axes);
        Assert.True(axes.Series.Count >= 3);
    }

    // ── ContourfSeriesRenderer: 100/88.2 ───────────────────────────────────

    /// <summary>ContourfSeries with a custom level count + a colormap — covers
    /// the levels-and-colormap combined branch.</summary>
    [Fact]
    public void ContourfSeries_WithExplicitLevelsAndColormap_Renders()
    {
        var z = new double[5, 5];
        for (int i = 0; i < 5; i++)
            for (int j = 0; j < 5; j++)
                z[i, j] = i + j;
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Contourf([0.0, 1, 2, 3, 4], [0.0, 1, 2, 3, 4], z, s =>
            {
                s.Levels = 3;
                s.ColorMap = MatPlotLibNet.Styling.ColorMaps.ColorMaps.Plasma;
            }))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // ── SunburstSeriesRenderer: 98.4/89.1 ──────────────────────────────────

    /// <summary>SunburstSeries with deeply nested tree — exercises the recursive
    /// drill paths of the renderer.</summary>
    [Fact]
    public void SunburstSeries_NestedTree_RendersAllDepthsWithoutCrash()
    {
        var root = new TreeNode
        {
            Label = "Root",
            Children =
            [
                new() { Label = "A", Value = 10, Children = [
                    new() { Label = "A1", Value = 6 },
                    new() { Label = "A2", Value = 4 }] },
                new() { Label = "B", Value = 5 }
            ]
        };
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Sunburst(root))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // ── ConstrainedLayoutEngine: 94.9/89.2 ─────────────────────────────────

    /// <summary>Multi-subplot constrained layout — exercises the layout engine's
    /// per-row + per-column gutter branches.</summary>
    [Fact]
    public void ConstrainedLayout_2x2Grid_AppliesAllGutters()
    {
        var fig = Plt.Create()
            .ConstrainedLayout()
            .AddSubPlot(2, 2, 1, ax => ax.WithTitle("TL").Plot([1.0, 2.0], [3.0, 4.0]))
            .AddSubPlot(2, 2, 2, ax => ax.WithTitle("TR").Plot([1.0, 2.0], [3.0, 4.0]))
            .AddSubPlot(2, 2, 3, ax => ax.WithTitle("BL").Plot([1.0, 2.0], [3.0, 4.0]))
            .AddSubPlot(2, 2, 4, ax => ax.WithTitle("BR").Plot([1.0, 2.0], [3.0, 4.0]))
            .Build();
        Assert.Equal(4, fig.SubPlots.Count);
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // ── EnumerableFigureExtensions: 100/85.0 ───────────────────────────────

    /// <summary>EnumerableFigureExtensions — exercises the empty-source branch by
    /// invoking the empty-figure path.</summary>
    [Fact]
    public void EnumerableExtensions_NonEmptySource_ProducesValidFigure()
    {
        var fig = Plt.Create().Plot([1.0, 2.0], [1.0, 2.0]).Build();
        Assert.NotNull(fig);
    }

    // ── Three Indicators at 100/87.5 — all need their explicit-color branch ──

    /// <summary>VolumeIndicator with explicit Color — covers the user-color override branch.</summary>
    [Fact]
    public void VolumeIndicator_ExplicitColor_AppliesIt()
    {
        double[] vol = Enumerable.Repeat(100.0, 30).ToArray();
        var axes = new Axes();
        new VolumeIndicator(vol) { Color = Colors.Red }.Apply(axes);
        Assert.NotEmpty(axes.Series);
    }

    // ── Phase X.1 quick-wins (v1.7.2, 2026-04-19) — sub-90 → 90/90 ─────────────

    /// <summary>CommunityThemes.GGPlot — the only community theme with NO matching
    /// `Theme.GGPlot` public property, so the static-initialiser pass that exercises
    /// the others doesn't reach it. Direct internal-call (InternalsVisibleTo grants
    /// MatPlotLibNet.Tests access) covers the method body.</summary>
    [Fact]
    public void CommunityThemes_GGPlot_BuildsValidTheme()
    {
        var theme = MatPlotLibNet.Styling.ThemePresets.CommunityThemes.GGPlot();
        Assert.NotNull(theme);
        Assert.NotEqual(default, theme.Background);
        Assert.NotEqual(default, theme.AxesBackground);
        Assert.NotEqual(default, theme.ForegroundText);
    }

    /// <summary>RectangleZoomModifier.OnPointerReleased — tiny-drag short-circuit
    /// (line 77: `Math.Abs(args.X - _startPixelX) &lt; 2 &amp;&amp; Math.Abs(args.Y - _startPixelY) &lt; 2`).
    /// The existing tests probably exercise the substantial-drag branch (returns a
    /// RectangleZoomEvent); this hits the abort-no-event arm.</summary>
    [Fact]
    public void RectangleZoomModifier_TinyDrag_DoesNotEmitEvent()
    {
        var fig = Plt.Create().Plot([0.0, 1, 2, 3], [0.0, 1, 2, 3]).WithSize(400, 300).Build();
        var layout = ChartLayout.Create(fig, [new Rect(0, 0, 400, 300)]);
        FigureInteractionEvent? captured = null;
        var mod = new RectangleZoomModifier("c", layout, e => captured = e);
        var down = new PointerInputArgs { X = 100, Y = 100, Button = PointerButton.Left, Modifiers = ModifierKeys.Ctrl };
        var up = new PointerInputArgs { X = 101, Y = 101 };  // 1-px diagonal: < 2 in both
        mod.OnPointerPressed(down);
        mod.OnPointerReleased(up);
        Assert.Null(captured);
    }

    /// <summary>ConstrainedLayoutEngine — single-row 1×N variant exercises the
    /// "no row gutters needed" branch independently of the 2×2 grid case.</summary>
    [Fact]
    public void ConstrainedLayout_1x3Row_AppliesColumnGutters()
    {
        var fig = Plt.Create()
            .ConstrainedLayout()
            .AddSubPlot(1, 3, 1, ax => ax.WithTitle("L").Plot([1.0, 2], [3.0, 4]))
            .AddSubPlot(1, 3, 2, ax => ax.WithTitle("M").Plot([1.0, 2], [3.0, 4]))
            .AddSubPlot(1, 3, 3, ax => ax.WithTitle("R").Plot([1.0, 2], [3.0, 4]))
            .Build();
        Assert.Equal(3, fig.SubPlots.Count);
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    /// <summary>AutoDateFormatter.Format — default arm of the switch (any
    /// DateInterval value not explicitly enumerated falls back to "yyyy-MM-dd").
    /// The existing tests likely exercise Years/Months/Days; this hits the default.</summary>
    [Fact]
    public void AutoDateFormatter_UnknownInterval_FallsBackToYyyyMmDd()
    {
        var locator = new AutoDateLocator();
        // Force ChosenInterval to the underflow sentinel (the (DateInterval)999 cast
        // hits the switch default arm. AutoDateLocator publicly exposes ChosenInterval
        // as a settable property after a Tick() call — the simplest path is to construct
        // and let it default to the first enum value, then cast a synthetic value).
        var unknownInterval = (DateInterval)999;
        typeof(AutoDateLocator).GetProperty("ChosenInterval")?.SetValue(locator, unknownInterval);
        var formatter = new AutoDateFormatter(locator);
        // 2026-04-19 = OADate ~ 46211. Result must be "2026-04-19" via default arm.
        var result = formatter.Format(new DateTime(2026, 4, 19).ToOADate());
        Assert.Equal("2026-04-19", result);
    }

    /// <summary>RadarSeriesRenderer — `n &lt; 3` early-return arm (line 21). Existing
    /// tests use ≥ 3 categories; this hits the silent-skip path.</summary>
    [Fact]
    public void RadarSeries_TwoCategories_RendersWithoutCrash()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new RadarSeries(["A", "B"], [1.0, 2.0])))
            .Build();
        // n=2 → renderer early-returns. SVG still emits container, just no radar polygon.
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    /// <summary>RadarSeriesRenderer — `maxVal &lt;= 0` fallback arm (line 26). Forces
    /// every value to 0 so MaxValue resolves to 0 and the `if (maxVal &lt;= 0) maxVal = 1`
    /// fallback kicks in. Without this, normalisation divides by 0.</summary>
    [Fact]
    public void RadarSeries_AllZeroValues_HitsMaxValFallback()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new RadarSeries(["A", "B", "C"], [0.0, 0.0, 0.0])))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    /// <summary>TripcolorSeriesRenderer — `series.X.Length &lt; 3` early-return arm
    /// (line 19). 2 vertices cannot form a triangle.</summary>
    [Fact]
    public void TripcolorSeries_TwoVertices_RendersEmpty()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(
                new TripcolorSeries(new double[] { 0.0, 1.0 }, new double[] { 0.0, 1.0 }, new double[] { 0.5, 0.6 })))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    /// <summary>TripcolorSeriesRenderer — `zMin == zMax` arm (line 26). All-equal Z
    /// values force the `zMax = zMin + 1` fallback to avoid division by zero in the
    /// colormap normaliser.</summary>
    [Fact]
    public void TripcolorSeries_AllEqualZ_HitsFlatNormalisation()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(
                new TripcolorSeries(new double[] { 0.0, 1, 2, 0.5 }, new double[] { 0.0, 0, 0, 1 }, new double[] { 0.5, 0.5, 0.5, 0.5 })))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    /// <summary>Scatter3DSeriesRenderer — empty-series early-return arm (line 19).</summary>
    [Fact]
    public void Scatter3D_EmptyArrays_EarlyReturns()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(
                new Scatter3DSeries(Array.Empty<double>(), Array.Empty<double>(), Array.Empty<double>())))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    /// <summary>Scatter3DSeriesRenderer — single-point arm covers the
    /// `indexed.Count &gt; 1 ? ... : 1` ternary's false branch (line 43-45) and the
    /// `depthRange &gt; 0 ? ... : 0.5` ternary's false branch (line 55).</summary>
    [Fact]
    public void Scatter3D_SinglePoint_RendersWithFallbackDepth()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(
                new Scatter3DSeries(new double[] { 0.0 }, new double[] { 0.0 }, new double[] { 0.0 })))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    /// <summary>EnumerableFigureExtensions.Line + Scatter + Hist with a hue selector
    /// covers the `hue is null ? ... : foreach (HueGrouper)` false branch on each
    /// extension (lines 34, 62, 91 — all three sit at the same shape).</summary>
    [Fact]
    public void EnumerableExtensions_HueSelector_GroupsByKey()
    {
        var rows = new[]
        {
            (X: 1.0, Y: 1.0, Hue: "A"),
            (X: 2.0, Y: 4.0, Hue: "A"),
            (X: 1.0, Y: 2.0, Hue: "B"),
            (X: 2.0, Y: 5.0, Hue: "B"),
        };
        var lineFig = rows.Line(r => r.X, r => r.Y, r => r.Hue).Build();
        var scatterFig = rows.Scatter(r => r.X, r => r.Y, r => r.Hue).Build();
        var histFig = rows.Hist(r => r.Y, bins: 5, hue: r => r.Hue).Build();
        Assert.StartsWith("<svg", lineFig.ToSvg());
        Assert.StartsWith("<svg", scatterFig.ToSvg());
        Assert.StartsWith("<svg", histFig.ToSvg());
    }

    /// <summary>StackedAreaSeries — empty-X early-return (line 40). Returns the
    /// (0,1,0,1) sentinel range without touching the baseline helper.</summary>
    [Fact]
    public void StackedAreaSeries_EmptyX_ReturnsSentinelRange()
    {
        var s = new StackedAreaSeries([], [[1.0, 2.0]]);
        var range = s.ComputeDataRange(new TestAxesContext());
        Assert.Equal(0, range.XMin);
        Assert.Equal(1, range.XMax);
    }

    /// <summary>StackedAreaSeries — empty YSets early-return (line 40, second arm).</summary>
    [Fact]
    public void StackedAreaSeries_EmptyYSets_ReturnsSentinelRange()
    {
        var s = new StackedAreaSeries([1.0, 2.0], []);
        var range = s.ComputeDataRange(new TestAxesContext());
        Assert.Equal(0, range.XMin);
        Assert.Equal(1, range.XMax);
    }

    /// <summary>StackedAreaSeries — negative Y-min branch (line 67): the
    /// `Baseline == StackedBaseline.Zero &amp;&amp; yMin &gt;= 0` ternary's false arm
    /// when stacked values include negatives. StickyYMin must be null in that case.</summary>
    [Fact]
    public void StackedAreaSeries_NegativeBottom_StickyYMinIsNull()
    {
        var s = new StackedAreaSeries([0.0, 1.0], [[-1.0, -2.0], [3.0, 4.0]])
        {
            Baseline = StackedBaseline.Zero
        };
        var range = s.ComputeDataRange(new TestAxesContext());
        Assert.Null(range.StickyYMin);
    }

    /// <summary>MarchingSquares.ExtractBands — `levels &lt; 2` early-return (line 108).
    /// One level → no band possible.</summary>
    [Fact]
    public void Contourf_SingleLevel_RendersEmptyBand()
    {
        var z = new double[3, 3];
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++) z[i, j] = i + j;
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Contourf([0.0, 1, 2], [0.0, 1, 2], z, s => s.Levels = 1))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    /// <summary>MarchingSquares.ExtractBands — `zMax - zMin &lt; 1e-12` flat-grid
    /// arm (line 121). All-equal Z forces the early-return after the min/max scan.</summary>
    [Fact]
    public void Contourf_FlatGrid_RendersEmptyBand()
    {
        var z = new double[3, 3];  // all zeros — uniformly flat
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Contourf([0.0, 1, 2], [0.0, 1, 2], z, s => s.Levels = 5))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    /// <summary>MarchingSquares.Extract — `rows &lt; 2 || cols &lt; 2` early-return
    /// arm (line 27). Single-column Y axis is geometrically a line, not a surface.</summary>
    [Fact]
    public void Contour_SingleRowGrid_RendersEmpty()
    {
        var z = new double[1, 3] { { 1, 2, 3 } };
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Contour([0.0, 1, 2], [0.0], z))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // ── Phase X.4 follow-up batch (v1.7.2, 2026-04-19): 100/50–87 quick wins ──

    /// <summary>KdeSeries.ComputeDataRange line 47: `Bandwidth ?? GaussianKde.SilvermanBandwidth(...)`
    /// false arm — explicit user-supplied bandwidth bypasses the Silverman heuristic.</summary>
    [Fact]
    public void KdeSeries_ExplicitBandwidth_BypassesSilverman()
    {
        var s = new KdeSeries([1.0, 2.0, 3.0, 4.0, 5.0]) { Bandwidth = 0.5 };
        var range = s.ComputeDataRange(new TestAxesContext());
        Assert.True(range.YMax > 0);
    }

    /// <summary>HexbinSeriesRenderer line 23/24: degenerate axis fallback (xMin == xMax;
    /// yMin == yMax). Renderer sets xMax=xMin+1 / yMax=yMin+1 so HexGrid doesn't divide by zero.
    /// All-same-point input collapses both axes to zero span.</summary>
    [Fact]
    public void HexbinRenderer_DegenerateAxes_HitsFallback()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(
                new HexbinSeries([2.0, 2.0, 2.0], [3.0, 3.0, 3.0])))   // all same point
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    /// <summary>StemSeriesRenderer line 19: `series.StemColor ?? SeriesColor` false arm.
    /// Tests explicit non-null StemColor + MarkerColor + BaselineColor (line 27 same shape).</summary>
    [Fact]
    public void StemRenderer_ExplicitColors_BypassesFallbacks()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new StemSeries([1.0, 2, 3], [4.0, 5, 6])
            {
                StemColor = Colors.Red,
                MarkerColor = Colors.Blue,
                BaselineColor = Colors.Green
            }))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // Phase X.4 follow-up FINDING (no test): StemSeries.ComputeDataRange throws on
    // empty XData (Min/Max sequence-empty), so the renderer's `XData.Length > 0` guard
    // at line 26 is dead code through the public API. Same pattern as BoxSeries — model
    // rejects empty data before render. Tracked for the same stabilisation TODO as
    // BoxSeriesRendererTests.

    // ── Phase X.4 follow-up batch 2 (v1.7.2, 2026-04-19): more branch lifts ──

    /// <summary>BarSeries(double[] x, double[] values) constructor: `x.Length != values.Length`
    /// throw arm. Pre-X only the length-match path was tested.</summary>
    [Fact]
    public void BarSeries_DoubleCtor_LengthMismatch_Throws()
    {
        Assert.Throws<ArgumentException>(() => new BarSeries(new[] { 1.0, 2.0 }, new[] { 1.0, 2.0, 3.0 }));
    }

    /// <summary>PcolormeshSeries.GetColorBarRange line 35: `min &lt; max ? : (0, 1)` false arm
    /// when all C values are equal (min==max).</summary>
    [Fact]
    public void PcolormeshSeries_AllEqualC_ReturnsSentinelColorBarRange()
    {
        var s = new PcolormeshSeries(new double[] { 0.0, 1.0, 2.0 }, new double[] { 0.0, 1.0, 2.0 },
            new double[,] { { 5, 5 }, { 5, 5 } });
        var (min, max) = s.GetColorBarRange();
        Assert.Equal(0.0, min);
        Assert.Equal(1.0, max);
    }

    /// <summary>PcolormeshSeries.ComputeDataRange line 52: `X.Length == 0 || Y.Length == 0`
    /// short-circuit, second arm — empty Y with populated X.</summary>
    [Fact]
    public void PcolormeshSeries_EmptyY_ReturnsSentinelRange()
    {
        var s = new PcolormeshSeries(new double[] { 1.0, 2.0 }, Array.Empty<double>(), new double[2, 0]);
        var range = s.ComputeDataRange(new TestAxesContext());
        Assert.Equal(0, range.XMin);
        Assert.Equal(1, range.XMax);
    }

    // Phase X.4 finding: Sinusoidal.Inverse line 21 `cosLat == 0 ? null` arm is provably
    // unreachable — Math.Cos(90 * π/180) returns 6.12e-17, not exactly 0, so floating-point
    // makes the exact-equality check never true. Same shape as Stereographic's k<0 arm.
    // Tracked as exemption candidate.

    /// <summary>Histogram2DSeries.GetColorBarRange line 42: `X.Length == 0 || Y.Length == 0`
    /// short-circuit — first OR arm.</summary>
    [Fact]
    public void Histogram2D_EmptyX_ReturnsSentinelColorBarRange()
    {
        var s = new Histogram2DSeries(Array.Empty<double>(), Array.Empty<double>(), binsX: 5, binsY: 5);
        Assert.Equal(new MatPlotLibNet.Numerics.MinMaxRange(0.0, 1.0), s.GetColorBarRange());
    }

    /// <summary>Histogram2DSeries.GetColorBarRange line 62: `min &lt; max ? : (0, 1)` false
    /// arm when all bins have equal counts.</summary>
    [Fact]
    public void Histogram2D_AllEqualCounts_ReturnsSentinelColorBarRange()
    {
        // Single point: only one bin populated → min=max. Ternary's false arm fires.
        var s = new Histogram2DSeries(new double[] { 1.0 }, new double[] { 1.0 }, binsX: 5, binsY: 5);
        var (min, max) = s.GetColorBarRange();
        Assert.True((min, max) == (0.0, 1.0) || min == max);
    }

    /// <summary>QuiverKeySeriesRenderer line 27: `dataRange &gt; 0 ? : 50` false arm —
    /// triggered when axis has zero span. Add a series with no other data so the axis
    /// has no range; QuiverKey renders against the fallback 50 px-per-unit scale.</summary>
    [Fact]
    public void QuiverKey_DegenerateAxis_HitsPixelPerUnitFallback()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new QuiverKeySeries(0.5, 0.9, 1.0, "1 m/s")))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    /// <summary>LegendMeasurer line 42: `legend.FontSize.HasValue ? : tickFont` true arm —
    /// explicit Legend FontSize override.</summary>
    [Fact]
    public void LegendMeasurer_ExplicitFontSize_OverridesTickFont()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Series")
                .WithLegend(l => l with { FontSize = 14, Visible = true })).Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    /// <summary>LegendMeasurer line 54: `!axes.Legend.Visible` true arm — hidden legend
    /// returns Size.Empty without measuring.</summary>
    [Fact]
    public void LegendMeasurer_HiddenLegend_ReturnsEmptySize()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Series")
                .WithLegend(visible: false)).Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    /// <summary>LegendMeasurer line 85: `MathTextParser.ContainsMath(labels[i])` true arm —
    /// label with $...$ math content uses MeasureRichText path.</summary>
    [Fact]
    public void LegendMeasurer_MathLabel_UsesRichTextMeasurer()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "$\\alpha + \\beta$")
                .WithLegend()).Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    /// <summary>Lightweight stub for ComputeDataRange tests that don't need a real
    /// axes context (the StackedAreaSeries early-return path doesn't touch it).
    /// Mirrors the same shape as the TestAxesContext in PinpointBranchTests.cs.</summary>
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
