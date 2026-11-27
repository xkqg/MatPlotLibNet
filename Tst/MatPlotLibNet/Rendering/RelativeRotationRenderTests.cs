// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>v1.11.0 — Verifies SVG output of <see cref="RelativeRotationSeries"/> rendering.</summary>
public class RelativeRotationRenderTests
{
    private static double[] Flat(int n, double v = 100.0) => Enumerable.Repeat(v, n).ToArray();
    private static double[] Rising(int n, double start = 100.0, double step = 1.0) =>
        Enumerable.Range(0, n).Select(i => start + i * step).ToArray();

    private static string RenderSvg(Action<RelativeRotationSeries>? configure = null)
    {
        // 60 bars: enough for DualEma(10,26) to produce valid momentum
        // (rsRatioValid = 60-25 = 35 ≥ longP=26, so the second EMA pass succeeds).
        var assetCloses   = new[] { Rising(60), Flat(60, 95) };
        var benchClose    = Flat(60);
        var labels        = new[] { "ETH", "BNB" };
        return Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.RelativeRotation(assetCloses, benchClose, labels, configure))
            .ToSvg();
    }

    // ── Smoke tests ──────────────────────────────────────────────────────────

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("Render_EmitsAssetLabelsAsText", () => RenderSvg(), "ETH", "BNB"),
        // Periods larger than data → all rsRatio + rsMomentum NaN → DrawAsset returns
        // early at the points.Count == 0 guard without rendering any circles.
        // > data length → EMA returns empty → all NaN
        // no heads drawn
        new DrawnToken("AllNaNTail_DoesNotCrash", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
                ax.RelativeRotation([Rising(30), Flat(30, 95)], Flat(30), ["ETH", "BNB"], s =>
                {
                    s.ShortPeriod = 25;
                    s.LongPeriod  = 50;  
                }))
            .ToSvg(), "<svg") { Absent = ["<circle "] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    // ── Quadrant grid ────────────────────────────────────────────────────────

    /// <summary>The rotation chart of two assets with a tail of <paramref name="tailLength"/> points and none of the overlays an overlay fact adds.</summary>
    private static string BaselineSvg(int tailLength) => Plt.Create()
        .AddSubPlot(1, 1, 1, ax => ax.RelativeRotation(
            [Rising(60), Flat(60, 95)], Flat(60), ["ETH", "BNB"], s =>
            {
                s.ShortPeriod = 3;
                s.LongPeriod  = 5;
                s.TailLength  = tailLength;
            }))
        .ToSvg();

    /// <summary>Four quadrant fills are at least 4 rect elements beyond the plot frame, and the crosshair is 2 dashed lines (horizontal and
    /// vertical through 100/100).</summary>
    [Theory]
    [InlineData("<rect ")]
    [InlineData("<line ")]
    public void ShowQuadrantGrid_True_EmitsMoreOfTheTagThanFalse(string tag)
    {
        string svgOn  = RenderSvg(s => s.ShowQuadrantGrid = true);
        string svgOff = RenderSvg(s => s.ShowQuadrantGrid = false);
        int on  = svgOn.CountOf(tag);
        int off = svgOff.CountOf(tag);
        Assert.True(on > off, $"ShowQuadrantGrid=true ({on} {tag}) must exceed off ({off}).");
    }

    // ── the centre of a chart whose assets all stand on one side of it ───────

    /// <summary>The quadrants and the crosshair are what a relative rotation graph is read by. With every asset above 100 the
    /// plot used to end before the centre: no crosshair, and the quadrants on that side had no area. Now the centre is in the
    /// plot, so there are two crosshair lines and four quadrants with an area.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ARrgOfAssetsOnOneSideOfTheCentre_StillDrawsTheCrosshairAndFourQuadrants(bool above)
    {
        double[] asset = [.. Enumerable.Range(0, 60).Select(i => 100 + (above ? 1 : -1) * i * i * 0.02)];
        var figure = Plt.Create().WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax.RelativeRotation([asset], Flat(60), ["ONE"], s => { s.ShortPeriod = 3; s.LongPeriod = 5; }))
            .Build();
        var ctx = new RecordingRenderContext();

        new ChartRenderer().Render(figure, ctx);

        var quadrants = ctx.Of<DrawRectangleCall>()
            .Where(c => (c.Fill) is { A: 20 })
            .Select(c => c.Rect).ToList();
        Assert.Equal(4, quadrants.Count);
        Assert.All(quadrants, r => Assert.True(r.Width > 0 && r.Height > 0, $"a quadrant of no area: {r}"));
        Assert.Equal(2, ctx.Of<DrawLineCall>().Count(c => (c.Color) == new Color(100, 100, 100, 120)));
    }

    // ── Asset heads ──────────────────────────────────────────────────────────

    [Fact]
    public void Render_EmitsCirclePerAssetHead()
    {
        // 2 assets → at least 2 <circle> elements for the asset heads.
        string svg = RenderSvg();
        int circles = svg.CountOf("<circle ");
        Assert.True(circles >= 2, $"Expected ≥2 circles for 2 assets, got {circles}.");
    }

    // ── Tail polylines ───────────────────────────────────────────────────────

    [Fact]
    public void Render_EmitsPolylineElementsForTails()
    {
        // Each asset with a valid tail emits at least 1 <polyline> or <line> for segments.
        string svg = RenderSvg(s => { s.ShortPeriod = 3; s.LongPeriod = 5; s.TailLength = 4; });
        bool hasTailElements = svg.Contains("<polyline ") || svg.Contains("<line ");
        Assert.True(hasTailElements, "Expected polyline or line elements for tails.");
    }

    [Fact]
    public void ShowQuadrantGrid_False_SameCircleCount()
    {
        // Toggling the quadrant grid should not add/remove asset head circles.
        string svgOn  = RenderSvg(s => s.ShowQuadrantGrid = true);
        string svgOff = RenderSvg(s => s.ShowQuadrantGrid = false);
        Assert.Equal(svgOn.CountOf("<circle "), svgOff.CountOf("<circle "));
    }

    // ── ZScore formula ───────────────────────────────────────────────────────

    // ── LogReturn formula ─────────────────────────────────────────────────────

    // ── Single-asset branch ───────────────────────────────────────────────────

    // ── Absorption overlay ────────────────────────────────────────────────────

    /// <summary>The markup of two assets drawn with a tail of four points and the overlay the caller sets on the series.</summary>
    private static string OverlaySvg(Action<RelativeRotationSeries> overlay) => Plt.Create()
        .AddSubPlot(1, 1, 1, ax => ax.RelativeRotation(
            [Rising(60), Flat(60, 95)], Flat(60), ["ETH", "BNB"], s =>
            {
                s.ShortPeriod = 3;
                s.LongPeriod  = 5;
                s.TailLength  = 4;
                overlay(s);
            }))
        .ToSvg();

    /// <summary>Without an overlay a series draws one circle per asset (its head only); with the absorption ratio, the effective number of
    /// bets or both it draws one per tail point per asset.</summary>
    [Theory]
    [InlineData("absorption", true, false)]
    [InlineData("ENB", false, true)]
    [InlineData("both", true, true)]
    public void AnOverlay_EmitsACirclePerTailPoint(string overlayName, bool absorption, bool enb)
    {
        const int TailLen   = 4;
        const int NumAssets = 2;

        string svgOverlay = OverlaySvg(s =>
        {
            if (absorption) s.AbsorptionRatioPerBar = Enumerable.Repeat(0.4, 60).ToArray();
            if (enb) s.EnbPerBar = Enumerable.Repeat(3.0, 60).ToArray();
        });

        int circlesOverlay = svgOverlay.CountOf("<circle ");
        int circlesBase    = BaselineSvg(TailLen).CountOf("<circle ");

        Assert.True(circlesOverlay > circlesBase,
            $"The {overlayName} overlay ({circlesOverlay} circles) must exceed baseline ({circlesBase}).");
        Assert.Equal(TailLen * NumAssets, circlesOverlay);
    }

    // ── Fluent API coverage ────────────────────────────────────────────────────

}
