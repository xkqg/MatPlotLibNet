// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>v1.10 — verifies SVG output of <see cref="HeatmapSeries"/> rendering with the
/// new <c>ShowLabels</c>, <c>LabelFormat</c>, <c>CellValueColor</c>, and <c>MaskMode</c>
/// extensions.</summary>
public class HeatmapRenderTests
{
    private static double[,] Symmetric3x3 => new double[,]
    {
        { 1.00, 0.50, 0.20 },
        { 0.50, 1.00, 0.30 },
        { 0.20, 0.30, 1.00 },
    };

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

    private static string RenderSvg(double[,] data, Action<HeatmapSeries> configure) =>
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(data, configure))
            .ToSvg();

    // ── Baseline ─────────────────────────────────────────────────────────────

    [Fact]
    public void ShowLabels_False_NoTextEmitted()
    {
        string svg = RenderSvg(Symmetric3x3, _ => { });
        Assert.DoesNotContain(">1.00<", svg);
        Assert.DoesNotContain(">0.50<", svg);
    }

    // ── ShowLabels / LabelFormat ─────────────────────────────────────────

    [Fact]
    public void ShowLabels_True_F2_EmitsFormattedValues()
    {
        string svg = RenderSvg(Symmetric3x3, s => { s.ShowLabels = true; });
        Assert.Contains(">1.00<", svg);
        Assert.Contains(">0.50<", svg);
        Assert.Contains(">0.20<", svg);
    }

    [Fact]
    public void ShowLabels_True_P1_EmitsPercentFormat()
    {
        var data = new double[,] { { 0.25, 0.75 } };
        string svg = RenderSvg(data, s => { s.ShowLabels = true; s.LabelFormat = "P1"; });
        // Culture-invariant percent format is "25.0%" / "75.0%".
        Assert.Contains("25.0", svg);
        Assert.Contains("75.0", svg);
    }

    [Fact]
    public void ShowLabels_True_ExplicitColor_UsesIt()
    {
        string svg = RenderSvg(Symmetric3x3, s =>
        {
            s.ShowLabels = true;
            s.CellValueColor = Colors.Red;
        });
        // Auto-contrast would never emit pure red against viridis colours, so finding the
        // hex of Colors.Red proves the explicit override applied.
        Assert.Contains(Colors.Red.ToHex(), svg);
    }

    // ── MaskMode ─────────────────────────────────────────────────────────────

    [Fact]
    public void MaskMode_None_KeepsAllCells()
    {
        string svg = RenderSvg(Symmetric3x3, _ => { });
        // 3x3 + plot frame + axes elements; the heatmap itself contributes 9 fill rects.
        // Use a sentinel: the cell at (col=2, row=0) carries value 0.20 — visible only
        // when MaskMode != UpperTriangle/UpperTriangleStrict.
        Assert.Contains("rect", svg);
        // 9 cells rendered (sanity — too tight a count would be brittle, so just verify >=9 fill rects).
        Assert.True(CountOccurrences(svg, "<rect") >= 9);
    }

    [Fact]
    public void MaskMode_UpperTriangle_HidesUpperCells()
    {
        // 3x3: lower triangle + diagonal = 6 visible cells (out of 9).
        string lower = RenderSvg(Symmetric3x3, s => { s.MaskMode = HeatmapMaskMode.UpperTriangle; });
        string none  = RenderSvg(Symmetric3x3, _ => { });
        Assert.True(CountOccurrences(none, "<rect") - CountOccurrences(lower, "<rect") == 3);
    }

    [Fact]
    public void MaskMode_LowerTriangle_HidesLowerCells()
    {
        string upper = RenderSvg(Symmetric3x3, s => { s.MaskMode = HeatmapMaskMode.LowerTriangle; });
        string none  = RenderSvg(Symmetric3x3, _ => { });
        Assert.True(CountOccurrences(none, "<rect") - CountOccurrences(upper, "<rect") == 3);
    }

    [Fact]
    public void MaskMode_UpperTriangleStrict_HidesDiagonalToo()
    {
        // 3x3: strict lower triangle = 3 cells (diagonal hidden).
        string strict = RenderSvg(Symmetric3x3, s => { s.MaskMode = HeatmapMaskMode.UpperTriangleStrict; });
        string none   = RenderSvg(Symmetric3x3, _ => { });
        // 6 cells hidden (diagonal 3 + upper 3).
        Assert.True(CountOccurrences(none, "<rect") - CountOccurrences(strict, "<rect") == 6);
    }

    [Fact]
    public void MaskMode_LowerTriangleStrict_HidesDiagonalToo()
    {
        string strict = RenderSvg(Symmetric3x3, s => { s.MaskMode = HeatmapMaskMode.LowerTriangleStrict; });
        string none   = RenderSvg(Symmetric3x3, _ => { });
        Assert.True(CountOccurrences(none, "<rect") - CountOccurrences(strict, "<rect") == 6);
    }

    [Fact]
    public void Combined_ShowLabels_LowerTriangle_TextOnlyOnVisibleCells()
    {
        string masked = RenderSvg(Symmetric3x3, s =>
        {
            s.ShowLabels = true;
            s.MaskMode = HeatmapMaskMode.UpperTriangle;
        });
        string none = RenderSvg(Symmetric3x3, s => { s.ShowLabels = true; });

        // Off-diagonal value 0.50 appears at (0,1) and (1,0). Masking the upper triangle
        // hides (0,1), leaving exactly one ">0.50<" annotation; the unmasked render has two.
        Assert.Equal(2, CountOccurrences(none, ">0.50<"));
        Assert.Equal(1, CountOccurrences(masked, ">0.50<"));
    }

    [Fact]
    public void ShowLabels_False_WithActiveMaskMode_EmitsNoText()
    {
        // ShowLabels=false must suppress text even when MaskMode is active.
        string svg = RenderSvg(Symmetric3x3, s => { s.MaskMode = HeatmapMaskMode.UpperTriangle; });
        Assert.DoesNotContain(">1.00<", svg);
        Assert.DoesNotContain(">0.50<", svg);
    }

    [Fact]
    public void LabelFormat_IntegerOnlySpecifier_ThrowsFormatException()
    {
        // "D" (decimal integer) is only valid for integral types; double.ToString("D") throws FormatException.
        // Parallel.For in SvgTransform wraps it in AggregateException.
        var ex = Assert.Throws<AggregateException>(() =>
            RenderSvg(new double[,] { { 1.0 } }, s => { s.ShowLabels = true; s.LabelFormat = "D"; }));
        Assert.Contains(ex.Flatten().InnerExceptions, e => e is FormatException);
    }

    // ── Auto-contrast ────────────────────────────────────────────────────────

    [Fact]
    public void AutoContrast_DarkFill_PicksWhite()
    {
        // Viridis at value 0 is dark purple → white text expected.
        var data = new double[,] { { 0.0, 1.0 } };
        string svg = RenderSvg(data, s => { s.ShowLabels = true; });
        Assert.Contains(Colors.White.ToHex(), svg);
    }

    [Fact]
    public void AutoContrast_LightFill_PicksBlack()
    {
        // Viridis at value 1 is bright yellow → black text expected.
        var data = new double[,] { { 0.0, 1.0 } };
        string svg = RenderSvg(data, s => { s.ShowLabels = true; });
        Assert.Contains(Colors.Black.ToHex(), svg);
    }
}
