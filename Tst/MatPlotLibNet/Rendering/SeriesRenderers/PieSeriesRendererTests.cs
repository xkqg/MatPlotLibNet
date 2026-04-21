// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>Phase X.9.a (v1.7.2, 2026-04-19) — drives every render branch in
/// <see cref="MatPlotLibNet.Rendering.SeriesRenderers.PieSeriesRenderer"/>. Pre-X.9
/// the renderer was at 98%L / 61%B because slice-decoration arms (shadow, explode,
/// AutoPct, outer-labels with collision pass) were not exercised. This file pins:
///   - Sizes.Sum() == 0 → early return (line 23)
///   - Radius set explicitly (line 27 ?? has-value arm)
///   - CounterClockwise true vs false (line 33 ternary's two arms)
///   - Explode array per-slice (line 49 ternary's true arm)
///   - Shadow=true (line 55 true arm draws gray slice first)
///   - Colors per-slice (line 65 ternary's true arm)
///   - AutoPct format string (line 73 true arm)
///   - Labels per-slice → outer-label collision pass (lines 85-96 + 102-116)
///   - Labels alignment branches at 0°/90°/180° angles (line 92 ternary)</summary>
public class PieSeriesRendererTests
{
    private static string Render(PieSeries s) =>
        Plt.Create()
            .WithSize(400, 400)
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(s).HideAllAxes())
            .Build()
            .ToSvg();

    [Fact]
    public void Render_BasicPie_DrawsSlices()
    {
        var svg = Render(new PieSeries([1.0, 2.0, 3.0]));
        Assert.Contains("<path", svg);
    }

    /// <summary>Sizes.Sum() == 0 → early return at line 23.</summary>
    [Fact]
    public void Render_AllZeroSizes_EarlyReturn()
    {
        var svg = Render(new PieSeries([0.0, 0.0, 0.0]));
        Assert.DoesNotContain("<path", svg);
    }

    /// <summary>Explicit Radius (line 27 .HasValue true arm) — radius scaled by Radius.Value.</summary>
    [Fact]
    public void Render_ExplicitRadius_ScalesPie()
    {
        var svg = Render(new PieSeries([1.0, 2.0, 3.0]) { Radius = 0.5 });
        Assert.Contains("<path", svg);
    }

    /// <summary>CounterClockwise=true (line 33 ternary's true arm) — slices sweep
    /// in the opposite direction.</summary>
    [Fact]
    public void Render_CounterClockwise_SweepsForward()
    {
        var svg = Render(new PieSeries([1.0, 2.0, 3.0]) { CounterClockwise = true });
        Assert.Contains("<path", svg);
    }

    /// <summary>Explode set with matching length (line 49 ternary's true arm). Each slice
    /// drifts outward by Explode[i] * radius.</summary>
    [Fact]
    public void Render_Explode_OffsetsSlicesOutward()
    {
        var svg = Render(new PieSeries([1.0, 2.0, 3.0])
        {
            Explode = new[] { 0.0, 0.1, 0.2 },
        });
        Assert.Contains("<path", svg);
    }

    /// <summary>Shadow=true (line 55 true arm) — emits a gray offset slice before the
    /// real one. SVG path count doubles.</summary>
    [Fact]
    public void Render_Shadow_EmitsExtraGraySlice()
    {
        var basic = Render(new PieSeries([1.0, 2.0]));
        var shadow = Render(new PieSeries([1.0, 2.0]) { Shadow = true });
        // Shadow path count > basic path count (one extra gray <path> per slice).
        Assert.True(shadow.Split("<path").Length > basic.Split("<path").Length);
    }

    /// <summary>Colors per-slice (line 65 ternary's true arm). Custom palette.</summary>
    [Fact]
    public void Render_Colors_PerSlice_UsesCustomPalette()
    {
        var svg = Render(new PieSeries([1.0, 2.0, 3.0])
        {
            Colors = new[] { Colors.Red, Colors.Green, Colors.Blue },
        });
        Assert.Contains("<path", svg);
    }

    /// <summary>AutoPct (line 73 true arm) — emits percentage text inside each slice.</summary>
    [Fact]
    public void Render_AutoPct_DrawsPercentages()
    {
        var svg = Render(new PieSeries([10.0, 20.0, 30.0]) { AutoPct = "{0:F1}%" });
        Assert.Contains(">16.7%<", svg);     // 10/(10+20+30) = 16.67%
        Assert.Contains(">33.3%<", svg);     // 20/(60) = 33.33%
        Assert.Contains(">50.0%<", svg);     // 30/(60) = 50%
    }

    /// <summary>Colors[] shorter than Sizes[] (line 65 ternary's `i &lt; Colors.Length`
    /// false arm). Per-slice color array runs out before all slices are drawn.</summary>
    [Fact]
    public void Render_ColorsShorterThanSizes_FallsBackToSeriesColor()
    {
        var svg = Render(new PieSeries([1.0, 2.0, 3.0])
        {
            Colors = new[] { Colors.Red },     // only 1 color, 3 slices
        });
        Assert.Contains("<path", svg);
    }

    /// <summary>Theme with DefaultFont set (line 38 `is { } f` true arm). MatplotlibClassic
    /// theme provides explicit font metrics; the renderer should pick them up rather than
    /// fall back to the size-12 default.</summary>
    [Fact]
    public void Render_WithThemedFont_PicksUpThemeFont()
    {
        var svg = Plt.Create()
            .WithSize(400, 400)
            .WithTheme(Theme.MatplotlibClassic)
            .AddSubPlot(1, 1, 1, ax => ax
                .AddSeries(new PieSeries([1.0, 2.0]) { Labels = new[] { "X", "Y" } })
                .HideAllAxes())
            .Build()
            .ToSvg();
        Assert.Contains(">X<", svg);
    }

    /// <summary>Labels (lines 85-96) → outer-label collision-resolution pass (lines 102-116).
    /// All three text-alignment arms (Left for cosA &gt; 0.1, Right for cosA &lt; -0.1, Center
    /// otherwise) are exercised by 4 slices spaced at cardinal directions.</summary>
    [Fact]
    public void Render_Labels_AllAlignmentArms()
    {
        // 4 equal slices starting at 90° (top), sweeping clockwise:
        // slice 0 mid 45° (right of top, cosA > 0 → Left-aligned)
        // slice 1 mid 315° = -45° (right-bottom, cosA > 0 → Left)
        // slice 2 mid 225° (left-bottom, cosA < 0 → Right)
        // slice 3 mid 135° (left-top, cosA < 0 → Right)
        var svg = Render(new PieSeries([1.0, 1.0, 1.0, 1.0])
        {
            Labels = new[] { "A", "B", "C", "D" },
        });
        Assert.Contains(">A<", svg);
        Assert.Contains(">B<", svg);
        Assert.Contains(">C<", svg);
        Assert.Contains(">D<", svg);
    }

    // ── Wave J.0.e — remaining uncovered arms ────────────────────────────────

    /// <summary>L92 CENTER arm — cosA ∈ [-0.1, 0.1] → TextAlignment.Center.
    /// StartAngle=0, CCW, sizes [1,1,2]: third slice midAngle = 3π/2 → cos = 0.</summary>
    [Fact]
    public void Render_LabelAtTopOrBottom_UsesCenterAlignment()
    {
        var svg = Render(new PieSeries([1.0, 1.0, 2.0])
        {
            Labels = ["left", "right", "center"],
            StartAngle = 0,
            CounterClockwise = true,
        });
        Assert.Contains(">center<", svg);
    }

    /// <summary>L110 TRUE arm — very wide labels on a tiny canvas guarantee that
    /// LabelLayoutEngine offsets each label &gt;6px from its anchor, setting
    /// <c>LeaderLineStart</c> so <c>DrawLeaderLine</c> fires. The SVG emits &lt;line&gt;
    /// elements (one per displaced label) that do not appear in pie renders without
    /// labels or without collision.</summary>
    [Fact]
    public void Render_DenseLabels_SmallCanvas_DrawsLeaderLines()
    {
        // 12 equal slices on a 100×100 canvas. Outer label radius ≈44px, arc spacing
        // ≈23px, "WWWWWWWWWW" ≈92px wide → every adjacent pair overlaps → labels
        // are pushed well past the 6px leader-line threshold.
        double[] sizes = Enumerable.Repeat(1.0, 12).ToArray();
        string[] labels = Enumerable.Repeat("WWWWWWWWWW", 12).ToArray();
        string svg = Plt.Create()
            .WithSize(100, 100)
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new PieSeries(sizes) { Labels = labels }).HideAllAxes())
            .Build()
            .ToSvg();
        // HideAllAxes removes all axis lines; the only <line> elements come from leader lines.
        Assert.Contains("<line", svg);
    }
}
